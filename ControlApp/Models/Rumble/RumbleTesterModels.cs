using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Rumble;

internal interface IRumbleOutput : IDisposable
{
    uint SetRumble(byte largeMotor, byte smallMotor);
}

internal sealed class IpcRumbleOutput : IRumbleOutput
{
    private readonly int _deviceIndex;
    private readonly DsHidMiniInterop _interop = new();

    public IpcRumbleOutput(int deviceIndex)
    {
        _deviceIndex = deviceIndex;
    }

    public uint SetRumble(byte largeMotor, byte smallMotor)
    {
        return _interop.SetRumble(_deviceIndex, largeMotor, smallMotor);
    }

    public void Dispose()
    {
        _interop.Dispose();
    }
}

internal readonly record struct RumbleCommandResult(bool Sent, uint Status, string? Error)
{
    public static RumbleCommandResult NotSent { get; } = new(false, 0, null);

    public bool Succeeded => Sent && Error is null && PowerOffUsbResult.IsNtSuccess(Status);

    public static RumbleCommandResult FromStatus(uint status)
    {
        return new RumbleCommandResult(true, status, null);
    }

    public static RumbleCommandResult FromException(Exception exception)
    {
        return new RumbleCommandResult(true, 0, exception.Message);
    }
}

internal static class RumbleStrength
{
    public static byte FromSlider(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return 0;
        }

        int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded >= byte.MaxValue)
        {
            return byte.MaxValue;
        }

        return (byte)rounded;
    }
}

internal static class RumbleTesterStatus
{
    public const string Ready = "Ready to pulse the motors.";

    public const string Off = "Rumble off.";

    public static string Pulsing(byte large, byte small)
    {
        return $"Rumble on: large {large}, small {small}.";
    }

    public static string Rejected(uint status)
    {
        return $"The driver rejected the rumble request (0x{status:X8}).";
    }
}

internal static class RumbleTesterAvailability
{
    public static bool CanOpen(bool ipcAvailable, bool hasSlot, bool hasRumble)
    {
        return ipcAvailable && hasSlot && hasRumble;
    }

    public static string ToolTip(bool canOpen)
    {
        return canOpen
            ? "Pulse the large and small motors through driver IPC. The selected HID mode does not change this path."
            : "Rumble tester needs a controller with rumble, driver IPC, and a published device slot.";
    }
}
