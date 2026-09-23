using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.DsHidMini.IPC.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class ThirdPartyHidCapabilityTests
{
    [Theory]
    [InlineData((ushort)0x2563, (ushort)0x0575, DsDeviceType.ThirdPartyHid)]
    [InlineData((ushort)0x2563, (ushort)0x0268, DsDeviceType.Unknown)]
    [InlineData((ushort)0x054C, (ushort)0x0575, DsDeviceType.Unknown)]
    public void FromHardwareIds_MapsShanWanAdapter(ushort vendorId, ushort productId, DsDeviceType expected)
    {
        Assert.Equal(expected, DsDeviceCapabilities.FromHardwareIds(vendorId, productId));
    }

    [Theory]
    [InlineData(@"USB\VID_2563&PID_0575\6&4B29C3C&0&2")]
    [InlineData(@"USB\VID_2563&PID_0575&REV_0100")]
    public void FromInstanceId_ShanWanAdapter_IsThirdPartyHid(string instanceId)
    {
        Assert.Equal(DsDeviceType.ThirdPartyHid, DsDeviceCapabilities.FromInstanceId(instanceId));
    }

    [Fact]
    public void ThirdPartyHid_HasRumbleAndNoLedsOrBluetooth()
    {
        Assert.True(DsDeviceCapabilities.HasRumble(DsDeviceType.ThirdPartyHid));
        Assert.False(DsDeviceCapabilities.HasLeds(DsDeviceType.ThirdPartyHid));
        Assert.False(DsDeviceCapabilities.HasSingleLed(DsDeviceType.ThirdPartyHid));
        Assert.False(DsDeviceCapabilities.SupportsBluetooth(DsDeviceType.ThirdPartyHid));
        Assert.Equal("PS1/PS2 USB Adapter (ShanWan)", DsDeviceCapabilities.DisplayName(DsDeviceType.ThirdPartyHid));
        Assert.Contains("neutral", DsDeviceCapabilities.HidModeGuidance(DsDeviceType.ThirdPartyHid));
        Assert.Contains("paired", DsDeviceCapabilities.OutputStallGuidance(DsDeviceType.ThirdPartyHid));
    }

    [Theory]
    [InlineData(DsDeviceType.Unknown)]
    [InlineData(DsDeviceType.Sixaxis)]
    [InlineData(DsDeviceType.Navigation)]
    [InlineData(DsDeviceType.Motion)]
    [InlineData(DsDeviceType.Wireless)]
    public void OutputStallGuidance_IsEmptyExceptThirdPartyHid(DsDeviceType type)
    {
        Assert.Equal(string.Empty, DsDeviceCapabilities.OutputStallGuidance(type));
    }

    [Fact]
    public void OutputReportStatusProperty_IsDistinctReadOnlyKey()
    {
        Assert.NotEqual(DsHidMiniDriver.DeviceTypeProperty, DsHidMiniDriver.OutputReportStatusProperty);
        Assert.NotEqual(DsHidMiniDriver.LastHostRequestStatusProperty, DsHidMiniDriver.OutputReportStatusProperty);
        Assert.NotEqual(DsHidMiniDriver.MotionCalibrationSourceProperty, DsHidMiniDriver.OutputReportStatusProperty);
        Assert.Equal(15u, DsHidMiniDriver.OutputReportStatusProperty.PropertyIdentifier);
    }

    [Fact]
    public void SettingsEditor_ThirdPartyHidHidesLedsAndWirelessAndKeepsRumble()
    {
        SettingsEditorViewModel editor = new();
        editor.ApplyDeviceCapabilities(DsDeviceType.ThirdPartyHid);

        Assert.False(editor.HideRumbleSettings);
        Assert.True(editor.GeneralRumbleSettingsVM.IsGroupVisible);
        Assert.False(editor.LedsSettingsVM.IsGroupVisible);
        Assert.False(editor.WirelessSettingsVM.IsGroupVisible);
        Assert.True(editor.HasDeviceCapabilityNote);
    }

    [Fact]
    public void SettingsEditor_SixaxisKeepsLedAndWirelessGroups()
    {
        SettingsEditorViewModel editor = new();
        editor.ApplyDeviceCapabilities(DsDeviceType.Sixaxis);

        Assert.True(editor.LedsSettingsVM.IsGroupVisible);
        Assert.True(editor.WirelessSettingsVM.IsGroupVisible);
    }
}
