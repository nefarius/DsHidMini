#nullable enable

namespace Nefarius.DsHidMini.Setup;

/// <summary>
///     Launch decision for the post-install article. Extracted so the custom action
///     can keep talking to the MSI session while tests assert the same rules.
/// </summary>
internal static class OpenArticleDecision
{
    /// <summary>
    ///     Full ManagedUI honors <c>PostInstArticle</c>. Reduced/basic/suppressed UI
    ///     (no <c>WIXSHARP_MANAGED_UI_HANDLE</c>) always launches.
    /// </summary>
    internal static bool ShouldLaunch(string? managedUiHandle, bool articleFeatureEnabled)
    {
        bool managedUiDisplayed = !string.IsNullOrWhiteSpace(managedUiHandle);
        return !managedUiDisplayed || articleFeatureEnabled;
    }
}
