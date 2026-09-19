using Nefarius.DsHidMini.ControlApp.Models.Input;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class ControllerInputStateTests
{
    [Fact]
    public void FromRawReport_MapsEveryDigitalBitIncludingPs()
    {
        DS3_RAW_INPUT_REPORT report = default;
        report.Buttons.Select = true;
        report.Buttons.Start = true;
        report.Buttons.PS = true;
        report.Buttons.L3 = true;
        report.Buttons.R3 = true;
        report.Buttons.Up = true;
        report.Buttons.Right = true;
        report.Buttons.Down = true;
        report.Buttons.Left = true;
        report.Buttons.L1 = true;
        report.Buttons.R1 = true;
        report.Buttons.L2 = true;
        report.Buttons.R2 = true;
        report.Buttons.Triangle = true;
        report.Buttons.Circle = true;
        report.Buttons.Cross = true;
        report.Buttons.Square = true;

        ControllerInputState state = ControllerInputState.FromRawReport(in report);

        Assert.True(state.Select);
        Assert.True(state.Start);
        Assert.True(state.Ps);
        Assert.True(state.L3);
        Assert.True(state.R3);
        Assert.True(state.Up);
        Assert.True(state.Right);
        Assert.True(state.Down);
        Assert.True(state.Left);
        Assert.True(state.L1);
        Assert.True(state.R1);
        Assert.True(state.L2);
        Assert.True(state.R2);
        Assert.True(state.Triangle);
        Assert.True(state.Circle);
        Assert.True(state.Cross);
        Assert.True(state.Square);
    }

    [Fact]
    public void FromRawReport_MapsPressureOrderAndNormalization()
    {
        DS3_RAW_INPUT_REPORT report = default;
        report.Pressure.Values.Up = 1;
        report.Pressure.Values.Right = 32;
        report.Pressure.Values.Down = 64;
        report.Pressure.Values.Left = 96;
        report.Pressure.Values.L2 = 128;
        report.Pressure.Values.R2 = 160;
        report.Pressure.Values.L1 = 192;
        report.Pressure.Values.R1 = 224;
        report.Pressure.Values.Triangle = 240;
        report.Pressure.Values.Circle = 248;
        report.Pressure.Values.Cross = 254;
        report.Pressure.Values.Square = 255;

        ControllerInputState state = ControllerInputState.FromRawReport(in report);

        Assert.Equal(1, state.UpPressure.Raw);
        Assert.Equal(32, state.RightPressure.Raw);
        Assert.Equal(64, state.DownPressure.Raw);
        Assert.Equal(96, state.LeftPressure.Raw);
        Assert.Equal(128, state.L2Pressure.Raw);
        Assert.Equal(160, state.R2Pressure.Raw);
        Assert.Equal(192, state.L1Pressure.Raw);
        Assert.Equal(224, state.R1Pressure.Raw);
        Assert.Equal(240, state.TrianglePressure.Raw);
        Assert.Equal(248, state.CirclePressure.Raw);
        Assert.Equal(254, state.CrossPressure.Raw);
        Assert.Equal(255, state.SquarePressure.Raw);
        Assert.Equal(1 / 255.0, state.UpPressure.Normalized, 6);
        Assert.Equal(1.0, state.SquarePressure.Normalized);
        Assert.Equal("255", state.SquarePressure.Label);
    }

    [Fact]
    public void FromRawReport_MapsStickCenterAndExtremes()
    {
        DS3_RAW_INPUT_REPORT report = default;
        report.LeftThumbX = 0x80;
        report.LeftThumbY = 0x80;
        report.RightThumbX = 0x00;
        report.RightThumbY = 0xFF;

        ControllerInputState state = ControllerInputState.FromRawReport(in report);

        Assert.Equal(0x80, state.LeftStick.RawX);
        Assert.Equal(0x80, state.LeftStick.RawY);
        Assert.Equal(0, state.LeftStick.OffsetX);
        Assert.Equal(0, state.LeftStick.OffsetY);
        Assert.Equal("128, 128", state.LeftStick.Label);

        Assert.Equal(0x00, state.RightStick.RawX);
        Assert.Equal(0xFF, state.RightStick.RawY);
        Assert.Equal(-ControllerStick.DefaultTravel, state.RightStick.OffsetX);
        Assert.Equal((0xFF - ControllerStick.Center) / 128.0 * ControllerStick.DefaultTravel, state.RightStick.OffsetY, 6);
        Assert.Equal("0, 255", state.RightStick.Label);
    }

    [Fact]
    public void Idle_HasCenteredSticksAndNoButtons()
    {
        ControllerInputState idle = ControllerInputState.Idle;

        Assert.False(idle.Ps);
        Assert.Equal(0, idle.UpPressure.Raw);
        Assert.Equal(ControllerStick.Center, idle.LeftStick.RawX);
        Assert.Equal(ControllerStick.Center, idle.RightStick.RawY);
    }

    [Fact]
    public void StatusText_MentionsIpcAndHidModeIndependence()
    {
        Assert.Contains("IPC", InputTesterStatusFormatter.StatusText(false, false), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Waiting", InputTesterStatusFormatter.StatusText(true, false), StringComparison.OrdinalIgnoreCase);

        string live = InputTesterStatusFormatter.StatusText(true, true);
        Assert.Contains("raw", live, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HID mode", live, StringComparison.OrdinalIgnoreCase);
    }
}
