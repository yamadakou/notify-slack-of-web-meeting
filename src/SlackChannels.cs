using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using dcinc.api.entities;
using FluentValidation;

namespace dcinc.api
{
    public static class SlackChannels
    {
        [FunctionName("SlackChannels")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "notify-slack-of-web-meeting-db",
                collectionName: "SlackChannels",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string message = string.Empty;

            try {

                // メソッドにより取得処理と登録処理を切り替える。
                switch (req.Method)
                {
                    case "GET":
                        log.LogInformation("GET SlackChannels");
                        break;
                    case "POST":
                        log.LogInformation("POST SlackChannels");

                        // リクエストのBODYからパラメータ取得
                        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                        dynamic data = JsonConvert.DeserializeObject(requestBody);

                        // エンティティに設定
                        SlackChannel slackChannel = new SlackChannel()
                        {
                            Name = data?.name,
                            WebhookUrl = data?.webhookUrl,
                            RegisteredBy = data?.registeredBy
                        };

                        // 入力値チェックを行う
                        SlackChannelValidator validator = new SlackChannelValidator();
                        validator.ValidateAndThrow(slackChannel);

                        // Slackチャンネル情報を登録
                        message = await AddSlackChannels(documentsOut, slackChannel);
        
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid method: method={req.Method}");
                }
            }
            catch(Exception ex) {
                return new BadRequestObjectResult(ex);
            }

            return new OkObjectResult($"This HTTP triggered function executed successfully.\n{message}");
        }

        /// <summary>
        /// Slackチャンネル情報を登録する。
        /// </summary>
        /// <param name="documentsOut">CosmosDBのドキュメント</param>
        /// <param name="slackChannel">Slackチャンネル情報</param>
        /// <returns></returns>
        private static async Task<string> AddSlackChannels(
                    IAsyncCollector<dynamic> documentsOut,
                    SlackChannel slackChannel
                    ) {
            // 登録日時にUTCでの現在日時を設定
            slackChannel.RegisteredAt = DateTime.UtcNow;
            // Add a JSON document to the output container.
            string documentItem = JsonConvert.SerializeObject(slackChannel, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            await documentsOut.AddAsync(documentItem);
            return documentItem;
        }
    }
}
