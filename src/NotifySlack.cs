using dcinc.api;
using dcinc.api.queries;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace dcinc.jobs
{
    public class NotifySlack
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotifySlack> _logger;
        private readonly ILogger<WebMeetings> _webMeetingslogger;
        private readonly ILogger<SlackChannels> _slackChannelslogger;

        public NotifySlack(IHttpClientFactory httpClientFactory
            , ILogger<NotifySlack> logger, ILogger<WebMeetings> webMeetingslogger, ILogger<SlackChannels> slackChannelslogger)
        {
            _httpClient = httpClientFactory.CreateClient("RetryHttpClient");
            _logger = logger;
            _webMeetingslogger = webMeetingslogger;
            _slackChannelslogger = slackChannelslogger;
        }

        // 参考：https://docs.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions
        // RELEASE："0 0 9 * * 1-5"
        // DEBUG："0 */5 * * * *"
        // アプリケーション設定：WEBSITE_TIME_ZONE=Tokyo Standard Time
        [Function("NotifySlack")]
        public async Task Run([TimerTrigger("0 0 9 * * 1-5")]TimerInfo myTimer,
            [CosmosDBInput(
                databaseName: "notify-slack-of-web-meeting-db",
                containerName: "WebMeetings",
                Connection = "CosmosDbConnectionString")
                ]CosmosClient client)
        {
            // 現在日のWeb会議情報を取得する
            // ※アプリケーション設定でタイムゾーンを指定するため、ローカルの現在日を取得
            var today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            var webMeetingsParam = new WebMeetingsQueryParameter {
                FromDate = today,
                ToDate = today
            };
            var webMeetingsObject = new WebMeetings(_webMeetingslogger);
            var webMeetings =  await webMeetingsObject.GetWebMeetings(client, webMeetingsParam);

            _logger.LogInformation($"Notify count: {webMeetings.Count()}");
            if(!webMeetings.Any())
            {
                return;
            }
            var webMeetingsBySlackChannelMap = webMeetings.OrderBy(w => w.StartDateTime).GroupBy(w => w.SlackChannelId)
                                                        .ToDictionary(g => g.Key, ws => ws.OrderBy(w => w.StartDateTime).ToList());
            
            // 取得したWeb会議情報のSlackチャンネル情報を取得する
            var slackChannelIds = webMeetingsBySlackChannelMap.Keys;
            _logger.LogInformation($"Slack channels count: {slackChannelIds.Count()}");
            if(!slackChannelIds.Any())
            {
                return;
            }
            var slackChannelParam = new SlackChannelsQueryParameter {
                Ids = string.Join(", ", slackChannelIds)
            };
            var SlackChannelsObject = new SlackChannels(_slackChannelslogger);
            var slackChannels = await SlackChannelsObject.GetSlackChannels(client, slackChannelParam);

            // Slackに通知する
            foreach (var slackChannel in slackChannels)
            {
                var message = new StringBuilder($"{DateTime.Today.ToString("yyyy/MM/dd")}のWeb会議情報\n");
                foreach(var webMeeting in webMeetingsBySlackChannelMap[slackChannel.Id])
                {
                    message.AppendLine($"{webMeeting.StartDateTime.ToString("HH:mm")}～：<{webMeeting.Url}|{webMeeting.Name}>");
                }
                _logger.LogInformation(slackChannel.WebhookUrl);
                _logger.LogInformation(message.ToString());
                var content = new StringContent(JsonSerializer.Serialize(new {text = message.ToString()}), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(slackChannel.WebhookUrl, content);

                _logger.LogInformation(response.ToString());
                
                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // 通知したWeb会議情報を削除する
                    await webMeetingsObject.DeleteWebMeetingById(client, string.Join(",", webMeetingsBySlackChannelMap[slackChannel.Id].Select(w => w.Id)));
                }
            }

            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
