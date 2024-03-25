using dcinc.api.entities;
using dcinc.api.queries;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Container = Microsoft.Azure.Cosmos.Container;
using dcinc.cosmos.helpers;
using dcinc.json.helpers;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace dcinc.api
{
    /// <summary>
    /// Slackチャンネル情報API
    /// </summary>
    public class SlackChannels
    {
        private readonly ILogger<SlackChannels> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly CosmosLinqSerializerOptions _cosmosLinqSerializerOptions;

        public SlackChannels(ILogger<SlackChannels> logger)
        {
            _logger = logger;
            _jsonSerializerOptions = JsonSerializerHelper.GetJsonSerializerOptions();
            _cosmosLinqSerializerOptions = CosmosSerializerHelper.GetCosmosLinqSerializerOptions();
        }

        #region Slackチャンネル情報を登録
        /// <summary>
        /// Slackチャンネル情報を登録する。
        /// </summary>
        /// <returns>登録したSlackチャンネル情報</returns>        
        [Function("AddSlackChannels")]
        public async Task<IActionResult> AddSlackChannels(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SlackChannels")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "SlackChannels",
                Connection = "CosmosDbConnectionString")]CosmosClient client)
        {
            string message = string.Empty;

            try
            {

                _logger.LogInformation("POST SlackChannels");

                // リクエストのBODYからパラメータ取得
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<SlackChannel>(requestBody, _jsonSerializerOptions);

                // エンティティに設定
                SlackChannel slackChannel = new SlackChannel()
                {
                    Name = data?.Name,
                    WebhookUrl = data?.WebhookUrl,
                    RegisteredBy = data?.RegisteredBy
                };

                // 入力値チェックを行う
                SlackChannelValidator validator = new SlackChannelValidator();
                validator.ValidateAndThrow(slackChannel);

                // Slackチャンネル情報を登録
                message = await AddSlackChannels(client, slackChannel);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new {Message = ex.Message, StackTrace = ex.StackTrace});
            }

            return new OkObjectResult(message);
        }

        /// <summary>
        /// Slackチャンネル情報を登録する。
        /// </summary>
        /// <param name="documentsOut">CosmosDBのドキュメント</param>
        /// <param name="slackChannel">Slackチャンネル情報</param>
        /// <returns>登録したSlackチャンネル情報</returns>
        private async Task<string> AddSlackChannels(
                    CosmosClient client,
                    SlackChannel slackChannel
                    )
        {
            // 登録日時にUTCでの現在日時を設定
            slackChannel.RegisteredAt = DateTime.UtcNow;
            // Add a JSON document to the output container.
            string documentItem = JsonSerializer.Serialize(slackChannel, _jsonSerializerOptions);
            Container container = client.GetContainer("notify-slack-of-web-meeting-db", "SlackChannels");
            await container.CreateItemAsync<SlackChannel>(slackChannel);
            return documentItem;
        }
        #endregion

        #region Slackチャンネル情報一覧を取得
        /// <summary>
        /// Slackチャンネル情報一覧を取得する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="log">ロガー</param>
        /// <returns>Slackチャンネル情報一覧</returns>
        [Function("GetSlackChannels")]
        public async Task<IActionResult> GetSlackChannels(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SlackChannels")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "SlackChannels",
                Connection = "CosmosDbConnectionString")
                ]CosmosClient client)
        {
            string message = string.Empty;

            try
            {
                _logger.LogInformation("GET slackChannels");

                // クエリパラメータから検索条件パラメータを設定
                SlackChannelsQueryParameter queryParameter = new SlackChannelsQueryParameter()
                {
                    Ids = req.Query["ids"],
                    Name = req.Query["name"],
                    WebhookUrl = req.Query["webhookUrl"],
                    RegisteredBy = req.Query["registeredBy"]
                };

                // Slackチャンネル情報を取得
                message = JsonSerializer.Serialize(await GetSlackChannels(client, queryParameter));
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new {Message = ex.Message, StackTrace = ex.StackTrace});
            }

            return new OkObjectResult(message);
        }

        /// <summary>
        /// Slackチャンネル情報一覧を取得する。
        /// </summary>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="queryParameter">抽出条件パラメータ</param>
        /// <param name="log">ロガー</param>
        /// <returns>Slackチャンネル情報一覧</returns>
        internal async Task<IEnumerable<SlackChannel>> GetSlackChannels(
                   CosmosClient client,
                   SlackChannelsQueryParameter queryParameter
                   )
        {
            // Get a JSON document from the container.
            Container container = client.GetContainer("notify-slack-of-web-meeting-db", "SlackChannels");
            var whereExpression = queryParameter.GetWhereExpression();
            _logger.LogInformation(whereExpression.Body.Print());

            var documentItems = new List<SlackChannel>();
            using (var query = container.GetItemLinqQueryable<SlackChannel>(linqSerializerOptions: _cosmosLinqSerializerOptions).Where(whereExpression).AsQueryable().ToFeedIterator())
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

        #region Slackチャンネル情報を取得
        /// <summary>
        /// Slackチャンネル情報を取得する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="log">ロガー</param>
        /// <returns>Slackチャンネル情報</returns>
        [Function("GetSlackChannelById")]
        public IActionResult GetSlackChannelById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SlackChannels/{id}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "SlackChannels",
                Connection = "CosmosDbConnectionString",
                Id = "{id}", PartitionKey = "{id}")
                ]SlackChannel slackChannel)
        {
            string id = req.RouteValues["id"].ToString();
            _logger.LogInformation($"GET slackChannels/{id}");

            if (slackChannel == null)
            {
                return new NotFoundObjectResult($"Target item not found. Id={id}");
            }

            return new OkObjectResult(JsonSerializer.Serialize(slackChannel));
        }
        #endregion

        #region Slackチャンネル情報を削除
        /// <summary>
        /// Slackチャンネル情報を削除する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="log">ロガー</param>
        /// <returns>削除したSlackチャンネル情報</returns>
        [Function("DeleteSlackChannelById")]
        public async Task<IActionResult> DeleteSlackChannelById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "SlackChannels/{id}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "SlackChannels",
                Connection = "CosmosDbConnectionString")
                ]CosmosClient client)
        {
            string message = string.Empty;

            try
            {
                string id = req.RouteValues["id"].ToString();
                _logger.LogInformation($"DELETE slackChannels/{id}");

                // Slackチャンネル情報を削除
                var documentItems = await DeleteSlackChannelById(client, id);

                if(!documentItems.Any())
                {
                    return new NotFoundObjectResult($"Target item not found. Id={id}");
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
        /// Slackチャンネル情報を削除する。
        /// </summary>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="ids">削除するSlackチャンネル情報のID</param>
        /// <param name="log">ロガー</param>
        /// <returns>削除したSlackチャンネル情報</returns>
        private async Task<IEnumerable<SlackChannel>> DeleteSlackChannelById(
                   CosmosClient client,
                   string ids)
        {
            // 事前に存在確認後に削除

            // クエリパラメータに削除するSlackチャンネル情報のIDを設定
            SlackChannelsQueryParameter queryParameter = new SlackChannelsQueryParameter()
            {
                Ids = ids,
            };

            // Slackチャンネル情報を取得
            var documentItems = await GetSlackChannels(client, queryParameter);
            foreach (var documentItem in documentItems)
            {
                // Slackチャンネル情報を削除
                // Delete a JSON document from the container.
                Container container = client.GetContainer("notify-slack-of-web-meeting-db", "SlackChannels");
                await container.DeleteItemAsync<SlackChannel>(documentItem.Id, new PartitionKey(documentItem.Id));
            }

            return documentItems;
        }
        #endregion

    }
}
