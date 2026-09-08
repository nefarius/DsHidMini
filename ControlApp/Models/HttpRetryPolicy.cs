using System.Net;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Polly;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Shared retry rules for ControlApp HTTP clients. Covers typical transient
///     failures and 404, which Buildbot can return briefly for the latest JSON.
/// </summary>
internal static class HttpRetryPolicy
{
    public static IHttpClientBuilder AddCommonRetryPolicy(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("common-retry", pipeline =>
        {
            pipeline.AddRetry(CreateOptions());
        });

        return builder;
    }

    public static HttpRetryStrategyOptions CreateOptions()
    {
        return new HttpRetryStrategyOptions
        {
            ShouldHandle = args => ValueTask.FromResult(
                ShouldRetry(args.Outcome.Result?.StatusCode, args.Outcome.Exception))
        };
    }

    public static bool ShouldRetry(HttpStatusCode? statusCode, Exception? exception)
    {
        if (exception is HttpRequestException)
        {
            return true;
        }

        return statusCode is HttpStatusCode.NotFound
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or >= HttpStatusCode.InternalServerError;
    }
}
