using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FluentValidation;
using dcinc.api.entities;
using dcinc.api.queries;

namespace dcinc.api
{
    public static class WebMeetings
    {
        /// <summary>
        /// Web会議情報を登録する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="documentsOut">CosmosDBのドキュメント</param>
        /// <param name="log">ロガー</param>
        /// <returns></returns>
        [FunctionName("AddWebMeetings")]
        public static async Task<IActionResult> AddWebMeetings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "WebMeetings")] HttpRequest req,
            [CosmosDB(
                databaseName: "notify-slack-of-web-meeting-db",
                collectionName: "WebMeetings",
                ConnectionStringSetting = "CosmosDbConnectionString")
                ]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string message = string.Empty;

            try
            {
                log.LogInformation("POST webMeetings");

                // リクエストのBODYからパラメータ取得
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                // エンティティに設定
                WebMeeting webMeeting = new WebMeeting()
                {
                    Name = data?.name,
                    StartDateTime = data?.startDateTime ?? DateTime.UnixEpoch,
                    Url = data?.url,
                    RegisteredBy = data?.registeredBy,
                    SlackChannelId = data?.slackChannelId
                };

                // 入力値チェックを行う
                WebMeetingValidator validator = new WebMeetingValidator();
                validator.ValidateAndThrow(webMeeting);

                // Web会議情報を登録
                message = await AddWebMeetings(documentsOut, webMeeting);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }

            return new OkObjectResult($"This HTTP triggered function executed successfully.\n{message}");
        }

        /// <summary>
        /// Web会議情報を登録する。
        /// </summary>
        /// <param name="documentsOut">CosmosDBのドキュメント</param>
        /// <param name="webMeeting">Web会議情報</param>
        /// <returns></returns>
        private static async Task<string> AddWebMeetings(
                    IAsyncCollector<dynamic> documentsOut,
                    WebMeeting webMeeting
                    )
        {
            // 登録日時にUTCでの現在日時を設定
            webMeeting.RegisteredAt = DateTime.UtcNow;
            // Add a JSON document to the output container.
            string documentItem = JsonConvert.SerializeObject(webMeeting, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            await documentsOut.AddAsync(documentItem);
            return documentItem;
        }

        /// <summary>
        /// Web会議情報一覧を取得する。
        /// </summary>
        /// <param name="req">HTTPリクエスト</param>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="log">ロガー</param>
        /// <returns></returns>
        [FunctionName("GetWebMeetings")]
        public static async Task<IActionResult> GetWebMeetings(

            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "WebMeetings")] HttpRequest req,
            [CosmosDB(
                databaseName: "notify-slack-of-web-meeting-db",
                collectionName: "WebMeetings",
                ConnectionStringSetting = "CosmosDbConnectionString")
                ]DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string message = string.Empty;

            try
            {
                log.LogInformation("GET webMeetings");

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
                message = await GetWebMeetings(client, queryParameter);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }

            return new OkObjectResult($"This HTTP triggered function executed successfully.\n{message}");
        }

        /// <summary>
        /// Web会議情報一覧を取得する。
        /// </summary>
        /// <param name="client">CosmosDBのドキュメントクライアント</param>
        /// <param name="queryParameter">抽出条件パラメータ</param>
        /// <returns></returns>
        private static async Task<string> GetWebMeetings(
                   DocumentClient client,
                   WebMeetingsQueryParameter queryParameter
                   )
        {
            // Get a JSON document from the container.
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("notify-slack-of-web-meeting-db", "WebMeetings");
            IDocumentQuery<WebMeeting> query = client.CreateDocumentQuery<WebMeeting>(collectionUri, new FeedOptions { EnableCrossPartitionQuery = true, PopulateQueryMetrics = true })
                .Where(queryParameter.GetWhereExpression())
                .AsDocumentQuery();

            var documentItems = new List<WebMeeting>();
            while (query.HasMoreResults)
            {
                foreach (var documentItem in await query.ExecuteNextAsync<WebMeeting>())
                {
                    documentItems.Add(documentItem);
                }
            }
            return JsonConvert.SerializeObject(documentItems);
        }
    }
}
