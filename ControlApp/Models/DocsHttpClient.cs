using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Nefarius.DsHidMini.ControlApp.Models.Util.Web;
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

    public const string OuiDatabasePath = "/projects/DsHidMini/genuine_oui_db.json";

    public static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    public static string? GetCacheDatabasePath()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DsHidMini",
            "Cache");
        if (!TryEnsureCacheDirectory(directory))
        {
            return null;
        }

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
        string? databasePath = cacheDatabasePath ?? GetCacheDatabasePath();
        string? cacheDirectory = databasePath is null ? null : Path.GetDirectoryName(databasePath);
        bool useCache = databasePath is not null && TryEnsureCacheDirectory(cacheDirectory);

        IHttpClientBuilder builder = services.AddHttpClient(Name, client =>
        {
            client.BaseAddress = new Uri("https://docs.nefarius.at/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
        });

        if (primaryHandlerFactory is not null)
        {
            builder.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);
        }

        if (useCache && databasePath is not null)
        {
            string cacheDatabase = databasePath;
            // The cache handler is registered before retry so it stays outside the retry pipeline.
            // A refresh therefore runs the retry policy to completion before stale data is returned.
            // The OUI guard sits inside the cache, so a rejected document is not stored and cannot
            // replace the last valid snapshot.
            builder.AddLiteDbCache(options =>
            {
                options.ConnectionString = cacheDatabase;
                options.CollectionName = CacheCollectionName;
                options.EntryOptions.AbsoluteExpirationRelativeToNow = cacheLifetime ?? CacheLifetime;
                options.EntryOptions.ServeStaleOnError = true;
            });
            builder.AddHttpMessageHandler(() => new GenuineOuiDatabaseHandler());
        }

        builder.AddResilienceHandler("common-retry", pipeline =>
        {
            HttpRetryStrategyOptions options = HttpRetryPolicy.CreateOptions();
            configureRetry?.Invoke(options);
            pipeline.AddRetry(options);
        });

        return builder;
    }

    private static bool TryEnsureCacheDirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            return true;
        }

        try
        {
            Directory.CreateDirectory(directory);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Warning(ex,
                "Unable to create the Docs HTTP cache directory {CacheDirectory}. Continuing without a response cache.",
                directory);
            return false;
        }
    }

    /// <summary>
    ///     Turns a successful genuine-OUI response into a non-success result when its body cannot be kept.
    ///     The disk cache then skips it and keeps the previous valid snapshot.
    /// </summary>
    private sealed class GenuineOuiDatabaseHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!IsOuiDatabaseRequest(request) || !response.IsSuccessStatusCode)
            {
                return response;
            }

            if (response.Content is null)
            {
                response.StatusCode = HttpStatusCode.UnprocessableEntity;
                response.Content = new StringContent(string.Empty);
                return response;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            HttpContent originalContent = response.Content;
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
            originalContent.Dispose();

            if (IsValidOuiDatabase(body))
            {
                return response;
            }

            Log.Logger.Warning(
                "Rejected genuine OUI database response because it was malformed, null, or contained an invalid OUI.");
            response.StatusCode = HttpStatusCode.UnprocessableEntity;
            return response;
        }

        private static bool IsOuiDatabaseRequest(HttpRequestMessage request)
        {
            return string.Equals(
                request.RequestUri?.AbsolutePath,
                OuiDatabasePath,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidOuiDatabase(string body)
        {
            try
            {
                IList<string>? entries = JsonSerializer.Deserialize<IList<string>>(body);
                if (entries is null)
                {
                    return false;
                }

                foreach (string entry in entries)
                {
                    _ = new OUIEntry(entry);
                }

                return true;
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException)
            {
                return false;
            }
        }
    }
}
