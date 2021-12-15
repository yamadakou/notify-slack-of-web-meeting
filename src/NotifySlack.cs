using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using dcinc.api;
using dcinc.api.entities;
using dcinc.api.queries;

namespace dcinc.jobs
{
    public static class NotifySlack
    {
        [FunctionName("NotifySlack")]
        public static void Run([TimerTrigger("0 0 9 * * 1-5")]TimerInfo myTimer,
            [CosmosDB(
                databaseName: "notify-slack-of-web-meeting-db",
                collectionName: "WebMeetings",
                ConnectionStringSetting = "CosmosDbConnectionString")
                ]DocumentClient client, ILogger log)
        {
            // 現在日のWeb会議情報を取得する
            var today = DateTime.UtcNow.Date.ToString("YYYY-MM-DD");
            var webMeetingsParam = new WebMeetingsQueryParameter {
                FromDate = today,
                ToDate = today
            };
            var webMeetings = WebMeetings.GetWebMeetings(client, webMeetingsParam, log);
            
            // 取得したWeb会議情報のSlackチャンネル情報を取得する
            var slackChannelIds = webMeetings.Result.Select(w => w.SlackChannelId).Distinct();
            var slackChannelParam = new SlackChannelsQueryParameter {
                Ids = string.Join(", ", slackChannelIds)
            };
            var slackChannels = SlackChannels.GetSlackChannels(client, slackChannelParam, log);

            // Slackに通知する
            


            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
