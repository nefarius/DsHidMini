using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.DsHidMini.IPC.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class Ds3IdentityAdapterCapabilityTests
{
    [Fact]
    public void Ds3IdentityAdapter_EnumValue_IsSix()
    {
        Assert.Equal(6, (int)DsDeviceType.Ds3IdentityAdapter);
    }

    [Fact]
    public void Ds3IdentityAdapter_HasRumbleAndNoLedsOrBluetooth()
    {
        Assert.True(DsDeviceCapabilities.HasRumble(DsDeviceType.Ds3IdentityAdapter));
        Assert.False(DsDeviceCapabilities.HasLeds(DsDeviceType.Ds3IdentityAdapter));
        Assert.False(DsDeviceCapabilities.HasSingleLed(DsDeviceType.Ds3IdentityAdapter));
        Assert.False(DsDeviceCapabilities.SupportsBluetooth(DsDeviceType.Ds3IdentityAdapter));
        Assert.Equal(
            "PS1/PS2 USB Adapter (Ejoyous, DualShock 3 identity)",
            DsDeviceCapabilities.DisplayName(DsDeviceType.Ds3IdentityAdapter));
        Assert.Contains("fabricated", DsDeviceCapabilities.HidModeGuidance(DsDeviceType.Ds3IdentityAdapter));
        Assert.Empty(DsDeviceCapabilities.OutputStallGuidance(DsDeviceType.Ds3IdentityAdapter));
    }

    [Fact]
    public void FromHardwareIds_SonySixaxis_StaysSixaxis()
    {
        // Old drivers and ControlApp fallbacks cannot see bMaxPacketSize0.
        Assert.Equal(
            DsDeviceType.Sixaxis,
            DsDeviceCapabilities.FromHardwareIds(DsDeviceCapabilities.SonyVendorId,
                DsDeviceCapabilities.SixaxisProductId));
    }

    [Fact]
    public void SettingsEditor_Ds3IdentityAdapterHidesLedsAndWirelessAndKeepsRumble()
    {
        SettingsEditorViewModel editor = new();
        editor.ApplyDeviceCapabilities(DsDeviceType.Ds3IdentityAdapter);

        Assert.False(editor.HideRumbleSettings);
        Assert.True(editor.GeneralRumbleSettingsVM.IsGroupVisible);
        Assert.False(editor.LedsSettingsVM.IsGroupVisible);
        Assert.False(editor.WirelessSettingsVM.IsGroupVisible);
        Assert.True(editor.HasDeviceCapabilityNote);
        Assert.Equal(
            "PS1/PS2 USB Adapter (Ejoyous, DualShock 3 identity)",
            editor.DeviceTypeDisplay);
    }

    [Theory]
    [InlineData(DsDeviceType.Ds3IdentityAdapter, unchecked((int)0xC00000B5), false)]
    [InlineData(DsDeviceType.Ds3IdentityAdapter, 0, false)]
    [InlineData(DsDeviceType.Ds3IdentityAdapter, null, false)]
    public void IsOutputReportStalled_Ds3IdentityAdapterNeverStalls(
        DsDeviceType type,
        int? outputReportStatus,
        bool expected)
    {
        Assert.Equal(expected, DsDeviceCapabilities.IsOutputReportStalled(type, outputReportStatus));
    }
}
