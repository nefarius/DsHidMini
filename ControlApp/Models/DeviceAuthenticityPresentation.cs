using Nefarius.DsHidMini.ControlApp.Models.Util.Web;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     User-facing copy for one independent authenticity approximation.
/// </summary>
public readonly record struct AuthenticityCheckDisplay(string Summary, string Detail, bool ShowWarning);

/// <summary>
///     Best-effort wording for the Bluetooth chip-vendor lookup and Feature 0x01 identification check.
///     Neither result is a genuine/fake verdict.
/// </summary>
public static class DeviceAuthenticityPresentation
{
    public const string Disclaimer =
        "These two checks are independent approximations, not a verdict. " +
        "The address check looks for Bluetooth chip vendors Sony typically sourced (such as ALPS); " +
        "Sony does not own those prefixes, and aftermarket pads can copy them. " +
        "A different, usually cheaper, chip vendor is assumed more likely aftermarket. " +
        "The identification pattern only flags one known aftermarket signature. " +
        "USB vendor/product IDs and the product name are almost always copied from Sony.";

    public static AuthenticityCheckDisplay ForBluetoothAddress(
        AddressAuthenticityStatus status,
        bool addressMissingOrSynthesized)
    {
        if (addressMissingOrSynthesized)
        {
            return new AuthenticityCheckDisplay(
                "Unavailable",
                "This pad did not report its own Bluetooth address, so the chip vendor cannot be checked. That is inconclusive.",
                false);
        }

        return status switch
        {
            AddressAuthenticityStatus.SonyPrefixRecognized => new AuthenticityCheckDisplay(
                "Known Sony-sourced chip vendor",
                "The first three bytes of the Bluetooth address match a chip manufacturer Sony typically bought from, such as ALPS. Sony does not own these prefixes. Aftermarket pads can copy a genuine address, so this supports authenticity but is not proof.",
                false),
            AddressAuthenticityStatus.PrefixNotRecognized => new AuthenticityCheckDisplay(
                "Chip vendor Sony is not known to have used",
                "The first three bytes of the Bluetooth address belong to a chip manufacturer Sony is not known to have sourced. Aftermarket makers usually pick cheaper chips, so this is more likely not a Sony device. The list can also be incomplete.",
                true),
            _ => new AuthenticityCheckDisplay(
                "Check unavailable",
                "The chip-vendor list could not be loaded. This is inconclusive, not evidence of an aftermarket pad.",
                false)
        };
    }

    public static AuthenticityCheckDisplay ForIdentification(
        bool hasRawReport,
        bool parsed,
        bool cloneHeuristic)
    {
        if (!hasRawReport)
        {
            return new AuthenticityCheckDisplay(
                "Unavailable",
                "No identification report is available. Bluetooth pads only publish this after a prior USB connection. This is inconclusive.",
                false);
        }

        if (!parsed)
        {
            return new AuthenticityCheckDisplay(
                "Unreadable",
                "The identification report could not be parsed. This is inconclusive, not evidence of an aftermarket pad.",
                false);
        }

        if (cloneHeuristic)
        {
            return new AuthenticityCheckDisplay(
                "Known aftermarket pattern",
                "The identification report matches a known aftermarket signature. That is a strong clue, not a verdict.",
                true);
        }

        return new AuthenticityCheckDisplay(
            "No known aftermarket pattern",
            "The identification report does not match the known aftermarket signature. That does not prove the pad is genuine.",
            false);
    }
}
