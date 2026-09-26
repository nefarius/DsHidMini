namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Pure policy for the first-run donation prompt ported from DSHMC. The prompt stays
///     dismissed only when the user ticks that they already donated or will consider it.
/// </summary>
public static class DonationPromptPolicy
{
    public const string DonationsUrl = "https://docs.nefarius.at/Donations/";

    public static bool ShouldShow(bool hasAcknowledged) => !hasAcknowledged;

    public static bool ShouldAcknowledge(bool alreadyDonatedOrWillConsider) => alreadyDonatedOrWillConsider;

    public static bool ShouldOpenDonations(bool showHow) => showHow;
}
