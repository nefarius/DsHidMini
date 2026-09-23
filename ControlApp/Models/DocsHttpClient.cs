using System.IO;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Nefarius.HttpClient.LiteDbCache;

using Polly;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Named HTTP client for docs.nefarius.at, including the genuine MAC database.
/// </summary>
internal static class DocsHttpClient
{
    public const string Name = "Docs";

    public const string CacheCollectionName = "docs_response_cache";

    public static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    public static string GetCacheDatabasePath()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DsHidMini",
            "Cache");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "docs-http-cache.db");
    }

    /// <summary>
    ///     Registers the Docs client with retry inside the disk cache. An expired entry is
    ///     refreshed only after the retry policy is exhausted, and that entry is then served
    ///     when the refresh fails.
    /// </summary>
    public static IHttpClientBuilder AddDocsHttpClient(
        this IServiceCollection services,
        string applicationName,
        string? cacheDatabasePath = null,
        TimeSpan? cacheLifetime = null,
        Func<HttpMessageHandler>? primaryHandlerFactory = null,
        Action<HttpRetryStrategyOptions>? configureRetry = null)
    {
        string databasePath = cacheDatabasePath ?? GetCacheDatabasePath();
        string? databaseDirectory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        IHttpClientBuilder builder = services.AddHttpClient(Name, client =>
        {
            client.BaseAddress = new Uri("https://docs.nefarius.at/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
        });

        if (primaryHandlerFactory is not null)
        {
            builder.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);
        }

        // The cache handler is registered before retry so it stays outside the retry pipeline.
        // A refresh therefore runs the retry policy to completion before stale data is returned.
        builder.AddLiteDbCache(options =>
        {
            options.ConnectionString = databasePath;
            options.CollectionName = CacheCollectionName;
            options.EntryOptions.AbsoluteExpirationRelativeToNow = cacheLifetime ?? CacheLifetime;
            options.EntryOptions.ServeStaleOnError = true;
        });

        builder.AddResilienceHandler("common-retry", pipeline =>
        {
            HttpRetryStrategyOptions options = HttpRetryPolicy.CreateOptions();
            configureRetry?.Invoke(options);
            pipeline.AddRetry(options);
        });

        return builder;
    }
}
