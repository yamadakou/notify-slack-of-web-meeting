using dcinc.api.entities;
using dcinc.api.queries;
using dcinc.cosmos.helpers;
using dcinc.json.helpers;
using dcinc.json.converters;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Container = Microsoft.Azure.Cosmos.Container;

namespace dcinc.api
{
    /// <summary>
    /// Web会議情報API
    /// </summary>
    public class WebMeetings
    {
        private readonly ILogger<WebMeetings> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly CosmosLinqSerializerOptions _cosmosLinqSerializerOptions;

        public WebMeetings(ILogger<WebMeetings> logger)
        {
            _logger = logger;
            _jsonSerializerOptions = JsonSerializerHelper.GetJsonSerializerOptions();
            _cosmosLinqSerializerOptions = CosmosSerializerHelper.GetCosmosLinqSerializerOptions();
        }

        #region Web会議情報を登録
        /// <summary>
        /// Web会議情報を登録する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="documentsOut">CosmosDBのドキュメント</param>
        /// <param name="log">ロガー</param>
        /// <returns>登録したWeb会議情報</returns>
        [Function("AddWebMeetings")]
        public async Task<IActionResult> AddWebMeetings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "WebMeetings")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "WebMeetings",
                Connection = "CosmosDbConnectionString")
                ]CosmosClient client)
        {
            string message = string.Empty;

            try
            {
                _logger.LogInformation("POST webMeetings");

                // リクエストのBODYからパラメータ取得
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<WebMeeting>(requestBody, _jsonSerializerOptions);

                // エンティティに設定
                WebMeeting webMeeting = new WebMeeting()
                {
                    Name = data?.Name,
                    StartDateTime = data?.StartDateTime ?? DateTime.UnixEpoch,
                    Url = data?.Url,
                    RegisteredBy = data?.RegisteredBy,
                    SlackChannelId = data?.SlackChannelId
                };

                // 入力値チェックを行う
                WebMeetingValidator validator = new WebMeetingValidator();
                validator.ValidateAndThrow(webMeeting);

                // Web会議情報を登録
                message = await AddWebMeetings(client, webMeeting);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new {Message = ex.Message, StackTrace = ex.StackTrace});
            }

            return new OkObjectResult(message);
        }

        /// <summary>
        /// Web会議情報を登録する。
        /// </summary>
        /// <param name="documentsOut">CosmosDBのドキュメント</param>
        /// <param name="webMeeting">Web会議情報</param>
        /// <returns>登録したWeb会議情報</returns>
        private async Task<string> AddWebMeetings(
                    CosmosClient client,
                    WebMeeting webMeeting
                    )
        {
            // 登録日時にUTCでの現在日時を設定
            webMeeting.RegisteredAt = DateTime.UtcNow;
            // Add a JSON document to the output container.
            string documentItem = JsonSerializer.Serialize(webMeeting, _jsonSerializerOptions);
            Container container = client.GetContainer("notify-slack-of-web-meeting-db", "WebMeetings");
            await container.CreateItemAsync<WebMeeting>(webMeeting);
            return documentItem;
        }
        #endregion

        #region Web会議情報一覧を取得
        /// <summary>
        /// Web会議情報一覧を取得する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="log">ロガー</param>
        /// <returns>Web会議情報一覧</returns>
        [Function("GetWebMeetings")]
        public async Task<IActionResult> GetWebMeetings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "WebMeetings")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "WebMeetings",
                Connection = "CosmosDbConnectionString")
                ]CosmosClient client)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            string message = string.Empty;

            try
            {
                _logger.LogInformation("GET webMeetings");

                // クエリパラメータから検索条件パラメータを設定
                WebMeetingsQueryParameter queryParameter = new WebMeetingsQueryParameter()
                {
                    FromDate = req.Query["fromDate"],
                    ToDate = req.Query["toDate"],
                    RegisteredBy = req.Query["registeredBy"],
                    SlackChannelId = req.Query["slackChannelId"]
                };

                // 入力値チェックを行う
                var queryParameterValidator = new WebMeetingsQueryParameterValidator();
                queryParameterValidator.ValidateAndThrow(queryParameter);

                // Web会議情報を取得
                message = JsonSerializer.Serialize(await GetWebMeetings(client, queryParameter));
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new {Message = ex.Message, StackTrace = ex.StackTrace});
            }

            return new OkObjectResult(message);
        }

        /// <summary>
        /// Web会議情報一覧を取得する。
        /// </summary>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="queryParameter">抽出条件パラメータ</param>
        /// <param name="log">ロガー</param>
        /// <returns>Web会議情報一覧</returns>
        internal async Task<IEnumerable<WebMeeting>> GetWebMeetings(
                   CosmosClient client,
                   WebMeetingsQueryParameter queryParameter)
        {
            // Get a JSON document from the container.
            Container container = client.GetContainer("notify-slack-of-web-meeting-db", "WebMeetings");
            var whereExpression = queryParameter.GetWhereExpression();
            _logger.LogInformation(whereExpression.Body.Print());
            _logger.LogInformation(queryParameter.ToStringParamaters());

            var documentItems = new List<WebMeeting>();
            using (var query = container.GetItemLinqQueryable<WebMeeting>(linqSerializerOptions: _cosmosLinqSerializerOptions).Where(whereExpression).AsQueryable().ToFeedIterator())
            {
                while (query.HasMoreResults)
                {
                    foreach (var documentItem in await query.ReadNextAsync())
                    {
                        documentItems.Add(documentItem);
                    }
                }
            }
            return documentItems;
        }
        #endregion

        #region Web会議情報を取得
        /// <summary>
        /// Web会議情報を取得する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="log">ロガー</param>
        /// <returns>Web会議情報</returns>
        [Function("GetWebMeetingById")]
        public async Task<IActionResult> GetWebMeetingById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "WebMeetings/{ids}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "WebMeetings",
                Connection = "CosmosDbConnectionString")
                ]CosmosClient client)
        {
            string message = string.Empty;

            try
            {
                string ids = req.RouteValues["ids"].ToString();
                _logger.LogInformation($"GET webMeetings/{ids}");

                // クエリパラメータから検索条件パラメータを設定
                WebMeetingsQueryParameter queryParameter = new WebMeetingsQueryParameter()
                {
                    Ids = ids
                };

                // Web会議情報を取得
                var documentItems = await GetWebMeetings(client, queryParameter);

                if(!documentItems.Any())
                {
                    return new NotFoundObjectResult($"Target item not found. Id={ids}");
                }
                message = JsonSerializer.Serialize(documentItems);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new {Message = ex.Message, StackTrace = ex.StackTrace});
            }

            return new OkObjectResult(message);
        }

        #endregion

        #region Web会議情報を削除
        /// <summary>
        /// Web会議情報を削除する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="log">ロガー</param>
        /// <returns>削除したWeb会議情報</returns>
        [Function("DeleteWebMeetingById")]
        public async Task<IActionResult> DeleteWebMeetingById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "WebMeetings/{ids}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "WebMeetings",
                Connection = "CosmosDbConnectionString")
                ]CosmosClient client)
        {
            string message = string.Empty;

            try
            {
                string ids = req.RouteValues["ids"].ToString();
                _logger.LogInformation($"DELETE webMeetings/{ids}");

                // Web会議情報を削除
                var documentItems = await DeleteWebMeetingById(client, ids);

                if(!documentItems.Any())
                {
                    return new NotFoundObjectResult($"Target item not found. Id={ids}");
                }
                message = JsonSerializer.Serialize(documentItems);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new {Message = ex.Message, StackTrace = ex.StackTrace});
            }

            return new OkObjectResult(message);
        }

        /// <summary>
        /// Web会議情報を削除する。
        /// </summary>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="ids">削除するWeb会議情報のID</param>
        /// <param name="log">ロガー</param>
        /// <returns>削除したWeb会議情報</returns>
        internal async Task<IEnumerable<WebMeeting>> DeleteWebMeetingById(
                   CosmosClient client,
                   string ids)
        {
            // 削除に必要なパーティションキーを取得するため、Web会議情報を取得後に削除する。
            _logger.LogInformation($"DeleteWebMeetingById: ids={ids}");

            // クエリパラメータに削除するWeb会議情報のIDを設定
            WebMeetingsQueryParameter queryParameter = new WebMeetingsQueryParameter()
            {
                Ids = ids
            };
            // Delete a JSON document from the container.
            Container container = client.GetContainer("notify-slack-of-web-meeting-db", "WebMeetings");

            // Web会議情報を取得し削除する。
            var documentItems = await GetWebMeetings(client, queryParameter);
            _logger.LogInformation($"DeleteWebMeetingById: Count={documentItems.Count()}");

            foreach (var documentItem in documentItems)
            {
                // パーティションキーを取得
                var partitionKey = documentItem.Date.ToJsonFormat();
                _logger.LogInformation($"DeleteWebMeetingById: partitionKey={partitionKey}");

                // Web会議情報を削除
                await container.DeleteItemAsync<WebMeeting>(documentItem.Id, new PartitionKey(partitionKey));
            }

            return documentItems;
        }
        #endregion

    }
}
