using System.Buffers.Binary;
using DualController.Core;

int checks = 0;
void Check(bool ok, string name)
{
    checks++;
    if (!ok) throw new Exception(name);
}
byte[] NeutralUsb()
{
    byte[] r = new byte[64]; r[0] = 1;
    Array.Fill(r, (byte)128, 1, 4); r[5] = 8;
    return r;
}
var usb = NeutralUsb();
Check(Ds4Protocol.TryParse(usb, false, out var state), "USB neutral parse");
Check(state.Buttons == 0 && state.LeftX == 0 && state.LeftY == 0, "Neutral sticks/buttons");
usb[5] = 0xF1; usb[6] = 0xF3; usb[7] = 3; usb[8] = 127; usb[9] = 255;
Check(Ds4Protocol.TryParse(usb, false, out state), "USB populated parse");
Check(state.Buttons == 0xF7F9 && state.LeftTrigger == 127 && state.RightTrigger == 255,
    "Face/system/shoulder/diagonal mapping");
Check(state.TouchpadPressed, "Touchpad click retained independently");
for (int hat = 0; hat < 16; hat++)
{
    ushort[] expected = [1, 9, 8, 10, 2, 6, 4, 5, 0, 0, 0, 0, 0, 0, 0, 0];
    var r = NeutralUsb(); r[5] = (byte)hat;
    Check(Ds4Protocol.TryParse(r, false, out state) && state.Buttons == expected[hat], $"Hat {hat}");
}
Check(Ds4Protocol.Axis(0) == -32768 && Ds4Protocol.Axis(128) == 0
    && Ds4Protocol.Axis(255) == 32767, "Stick endpoints");
usb[1] = 0; usb[2] = 0; usb[3] = 255; usb[4] = 255;
Check(Ds4Protocol.TryParse(usb, false, out state) && state.LeftX == -32768
    && state.LeftY == 32767 && state.RightY == -32767, "Y inversion without overflow");
byte[] bt = new byte[78]; bt[0] = 0x11;
usb.AsSpan(1, 32).CopyTo(bt.AsSpan(3));
BinaryPrimitives.WriteUInt32LittleEndian(bt.AsSpan(74), Ds4Protocol.Crc32(0xA1, bt.AsSpan(0, 74)));
Check(Ds4Protocol.TryParse(bt, true, out var wireless) && wireless == state, "BT equivalent payload");
bt[4] ^= 1;
Check(!Ds4Protocol.TryParse(bt, true, out _), "Corrupt BT CRC rejected");
Check(!Ds4Protocol.TryParse(bt, false, out _), "BT report rejected on USB");
Check(Ds4Protocol.TryParse(NeutralUsb().AsSpan(0, 10), true, out state)
    && state.BatteryPercent == null, "BT basic report fallback");
Check(Ds4Protocol.TryParse(NeutralUsb(), true, out state)
    && state.BatteryPercent == null, "Padded BT basic report does not invent battery status");
for (int length = 0; length < 64; length++)
    Check(!Ds4Protocol.TryParse(NeutralUsb().AsSpan(0, length), false, out _), $"Truncated USB {length}");
usb[30] = 0x13;
Check(Ds4Protocol.TryParse(usb, false, out state) && state.BatteryPercent == 30
    && state.Charging, "Charging scale");
usb[30] = 3;
Check(Ds4Protocol.TryParse(usb, false, out state) && state.BatteryPercent == 40
    && !state.Charging, "Wireless battery scale");
var output = Ds4Protocol.Output(false, 200, 80, 10, 20, 30);
Check(output.Length == 32 && output[0] == 5 && output[1] == 7 && output[4] == 80
    && output[5] == 200 && output[6] == 10 && output[8] == 30, "USB rumble/LED offsets");
output = Ds4Protocol.Output(true, 200, 80, 10, 20, 30);
Check(output.Length == 78 && output[0] == 0x11 && output[1] == 0xC4
    && output[6] == 80 && output[7] == 200 && Ds4Protocol.HasValidCrc(output, 0xA2), "BT output offsets and CRC");
// External CRC vector calculated independently with Python zlib.crc32.
Check(Ds4Protocol.Crc32(0xA1, "123456789"u8) == 0x88ED2411, "Independent CRC vector");
Check(DeviceOrigin.IsPhysicalDs4(["HID\\VID_054C&PID_05C4", "USB\\VID_054C&PID_05C4\\123"],
    0x054C, 0x05C4, out bool isBt) && !isBt, "Physical DS4 v1 USB");
Check(DeviceOrigin.IsPhysicalDs4(["HID\\VID_054C&PID_09CC", "BTHENUM\\DEV_123"],
    0x054C, 0x09CC, out isBt) && isBt, "Physical DS4 v2 BT");
Check(!DeviceOrigin.IsPhysicalDs4(["ROOT\\VIGEMBUS", "BTHENUM\\DEV_123"],
    0x054C, 0x05C4, out _), "ViGEm feedback loop blocked");
Check(!DeviceOrigin.IsPhysicalDs4(["BTHPS3BUS\\DEV_0268", "BTHENUM\\DEV_123"],
    0x054C, 0x05C4, out _), "PS3 DS4 emulation excluded");
Check(!DeviceOrigin.IsPhysicalDs4(["USB\\VID_054C&PID_0268"],
    0x054C, 0x05C4, out _), "USB DS3 DS4 emulation excluded");
Check(!DeviceOrigin.IsPhysicalDs4(["USB\\VID_054C&PID_05C4"],
    0x054C, 0x0268, out _), "Real PS3 left to upstream driver");
Console.WriteLine($"PASS: {checks} protocol and device-origin checks");
