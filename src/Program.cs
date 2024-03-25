using dcinc.jobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly.Extensions.Http;
using Polly;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    //.ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.Configure<LoggerFilterOptions>(options =>
        {
            // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
            // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
            LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (toRemove is not null)
            {
                options.Rules.Remove(toRemove);
            }
        });
        services.Configure<CosmosLinqSerializerOptions>(options =>
        {
            // プロパティ名の最初の文字は小文字
            options.PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase;
        });
        services.AddHttpClient("RetryHttpClient")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))  // ライフタイムを5分に設定
            .AddPolicyHandler(Configuration.GetRetryPolicy());
    })
    .Build();

host.Run();


internal class Configuration
{
    /// <summary>
    /// リトライのポリシーを取得する
    /// </summary>
    /// <returns>リトライのポリシー</returns>
    internal static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        // 指数関数的再試行で (最初は 2 秒) 6 回試すポリシー
        // リトライ対象となる条件は以下
        // * Network failures (as HttpRequestException)
        // * HTTP 5XX status codes (server errors)
        // * HTTP 408 status code (request timeout)
        // * HTTP 429 status code (too many requests)
        // ※参考
        // https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory#using-addtransienthttperrorpolicy
        // https://github.com/App-vNext/Polly.Extensions.Http/blob/master/src/Polly.Extensions.Http/HttpPolicyExtensions.cs#L17-L28
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
