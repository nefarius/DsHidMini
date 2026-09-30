using System.Buffers.Binary;

namespace DualController.Core;

// This is a normalized XInput snapshot, not a DS3 report. Keeping the formats
// separate preserves DS3 pressure/motion semantics in the upstream driver.
public readonly record struct GamepadState(ushort Buttons, byte LeftTrigger,
    byte RightTrigger, short LeftX, short LeftY, short RightX, short RightY,
    int? BatteryPercent, bool Charging, bool TouchpadPressed);

public static class Ds4Protocol
{
    public const int UsbInputLength = 64;
    public const int BluetoothInputLength = 78;

    public static bool TryParse(ReadOnlySpan<byte> report, bool bluetooth,
        out GamepadState state)
    {
        state = default;
        int offset;
        if (bluetooth && report.Length == BluetoothInputLength && report[0] == 0x11)
        {
            if (!HasValidCrc(report, 0xA1)) return false;
            offset = 3;
        }
        else if (report.Length >= 10 && report[0] == 0x01)
        {
            // Native Bluetooth initially uses the short/basic report. Do not
            // discard it if requesting the enhanced feature report fails.
            if (!bluetooth && report.Length != UsbInputLength) return false;
            offset = 1;
        }
        else return false;

        var common = report[offset..];
        byte b0 = common[4], b1 = common[5], b2 = common[6];
        ushort buttons = 0;
        int hat = b0 & 15;
        if (hat is 0 or 1 or 7) buttons |= 0x0001;
        if (hat is 3 or 4 or 5) buttons |= 0x0002;
        if (hat is 5 or 6 or 7) buttons |= 0x0004;
        if (hat is 1 or 2 or 3) buttons |= 0x0008;
        if ((b1 & 0x20) != 0) buttons |= 0x0010; // Options -> Start
        if ((b1 & 0x10) != 0) buttons |= 0x0020; // Share -> Back
        if ((b1 & 0x40) != 0) buttons |= 0x0040;
        if ((b1 & 0x80) != 0) buttons |= 0x0080;
        if ((b1 & 0x01) != 0) buttons |= 0x0100;
        if ((b1 & 0x02) != 0) buttons |= 0x0200;
        if ((b2 & 0x01) != 0) buttons |= 0x0400;
        if ((b0 & 0x20) != 0) buttons |= 0x1000; // Cross -> A
        if ((b0 & 0x40) != 0) buttons |= 0x2000; // Circle -> B
        if ((b0 & 0x10) != 0) buttons |= 0x4000; // Square -> X
        if ((b0 & 0x80) != 0) buttons |= 0x8000; // Triangle -> Y
        int? battery = null;
        bool charging = false;
        if (common.Length >= 32 && (!bluetooth || report[0] == 0x11))
        {
            int capacity = common[29] & 15;
            bool cable = (common[29] & 0x10) != 0;
            // DS4 wired and wireless capacity scales differ by one step.
            battery = Math.Clamp((capacity + (cable ? 0 : 1)) * 10, 0, 100);
            charging = cable && capacity < 11;
        }
        state = new(buttons, common[7], common[8], Axis(common[0]),
            Invert(Axis(common[1])), Axis(common[2]), Invert(Axis(common[3])),
            battery, charging, (b2 & 2) != 0);
        return true;
    }

    // Center is exactly zero; endpoints use all of XInput's signed range.
    public static short Axis(byte value) => value < 128
        ? (short)((value - 128) * 256)
        : (short)((value - 128) * 32767 / 127);

    private static short Invert(short value) => value == short.MinValue
        ? short.MaxValue : (short)-value;

    public static byte[] Output(bool bluetooth, byte largeMotor, byte smallMotor,
        byte red, byte green, byte blue)
    {
        byte[] report = new byte[bluetooth ? 78 : 32];
        int offset = bluetooth ? 3 : 1;
        report[0] = bluetooth ? (byte)0x11 : (byte)0x05;
        if (bluetooth) report[1] = 0xC4; // HID, CRC32, 4ms polling
        report[offset] = 0x07; // rumble, LED colour and blink fields valid
        report[offset + 3] = smallMotor;
        report[offset + 4] = largeMotor;
        report[offset + 5] = red;
        report[offset + 6] = green;
        report[offset + 7] = blue;
        if (bluetooth)
            BinaryPrimitives.WriteUInt32LittleEndian(report.AsSpan(74),
                Crc32(0xA2, report.AsSpan(0, 74)));
        return report;
    }

    public static bool HasValidCrc(ReadOnlySpan<byte> report, byte seed) =>
        report.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(report[^4..])
        == Crc32(seed, report[..^4]);

    public static uint Crc32(byte seed, ReadOnlySpan<byte> report)
    {
        uint crc = UpdateCrc(uint.MaxValue, seed);
        foreach (byte value in report) crc = UpdateCrc(crc, value);
        return ~crc;
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (int bit = 0; bit < 8; bit++)
            crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0);
        return crc;
    }
}
