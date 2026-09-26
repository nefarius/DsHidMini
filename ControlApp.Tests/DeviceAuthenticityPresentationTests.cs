using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;
using Nefarius.DsHidMini.IPC.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DeviceAuthenticityPresentationTests
{
    private const string GenuineDs3 =
        "00 01 04 00 08 0c 01 02 18 18 18 18 09 0a 10 11 " +
        "12 13 00 00 00 00 04 00 02 02 02 02 00 00 00 04 " +
        "04 04 04 00 00 03 00 01 02 00 00 17 00 00 00 00 " +
        "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00";

    private const string AftermarketDs3 =
        "00 01 03 00 05 0c 01 02 18 18 18 18 09 0a 10 11 " +
        "12 13 00 00 00 00 04 00 02 02 02 02 00 00 00 04 " +
        "04 04 04 00 00 02 01 02 00 64 00 17 00 00 00 00 " +
        "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00";

    [Fact]
    public void BluetoothAddress_RecognizedSonySourcedChip_SupportsAuthenticityWithoutProof()
    {
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForBluetoothAddress(
            AddressAuthenticityStatus.SonyPrefixRecognized,
            addressMissingOrSynthesized: false);

        Assert.Equal("Known Sony-sourced chip vendor", display.Summary);
        Assert.Contains("Sony typically bought from", display.Detail, StringComparison.Ordinal);
        Assert.Contains("does not own these prefixes", display.Detail, StringComparison.Ordinal);
        Assert.Contains("not proof", display.Detail, StringComparison.Ordinal);
        Assert.False(display.ShowWarning);
    }

    [Fact]
    public void BluetoothAddress_UnknownChipVendor_AssumesLikelyNotSony()
    {
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForBluetoothAddress(
            AddressAuthenticityStatus.PrefixNotRecognized,
            addressMissingOrSynthesized: false);

        Assert.Equal("Chip vendor Sony is not known to have used", display.Summary);
        Assert.Contains("cheaper chips", display.Detail, StringComparison.Ordinal);
        Assert.Contains("more likely not a Sony device", display.Detail, StringComparison.Ordinal);
        Assert.Contains("incomplete", display.Detail, StringComparison.Ordinal);
        Assert.True(display.ShowWarning);
    }

    [Fact]
    public void BluetoothAddress_LookupFailure_IsInconclusive()
    {
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForBluetoothAddress(
            AddressAuthenticityStatus.CheckUnavailable,
            addressMissingOrSynthesized: false);

        Assert.Equal("Check unavailable", display.Summary);
        Assert.Contains("inconclusive", display.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("aftermarket pad", display.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.False(display.ShowWarning);
    }

    [Fact]
    public void BluetoothAddress_SynthesizedOrMissing_IsUnavailable()
    {
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForBluetoothAddress(
            AddressAuthenticityStatus.SonyPrefixRecognized,
            addressMissingOrSynthesized: true);

        Assert.Equal("Unavailable", display.Summary);
        Assert.Contains("did not report its own Bluetooth address", display.Detail, StringComparison.Ordinal);
        Assert.False(display.ShowWarning);
    }

    [Fact]
    public void Identification_GenuineLikeDump_DoesNotProveAuthenticity()
    {
        Assert.True(DsIdentification.TryParse(ParseHex(GenuineDs3), out DsIdentificationInfo? info));
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForIdentification(
            hasRawReport: true,
            parsed: true,
            cloneHeuristic: info!.CloneHeuristic);

        Assert.False(info.CloneHeuristic);
        Assert.Equal("No known aftermarket pattern", display.Summary);
        Assert.Contains("does not prove", display.Detail, StringComparison.Ordinal);
        Assert.False(display.ShowWarning);
    }

    [Fact]
    public void Identification_KnownAftermarketDump_IsAClueNotAVerdict()
    {
        Assert.True(DsIdentification.TryParse(ParseHex(AftermarketDs3), out DsIdentificationInfo? info));
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForIdentification(
            hasRawReport: true,
            parsed: true,
            cloneHeuristic: info!.CloneHeuristic);

        Assert.True(info.CloneHeuristic);
        Assert.Equal("Known aftermarket pattern", display.Summary);
        Assert.Contains("not a verdict", display.Detail, StringComparison.Ordinal);
        Assert.True(display.ShowWarning);
    }

    [Fact]
    public void Identification_AbsentReport_IsInconclusive()
    {
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForIdentification(
            hasRawReport: false,
            parsed: false,
            cloneHeuristic: false);

        Assert.Equal("Unavailable", display.Summary);
        Assert.Contains("inconclusive", display.Detail, StringComparison.Ordinal);
        Assert.False(display.ShowWarning);
    }

    [Fact]
    public void Identification_UnreadableReport_IsInconclusive()
    {
        AuthenticityCheckDisplay display = DeviceAuthenticityPresentation.ForIdentification(
            hasRawReport: true,
            parsed: false,
            cloneHeuristic: false);

        Assert.Equal("Unreadable", display.Summary);
        Assert.Contains("inconclusive", display.Detail, StringComparison.Ordinal);
        Assert.False(display.ShowWarning);
    }

    [Fact]
    public void Disclaimer_StatesBestEffortAndIndependentChecks()
    {
        Assert.Contains("approximations, not a verdict", DeviceAuthenticityPresentation.Disclaimer, StringComparison.Ordinal);
        Assert.Contains("chip vendors Sony typically sourced", DeviceAuthenticityPresentation.Disclaimer, StringComparison.Ordinal);
        Assert.Contains("does not own those prefixes", DeviceAuthenticityPresentation.Disclaimer, StringComparison.Ordinal);
        Assert.Contains("cheaper, chip vendor", DeviceAuthenticityPresentation.Disclaimer, StringComparison.Ordinal);
        Assert.Contains("vendor/product IDs", DeviceAuthenticityPresentation.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] ParseHex(string hex)
    {
        string[] parts = hex.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        byte[] bytes = new byte[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            bytes[i] = Convert.ToByte(parts[i], 16);
        }

        return bytes;
    }
}
