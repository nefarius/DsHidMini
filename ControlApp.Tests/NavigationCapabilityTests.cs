using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.DsHidMini.IPC.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class NavigationCapabilityTests
{
    [Theory]
    [InlineData((ushort)0x054C, (ushort)0x042F, DsDeviceType.Navigation)]
    [InlineData((ushort)0x054C, (ushort)0x0268, DsDeviceType.Sixaxis)]
    [InlineData((ushort)0x054C, (ushort)0x05C4, DsDeviceType.Unknown)]
    [InlineData((ushort)0x045E, (ushort)0x028E, DsDeviceType.Unknown)]
    public void FromHardwareIds_MapsKnownSonyPids(ushort vendorId, ushort productId, DsDeviceType expected)
    {
        Assert.Equal(expected, DsDeviceCapabilities.FromHardwareIds(vendorId, productId));
    }

    [Fact]
    public void Navigation_HasSingleLedAndNoRumble()
    {
        Assert.True(DsDeviceCapabilities.IsNavigation(DsDeviceType.Navigation));
        Assert.True(DsDeviceCapabilities.HasSingleLed(DsDeviceType.Navigation));
        Assert.False(DsDeviceCapabilities.HasRumble(DsDeviceType.Navigation));
        Assert.Equal("Navigation Controller", DsDeviceCapabilities.DisplayName(DsDeviceType.Navigation));
        Assert.Contains("XInput", DsDeviceCapabilities.HidModeGuidance(DsDeviceType.Navigation));
    }

    [Theory]
    [InlineData(DsDeviceType.Sixaxis)]
    [InlineData(DsDeviceType.Unknown)]
    public void DualShock3Defaults_KeepRumbleAndFourLeds(DsDeviceType type)
    {
        Assert.False(DsDeviceCapabilities.IsNavigation(type));
        Assert.False(DsDeviceCapabilities.HasSingleLed(type));
        Assert.True(DsDeviceCapabilities.HasRumble(type));
        Assert.Equal(string.Empty, DsDeviceCapabilities.HidModeGuidance(type));
    }

    [Fact]
    public void SettingsEditor_NavigationHidesRumbleAndLimitsLeds()
    {
        SettingsEditorViewModel editor = new();
        Assert.False(editor.HideRumbleSettings);
        Assert.False(editor.LedsSettingsVM.IsSingleLedDevice);
        Assert.True(editor.GeneralRumbleSettingsVM.IsGroupVisible);
        Assert.True(editor.LeftMotorRescaleSettingsVM.IsGroupVisible);
        Assert.True(editor.AltRumbleSettingsVM.IsGroupVisible);

        editor.ApplyDeviceCapabilities(DsDeviceType.Navigation);

        Assert.True(editor.HideRumbleSettings);
        Assert.True(editor.LedsSettingsVM.IsSingleLedDevice);
        Assert.False(editor.GeneralRumbleSettingsVM.IsGroupVisible);
        Assert.False(editor.LeftMotorRescaleSettingsVM.IsGroupVisible);
        Assert.False(editor.AltRumbleSettingsVM.IsGroupVisible);
        Assert.True(editor.HasDeviceCapabilityNote);
        Assert.Equal(4, editor.LedsSettingsVM.Leds_VM!.Length);
    }

    [Fact]
    public void SettingsEditor_SixaxisKeepsRumbleGroupsAndFourLeds()
    {
        SettingsEditorViewModel editor = new();
        editor.ApplyDeviceCapabilities(DsDeviceType.Sixaxis);

        Assert.False(editor.HideRumbleSettings);
        Assert.False(editor.LedsSettingsVM.IsSingleLedDevice);
        Assert.True(editor.GeneralRumbleSettingsVM.IsGroupVisible);
        Assert.True(editor.LeftMotorRescaleSettingsVM.IsGroupVisible);
        Assert.True(editor.AltRumbleSettingsVM.IsGroupVisible);
        Assert.False(editor.HasDeviceCapabilityNote);
    }

    [Fact]
    public void SettingsEditor_NavigationDoesNotChangeDefaultHidMode()
    {
        DeviceSettings settings = new();
        SettingsEditorViewModel editor = new(settings);
        editor.ApplyDeviceCapabilities(DsDeviceType.Navigation);

        Assert.Equal(SettingsContext.XInput, editor.HidModeVM.Context);
        Assert.Equal(SettingsContext.XInput, settings.HidMode.SettingsContext);
    }

    [Fact]
    public void DeviceTypeProperty_IsDistinctReadOnlyKey()
    {
        Assert.NotEqual(DsHidMiniDriver.DeviceAddressSynthesizedProperty, DsHidMiniDriver.DeviceTypeProperty);
        Assert.NotEqual(DsHidMiniDriver.IdentificationCloneHeuristicProperty, DsHidMiniDriver.DeviceTypeProperty);
    }
}
