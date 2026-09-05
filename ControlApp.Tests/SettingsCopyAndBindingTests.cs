using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Util;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class SettingsCopyAndBindingTests
{
    [Fact]
    public void GeneralRumbleSettings_CopySettings_PreservesAlwaysStartInNormalMode()
    {
        GeneralRumbleSettings source = new()
        {
            AlwaysStartInNormalMode = true,
            IsAltModeToggleButtonComboEnabled = false,
            IsAltRumbleModeEnabled = true
        };
        source.AltModeToggleButtonCombo.IsEnabled = false;

        GeneralRumbleSettings dest = new();
        GeneralRumbleSettings.CopySettings(dest, source);

        Assert.True(dest.AlwaysStartInNormalMode);
        Assert.False(dest.IsAltModeToggleButtonComboEnabled);
        Assert.True(dest.IsAltRumbleModeEnabled);
    }

    [Fact]
    public void SettingsEditor_LoadSave_RoundTripsAllGroups()
    {
        DeviceSettings original = new();
        original.HidMode.SettingsContext = Models.DshmConfigManager.Enums.SettingsContext.GPJ;
        original.Wireless.IsWirelessIdleDisconnectEnabled = false;
        original.Wireless.WirelessIdleDisconnectTime = 120000;
        original.Sticks.LeftStickData.DeadZone = 20;
        original.Sticks.RightStickData.InvertYAxis = true;
        original.GeneralRumble.AlwaysStartInNormalMode = true;
        original.GeneralRumble.IsAltRumbleModeEnabled = true;
        original.GeneralRumble.AltModeToggleButtonCombo.IsEnabled = true;
        original.OutputReport.MaxOutputRate = 80;
        original.LeftMotorRescaling.LeftMotorStrRescalingLowerRange = 40;
        original.AltRumbleAdjusts.ForcedRightMotorHeavyThreshold = 200;

        SettingsEditorViewModel editor = new(original);
        DeviceSettings copy = new();
        editor.SaveAllChangesToBackingData(copy);

        Assert.Equal(original.HidMode.SettingsContext, copy.HidMode.SettingsContext);
        Assert.Equal(original.Wireless.IsWirelessIdleDisconnectEnabled, copy.Wireless.IsWirelessIdleDisconnectEnabled);
        Assert.Equal(original.Wireless.WirelessIdleDisconnectTime, copy.Wireless.WirelessIdleDisconnectTime);
        Assert.Equal(original.Sticks.LeftStickData.DeadZone, copy.Sticks.LeftStickData.DeadZone);
        Assert.Equal(original.Sticks.RightStickData.InvertYAxis, copy.Sticks.RightStickData.InvertYAxis);
        Assert.Equal(original.GeneralRumble.AlwaysStartInNormalMode, copy.GeneralRumble.AlwaysStartInNormalMode);
        Assert.True(copy.GeneralRumble.IsAltModeToggleButtonComboEnabled);
        Assert.Equal(original.OutputReport.MaxOutputRate, copy.OutputReport.MaxOutputRate);
        Assert.Equal(original.LeftMotorRescaling.LeftMotorStrRescalingLowerRange,
            copy.LeftMotorRescaling.LeftMotorStrRescalingLowerRange);
        Assert.Equal(original.AltRumbleAdjusts.ForcedRightMotorHeavyThreshold,
            copy.AltRumbleAdjusts.ForcedRightMotorHeavyThreshold);
    }

    [Fact]
    public void ButtonComboViewModel_NotifyAllPropertiesChanged_RaisesEmptyPropertyName()
    {
        ButtonsCombo combo = new() { IsEnabled = true, HoldTime = 2000 };
        ButtonComboViewModel vm = new(combo, 0);
        string? notified = "not-empty";
        vm.PropertyChanged += (_, args) => notified = args.PropertyName;

        vm.NotifyAllPropertiesChanged();

        Assert.Equal(string.Empty, notified);
    }

    [Fact]
    public void ButtonComboViewModel_HoldTimeSetter_NotifiesHoldTime()
    {
        ButtonsCombo combo = new();
        ButtonComboViewModel vm = new(combo, 0);
        string? notified = null;
        vm.PropertyChanged += (_, args) => notified = args.PropertyName;

        vm.HoldTime = 2;

        Assert.Equal(nameof(ButtonComboViewModel.HoldTime), notified);
        Assert.Equal(2000, combo.HoldTime);
    }

    [Fact]
    public void MacAddressFormatter_NormalizesAndFormats()
    {
        Assert.Equal("AABBCCDDEEFF", MacAddressFormatter.Normalize("aa:bb:cc:dd:ee:ff:11"));
        Assert.Equal("AA:BB:CC:DD:EE:FF", MacAddressFormatter.ToFriendly("aabbccddeeff"));
        Assert.Equal(string.Empty, MacAddressFormatter.Normalize(null));
        Assert.Equal(string.Empty, MacAddressFormatter.ToFriendly(null));
    }
}
