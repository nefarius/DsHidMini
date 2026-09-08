using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;

using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Checks Buildbot for a newer ControlApp once per local calendar day and offers a download.
/// </summary>
public sealed class ControlAppUpdateService(
    IHttpClientFactory httpClientFactory,
    IContentDialogService contentDialogService,
    ILogger<ControlAppUpdateService>? logger = null)
{
    public const string MetadataRelativePath = "builds/DsHidMini/latest/bin/.ControlApp.exe.json";
    public const string DownloadUrl = "https://buildbot.nefarius.at/builds/DsHidMini/latest/bin/ControlApp.exe";

    public async Task CheckOnStartupAsync(CancellationToken cancellationToken = default)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        ApplicationConfiguration config = ApplicationConfiguration.Instance;

        if (!UpdateCheckPolicy.ShouldPerformNetworkCheck(
                config.IsUpdateCheckEnabled,
                today,
                config.LastUpdateCheckDate))
        {
            return;
        }

        TryRecordCheckDate(config, today);

        ArtifactMetaData? metadata;
        try
        {
            HttpClient client = httpClientFactory.CreateClient("Buildbot");
            metadata = await client.GetFromJsonAsync<ArtifactMetaData>(
                MetadataRelativePath,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger?.LogWarning(ex, "ControlApp update check failed.");
            return;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "ControlApp update check failed unexpectedly.");
            return;
        }

        if (metadata is null ||
            !UpdateCheckPolicy.TryParseFileVersion(metadata.FileVersion, out Version? remote) ||
            remote is null)
        {
            return;
        }

        if (!TryGetLocalFileVersion(out Version? local) || local is null)
        {
            return;
        }

        if (!UpdateCheckPolicy.IsRemoteNewer(remote, local))
        {
            return;
        }

        await ShowUpdateDialogAsync(remote, local).ConfigureAwait(false);
    }

    private static void TryRecordCheckDate(ApplicationConfiguration config, DateOnly today)
    {
        try
        {
            config.LastUpdateCheckDate = today;
            config.Save();
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to persist LastUpdateCheckDate.");
        }
    }

    internal static bool TryGetLocalFileVersion(string? processPath, out Version? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(processPath))
        {
            return false;
        }

        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(processPath);
            return UpdateCheckPolicy.TryParseFileVersion(info.FileVersion, out version);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to read local ControlApp file version from {Path}.", processPath);
            return false;
        }
    }

    private bool TryGetLocalFileVersion(out Version? version)
    {
        return TryGetLocalFileVersion(Environment.ProcessPath, out version);
    }

    private async Task ShowUpdateDialogAsync(Version remote, Version local)
    {
        Application? application = Application.Current;
        if (application is null)
        {
            return;
        }

        if (!application.Dispatcher.CheckAccess())
        {
            await application.Dispatcher.InvokeAsync(() => ShowUpdateDialogAsync(remote, local)).Task.Unwrap();
            return;
        }

        ContentDialogResult result = await contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "Update available",
                Content = $"A newer ControlApp ({remote}) is available. You are running {local}.",
                PrimaryButtonText = "Download",
                CloseButtonText = "Skip"
            });

        if (result == ContentDialogResult.Primary)
        {
            OpenDownload();
        }
    }

    internal static void OpenDownload()
    {
        try
        {
            Process.Start(new ProcessStartInfo(DownloadUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to open ControlApp download URL.");
        }
    }
}
