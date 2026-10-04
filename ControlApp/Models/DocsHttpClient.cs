using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Nefarius.DsHidMini.ControlApp.Models.Util.Web;

using Polly;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Named HTTP client for docs.nefarius.at, including the genuine MAC database.
/// </summary>
internal static class DocsHttpClient
{
    public const string Name = "Docs";

    public const string OuiDatabasePath = "/projects/DsHidMini/genuine_oui_db.json";

    public static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    public static string? GetCacheFilePath()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DsHidMini",
            "Cache");
        if (!TryEnsureCacheDirectory(directory))
        {
            return null;
        }

        return Path.Combine(directory, "genuine-oui-db.json");
    }

    /// <summary>
    ///     Registers the Docs client with retry inside the OUI file cache. An expired
    ///     snapshot is refreshed only after the retry policy is exhausted, and that
    ///     snapshot is then served when the refresh fails.
    /// </summary>
    public static IHttpClientBuilder AddDocsHttpClient(
        this IServiceCollection services,
        string applicationName,
        string? cacheFilePath = null,
        TimeSpan? cacheLifetime = null,
        Func<HttpMessageHandler>? primaryHandlerFactory = null,
        Action<HttpRetryStrategyOptions>? configureRetry = null)
    {
        string? filePath = cacheFilePath ?? GetCacheFilePath();
        string? cacheDirectory = filePath is null ? null : Path.GetDirectoryName(filePath);
        bool useCache = filePath is not null && TryEnsureCacheDirectory(cacheDirectory);

        IHttpClientBuilder builder = services.AddHttpClient(Name, client =>
        {
            client.BaseAddress = new Uri("https://docs.nefarius.at/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
        });

        if (primaryHandlerFactory is not null)
        {
            builder.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);
        }

        if (useCache && filePath is not null)
        {
            string cacheFile = filePath;
            TimeSpan lifetime = cacheLifetime ?? CacheLifetime;
            // The cache handler is registered before retry so it stays outside the retry pipeline.
            // A refresh therefore runs the retry policy to completion before stale data is returned.
            // Validation happens before the file is written, so a rejected document cannot replace
            // the last valid snapshot.
            builder.AddHttpMessageHandler(() => new GenuineOuiFileCacheHandler(cacheFile, lifetime));
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

    internal static bool IsValidOuiDatabase(string body)
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

    /// <summary>
    ///     Caches only the genuine OUI JSON. Fresh hits skip the network; expired
    ///     snapshots are kept until a validated refresh succeeds.
    /// </summary>
    private sealed class GenuineOuiFileCacheHandler(string cacheFilePath, TimeSpan lifetime) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!IsOuiDatabaseRequest(request))
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            if (TryReadCache(out string? cached, out bool fresh) && fresh && cached is not null)
            {
                return Success(cached);
            }

            HttpResponseMessage? response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (TryServeStale(out HttpResponseMessage? stale))
                {
                    Log.Logger.Warning(ex,
                        "Genuine OUI database refresh failed; serving the last valid cached snapshot.");
                    return stale;
                }

                throw;
            }

            if (response.IsSuccessStatusCode)
            {
                string? body = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
                if (body is not null && IsValidOuiDatabase(body))
                {
                    TryWriteCache(body);
                    response.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    return response;
                }

                Log.Logger.Warning(
                    "Rejected genuine OUI database response because it was malformed, null, or contained an invalid OUI.");
            }

            if (TryServeStale(out HttpResponseMessage? staleSnapshot))
            {
                response.Dispose();
                return staleSnapshot;
            }

            if (response.IsSuccessStatusCode)
            {
                response.StatusCode = HttpStatusCode.UnprocessableEntity;
            }

            return response;
        }

        private static bool IsOuiDatabaseRequest(HttpRequestMessage request)
        {
            return string.Equals(
                request.RequestUri?.AbsolutePath,
                OuiDatabasePath,
                StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<string?> ReadBodyAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.Content is null)
            {
                return null;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            HttpContent originalContent = response.Content;
            originalContent.Dispose();
            return body;
        }

        private static HttpResponseMessage Success(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private bool TryServeStale(out HttpResponseMessage stale)
        {
            if (TryReadCache(out string? cached, out _) && cached is not null)
            {
                stale = Success(cached);
                return true;
            }

            stale = null!;
            return false;
        }

        private bool TryReadCache(out string? body, out bool fresh)
        {
            body = null;
            fresh = false;

            try
            {
                if (!File.Exists(cacheFilePath))
                {
                    return false;
                }

                CacheEnvelope? envelope = JsonSerializer.Deserialize<CacheEnvelope>(File.ReadAllText(cacheFilePath));
                if (envelope?.Body is null || !IsValidOuiDatabase(envelope.Body))
                {
                    return false;
                }

                fresh = DateTime.UtcNow - envelope.CachedAtUtc < lifetime;
                body = envelope.Body;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                Log.Logger.Warning(ex, "Unable to read the genuine OUI cache file {CacheFile}.", cacheFilePath);
                return false;
            }
        }

        private void TryWriteCache(string body)
        {
            string? directory = Path.GetDirectoryName(cacheFilePath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            string tempPath = Path.Combine(directory,
                Path.GetFileName(cacheFilePath) + "." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                if (!TryEnsureCacheDirectory(directory))
                {
                    return;
                }

                CacheEnvelope envelope = new()
                {
                    CachedAtUtc = DateTime.UtcNow,
                    Body = body
                };
                File.WriteAllText(tempPath, JsonSerializer.Serialize(envelope));
                File.Move(tempPath, cacheFilePath, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Logger.Warning(ex, "Unable to write the genuine OUI cache file {CacheFile}.", cacheFilePath);
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (Exception cleanupEx) when (cleanupEx is IOException or UnauthorizedAccessException)
                {
                    Log.Logger.Debug(cleanupEx, "Unable to delete leftover OUI cache temp file {TempFile}.", tempPath);
                }
            }
        }

        private sealed class CacheEnvelope
        {
            public DateTime CachedAtUtc { get; set; }

            public string? Body { get; set; }
        }
    }
}
