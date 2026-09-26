using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using Nefarius.DsHidMini.ControlApp.Models;

using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Outcome of one donation prompt: whether the user asked for the donations page, and
///     whether they asked not to be shown the prompt again.
/// </summary>
internal readonly record struct DonationPromptResult(bool ShowHow, bool AlreadyDonatedOrWillConsider);

/// <summary>
///     Shows the DSHMC donation prompt once ControlApp's main window is up, until the user
///     ticks that they already donated or will consider it.
/// </summary>
public sealed class DonationPromptService
{
    private readonly ApplicationConfiguration _config;
    private readonly IContentDialogService? _contentDialogService;
    private readonly Action _persist;

    public DonationPromptService(
        IContentDialogService? contentDialogService = null,
        ApplicationConfiguration? config = null,
        Action? persist = null)
    {
        _contentDialogService = contentDialogService;
        _config = config ?? ApplicationConfiguration.Instance;
        _persist = persist ?? (() => _config.Save());
    }

    /// <summary>
    ///     Test seam that replaces the live content dialog.
    /// </summary>
    internal Func<Task<DonationPromptResult>>? ShowPromptOverride { get; set; }

    /// <summary>
    ///     Test seam that replaces opening the donations page in the default browser.
    /// </summary>
    internal Action<string>? OpenUrlOverride { get; set; }

    public async Task ShowIfNeededAsync()
    {
        if (!DonationPromptPolicy.ShouldShow(_config.HasAcknowledgedDonationDialog))
        {
            return;
        }

        DonationPromptResult result = ShowPromptOverride is { } showPrompt
            ? await showPrompt().ConfigureAwait(true)
            : await ShowPromptAsync().ConfigureAwait(true);

        if (DonationPromptPolicy.ShouldOpenDonations(result.ShowHow))
        {
            OpenDonations();
        }

        if (!DonationPromptPolicy.ShouldAcknowledge(result.AlreadyDonatedOrWillConsider))
        {
            return;
        }

        _config.HasAcknowledgedDonationDialog = true;
        try
        {
            _persist();
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to persist donation prompt acknowledgement.");
        }
    }

    private void OpenDonations()
    {
        if (OpenUrlOverride is { } openUrl)
        {
            openUrl(DonationPromptPolicy.DonationsUrl);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(DonationPromptPolicy.DonationsUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to open donations URL.");
        }
    }

    private async Task<DonationPromptResult> ShowPromptAsync()
    {
        Application? application = Application.Current;
        if (application is null)
        {
            return default;
        }

        if (!application.Dispatcher.CheckAccess())
        {
            return await application.Dispatcher.InvokeAsync(ShowPromptAsync).Task.Unwrap();
        }

        System.Windows.Controls.CheckBox alreadyDonated = new()
        {
            Content = "I've already donated or will consider it",
            Margin = new Thickness(0, 16, 0, 0)
        };

        StackPanel content = new();
        content.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text =
                "Hello, Gamer!\n\nDid you know this project was only possible with years of dedication and enthusiasm? " +
                "You're receiving this work for absolutely free. If it brings you joy please consider giving back to the " +
                "author's efforts and show your appreciation through a donation.\n\nThanks for your attention ❤️",
            TextWrapping = TextWrapping.Wrap
        });
        content.Children.Add(alreadyDonated);

        if (_contentDialogService is null)
        {
            return default;
        }

        ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "May I have your attention",
                Content = content,
                PrimaryButtonText = "Sure, show me how!",
                CloseButtonText = "Acknowledged"
            });

        return new DonationPromptResult(
            result == ContentDialogResult.Primary,
            alreadyDonated.IsChecked == true);
    }
}
