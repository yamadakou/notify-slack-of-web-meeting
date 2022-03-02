using System;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
// # ※リトライ可能なHTTP要求を実現するための参考
// ## Microsoft Docs
// * IHttpClientFactory を使用して回復力の高い HTTP 要求を実装する
//   * https://docs.microsoft.com/ja-jp/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
// * IHttpClientFactory ポリシーと Polly ポリシーで指数バックオフを含む HTTP 呼び出しの再試行を実装する
//   * https://docs.microsoft.com/ja-jp/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly
// ## Blog
// * C# - HttpClientFactoryとPollyで回復力の高い何某
//   * https://dekirukigasuru.com/blog/2020/05/15/csharp-httpclientfactory-polly/
// ## Nuget
// * Microsoft.Extensions.DependencyInjection
//   * https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/3.1.22
// * Microsoft.Extensions.Http
//   * https://www.nuget.org/packages/Microsoft.Extensions.Http/3.1.22
// * Microsoft.Extensions.Http.Polly
//   * https://www.nuget.org/packages/Microsoft.Extensions.Http.Polly/3.1.22


[assembly: FunctionsStartup(typeof(dcinc.jobs.Startup))]

namespace dcinc.jobs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient("RetryHttpClient")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))  // ライフタイムを5分に設定
            .AddPolicyHandler(GetRetryPolicy());
        }

        /// <summary>
        /// リトライのポリシーを取得する
        /// </summary>
        /// <returns>リトライのポリシー</returns>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
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
}