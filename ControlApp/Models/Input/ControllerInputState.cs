using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Input;

/// <summary>
///     HID-mode-independent DualShock 3 input snapshot derived from the raw IPC report.
/// </summary>
public sealed class ControllerInputState
{
    public static ControllerInputState Idle { get; } = new()
    {
        LeftStick = new ControllerStick(0x80, 0x80),
        RightStick = new ControllerStick(0x80, 0x80)
    };

    public bool Select { get; init; }

    public bool Start { get; init; }

    public bool Ps { get; init; }

    public bool L3 { get; init; }

    public bool R3 { get; init; }

    public bool Up { get; init; }

    public bool Right { get; init; }

    public bool Down { get; init; }

    public bool Left { get; init; }

    public bool L1 { get; init; }

    public bool R1 { get; init; }

    public bool L2 { get; init; }

    public bool R2 { get; init; }

    public bool Triangle { get; init; }

    public bool Circle { get; init; }

    public bool Cross { get; init; }

    public bool Square { get; init; }

    public ControllerPressure UpPressure { get; init; }

    public ControllerPressure RightPressure { get; init; }

    public ControllerPressure DownPressure { get; init; }

    public ControllerPressure LeftPressure { get; init; }

    public ControllerPressure L1Pressure { get; init; }

    public ControllerPressure R1Pressure { get; init; }

    public ControllerPressure L2Pressure { get; init; }

    public ControllerPressure R2Pressure { get; init; }

    public ControllerPressure TrianglePressure { get; init; }

    public ControllerPressure CirclePressure { get; init; }

    public ControllerPressure CrossPressure { get; init; }

    public ControllerPressure SquarePressure { get; init; }

    public ControllerStick LeftStick { get; init; }

    public ControllerStick RightStick { get; init; }

    public static ControllerInputState FromRawReport(in DS3_RAW_INPUT_REPORT report)
    {
        DS3_RAW_INPUT_REPORT.ButtonUnion buttons = report.Buttons;
        DS3_RAW_INPUT_REPORT.PressureUnion.PressureValues pressure = report.Pressure.Values;

        return new ControllerInputState
        {
            Select = buttons.Select,
            Start = buttons.Start,
            Ps = buttons.PS,
            L3 = buttons.L3,
            R3 = buttons.R3,
            Up = buttons.Up,
            Right = buttons.Right,
            Down = buttons.Down,
            Left = buttons.Left,
            L1 = buttons.L1,
            R1 = buttons.R1,
            L2 = buttons.L2,
            R2 = buttons.R2,
            Triangle = buttons.Triangle,
            Circle = buttons.Circle,
            Cross = buttons.Cross,
            Square = buttons.Square,
            UpPressure = new ControllerPressure(pressure.Up),
            RightPressure = new ControllerPressure(pressure.Right),
            DownPressure = new ControllerPressure(pressure.Down),
            LeftPressure = new ControllerPressure(pressure.Left),
            L1Pressure = new ControllerPressure(pressure.L1),
            R1Pressure = new ControllerPressure(pressure.R1),
            L2Pressure = new ControllerPressure(pressure.L2),
            R2Pressure = new ControllerPressure(pressure.R2),
            TrianglePressure = new ControllerPressure(pressure.Triangle),
            CirclePressure = new ControllerPressure(pressure.Circle),
            CrossPressure = new ControllerPressure(pressure.Cross),
            SquarePressure = new ControllerPressure(pressure.Square),
            LeftStick = new ControllerStick(report.LeftThumbX, report.LeftThumbY),
            RightStick = new ControllerStick(report.RightThumbX, report.RightThumbY)
        };
    }
}

public readonly record struct ControllerPressure(byte Raw)
{
    public double Normalized => Raw / 255.0;

    public string Label => Raw.ToString();
}

public readonly record struct ControllerStick(byte RawX, byte RawY)
{
    public const byte Center = 0x80;

    public const double DefaultTravel = 28;

    public double OffsetX => ((RawX - Center) / 128.0) * DefaultTravel;

    // Raw Y already grows toward the bottom of the pad, same as WPF canvas Y.
    public double OffsetY => ((RawY - Center) / 128.0) * DefaultTravel;

    public string Label => $"{RawX}, {RawY}";
}
