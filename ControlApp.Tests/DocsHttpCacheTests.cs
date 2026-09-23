using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;

using Polly;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DocsHttpCacheTests
{
    private const string OuiDatabasePath = "/projects/DsHidMini/genuine_oui_db.json";

    private const string OuiDatabaseJson = "[\"00:11:22\"]";

    private static readonly PhysicalAddress GenuineAddress = PhysicalAddress.Parse("00-11-22-33-44-55");

    private static readonly PhysicalAddress UnknownAddress = PhysicalAddress.Parse("AA-BB-CC-DD-EE-FF");

    [Fact]
    public async Task IsGenuineAddress_UsesCachedDatabaseWhileOffline()
    {
        await using CacheHost host = await CacheHost.StartAsync(TimeSpan.FromHours(1), call =>
        {
            if (call == 1)
            {
                return Json(OuiDatabaseJson);
            }

            throw new HttpRequestException("offline");
        });

        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.False(await host.Validator.IsGenuineAddress(UnknownAddress));
        Assert.Equal(1, host.Handler.Calls);
    }

    [Fact]
    public async Task IsGenuineAddress_ServesExpiredDatabaseWhenRefreshFails()
    {
        await using CacheHost host = await CacheHost.StartAsync(TimeSpan.FromMilliseconds(200), call =>
        {
            if (call == 1)
            {
                return Json(OuiDatabaseJson);
            }

            throw new HttpRequestException("offline");
        });

        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.Equal(1 + CacheHost.RetryAttempts, host.Handler.Calls);
    }

    [Fact]
    public async Task IsGenuineAddress_DoesNotCacheFailedDatabaseDownload()
    {
        await using CacheHost host = await CacheHost.StartAsync(TimeSpan.FromHours(1), call =>
        {
            if (call <= CacheHost.RetryAttempts)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            return Json(OuiDatabaseJson);
        });

        Assert.False(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.Equal(CacheHost.RetryAttempts + 1, host.Handler.Calls);
    }

    [Fact]
    public async Task IsGenuineAddress_DoesNotReuseUnrelatedCachedResponse()
    {
        await using CacheHost host = await CacheHost.StartAsync(TimeSpan.FromHours(1), (request, call) =>
        {
            if (request.RequestUri?.AbsolutePath == "/unrelated")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("not-the-database")
                };
            }

            if (call == 2)
            {
                return Json(OuiDatabaseJson);
            }

            throw new HttpRequestException("unexpected request");
        });

        // global:: avoids the Nefarius.HttpClient namespace introduced by the cache package.
        using global::System.Net.Http.HttpClient client = host.Factory.CreateClient(DocsHttpClient.Name);
        using HttpResponseMessage unrelated = await client.GetAsync("/unrelated");
        Assert.True(unrelated.IsSuccessStatusCode);

        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.Equal(2, host.Handler.Calls);
        Assert.Equal(OuiDatabasePath, host.Handler.RequestPaths[1]);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("[\"00112\"]")]
    [InlineData("[\"00:11:22\",\"00112\"]")]
    public async Task IsGenuineAddress_DoesNotReuseRejectedDatabase(string rejectedJson)
    {
        await using CacheHost host = await CacheHost.StartAsync(TimeSpan.FromHours(1), call =>
            call == 1 ? Json(rejectedJson) : Json(OuiDatabaseJson));

        Assert.False(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.Equal(2, host.Handler.Calls);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("[\"00112\"]")]
    [InlineData("[\"00:11:22\",\"00112\"]")]
    public async Task IsGenuineAddress_KeepsValidSnapshotWhenRefreshIsRejected(string rejectedJson)
    {
        await using CacheHost host = await CacheHost.StartAsync(TimeSpan.FromMilliseconds(200), call =>
            call == 1 ? Json(OuiDatabaseJson) : Json(rejectedJson));

        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.Equal(2, host.Handler.Calls);
    }

    [Fact]
    public async Task AddDocsHttpClient_SkipsCacheWhenDirectoryCannotBeCreated()
    {
        string root = Path.Combine(Path.GetTempPath(), "DsHidMini-docs-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string blockingFile = Path.Combine(root, "blocked");
        await File.WriteAllTextAsync(blockingFile, "not a directory");
        string databasePath = Path.Combine(blockingFile, "docs-http-cache.db");

        await using CacheHost host = await CacheHost.StartAsync(
            TimeSpan.FromHours(1),
            call =>
            {
                if (call == 1)
                {
                    return Json(OuiDatabaseJson);
                }

                throw new HttpRequestException("offline");
            },
            databasePath,
            root);

        Assert.True(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.False(await host.Validator.IsGenuineAddress(GenuineAddress));
        Assert.Equal(1 + CacheHost.RetryAttempts, host.Handler.Calls);
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class CacheHost : IAsyncDisposable
    {
        public const int RetryAttempts = 3;

        private readonly IHost _host;
        private readonly string _directory;

        private CacheHost(IHost host, ScriptedHandler handler, string directory)
        {
            _host = host;
            _directory = directory;
            Handler = handler;
        }

        public ScriptedHandler Handler { get; }

        public AddressValidator Validator => _host.Services.GetRequiredService<AddressValidator>();

        public IHttpClientFactory Factory => _host.Services.GetRequiredService<IHttpClientFactory>();

        public static async Task<CacheHost> StartAsync(
            TimeSpan cacheLifetime,
            Func<int, HttpResponseMessage> respond,
            string? databasePath = null,
            string? cleanupDirectory = null)
        {
            return await StartAsync(
                cacheLifetime,
                (_, call) => respond(call),
                databasePath,
                cleanupDirectory);
        }

        public static async Task<CacheHost> StartAsync(
            TimeSpan cacheLifetime,
            Func<HttpRequestMessage, int, HttpResponseMessage> respond,
            string? databasePath = null,
            string? cleanupDirectory = null)
        {
            string directory = cleanupDirectory
                               ?? Path.Combine(Path.GetTempPath(), "DsHidMini-docs-cache-" + Guid.NewGuid().ToString("N"));
            databasePath ??= Path.Combine(directory, "docs-http-cache.db");
            ScriptedHandler handler = new(respond);

            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDocsHttpClient(
                        "ControlApp.Tests",
                        databasePath,
                        cacheLifetime,
                        () => handler,
                        options =>
                        {
                            options.MaxRetryAttempts = RetryAttempts - 1;
                            options.Delay = TimeSpan.Zero;
                            options.BackoffType = DelayBackoffType.Constant;
                            options.UseJitter = false;
                        });
                    services.AddSingleton<AddressValidator>();
                })
                .Build();

            await host.StartAsync();
            return new CacheHost(host, handler, directory);
        }

        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();

            for (int attempt = 0; attempt < 5 && Directory.Exists(_directory); attempt++)
            {
                try
                {
                    Directory.Delete(_directory, recursive: true);
                }
                catch (IOException) when (attempt < 4)
                {
                    await Task.Delay(50);
                }
            }
        }
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _respond;
        private readonly List<string> _requestPaths = new();

        public ScriptedHandler(Func<HttpRequestMessage, int, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public int Calls => _requestPaths.Count;

        public IReadOnlyList<string> RequestPaths => _requestPaths;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _requestPaths.Add(request.RequestUri?.AbsolutePath ?? "");
            return Task.FromResult(_respond(request, Calls));
        }
    }
}
