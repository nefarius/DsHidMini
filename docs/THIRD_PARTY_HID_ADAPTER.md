# ShanWan PS1/PS2 USB adapter (VID_2563 / PID_0575)

USB device `ShanWan` / `USB WirelessGamepad` (note the trailing space in the product string). The same VID/PID is reused by other ShanWan PC pads that speak this report. It is not a DualShock 3: it does not implement Feature `0x01`, `0xF2`, `0xF4`, or `0xEF`. A class request it does not understand can make the firmware disconnect and come back as `20BC:0055` without rumble. DsHidMini therefore skips the DS3 USB handshake for `DsDeviceTypeThirdPartyHid`.

The device enumerates as HID, USB 1.10, full speed, interrupt IN `0x81` and interrupt OUT `0x02`, 32-byte packets, 10 ms. The input report on the wire is 27 bytes with no report ID. Windows `ReadFile` on `hidusb` prepends a zero report ID, so that buffer is 28 bytes. The driver reads the USB pipe, so it sees the 27-byte payload.

## Input report

Confirmed from the HID caps Windows parsed out of the 137-byte report descriptor, and from the idle pipe sample `00 E0 0F 7F 7F 7F 7F` followed by 12 zero bytes and eight vendor bytes near `0x0200`.

| Offset | Content |
| --- | --- |
| 0 | Buttons, bit 0 first: Triangle, Circle, Cross, Square, L1, R1, L2, R2 |
| 1 | Select, Start, L3, R3, PS. Bits 5-7 are constant padding and read as 1 (`0xE0` at rest). Mask with `0x1F` |
| 2 | Hat in the low nibble. 0 = up, clockwise through 7. `0x0F` = neutral |
| 3-6 | Left X, left Y, right X, right Y. Rest is `0x7F`. Translated with a +1 bias so DS3 center `0x80` lines up, except `0xFF` which stays `0xFF` |
| 7-18 | Pressure, idle `0x00`: Right, Left, Up, Down, Triangle, Circle, Cross, Square, L1, R1, L2, R2 |
| 19-26 | Ignored. Not motion |

Battery is reported as charged. Motion is a fixed DS3 rest sample (accelerometer X/Y 512, Z at +1 g under the nominal calibration, gyro 512). There is no EEPROM read.

## Output report

Eight bytes, SET_REPORT Output, report id 0 (`wValue` `0x0200`) on the control endpoint. This is the transfer `HidD_SetOutputReport` accepted on the live adapter without changing VID/PID. The interrupt OUT pipe is not used.

| Offset | Content |
| --- | --- |
| 0 | `0x02` rumble message |
| 1 | `0x08` |
| 2 | Right motor, 0-255. DS3 small-motor magnitude (`LightCache`; the DS3 wire byte is only on/off) |
| 3 | Left motor, 0-255. DS3 large-motor byte |
| 4 | `0xFF` while either motor is non-zero, otherwise `0x00` |
| 5-7 | `0x00` |

`02 08 FF FF 20 00 00 00` followed by an all-zero stop was accepted by the adapter while `hidusb` was still bound. LEDs are not sent. USB power-off does not send Feature `0xF4` or the 48-byte DS3 output report.

## Capabilities

| | |
| --- | --- |
| Rumble | Yes |
| LEDs | No |
| Motion | Neutral only |
| Bluetooth / pairing | No. The driver publishes a synthesized device address so per-device configuration still has a stable key |
| HID modes | SDF, GPJ, SXS, DS4Windows, XInput, CGP, same as a DS3, with neutral motion |
