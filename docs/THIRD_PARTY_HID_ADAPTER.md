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

Eight bytes, report id 0, written to interrupt OUT endpoint `0x02` by default. That is the path Linux `usbhid` uses for hid-shanwan's output report. Setting `UsbOutputReportTransport` to `ControlEndpoint` sends the same bytes as SET_REPORT Output (`wValue` `0x0200`) instead.

Confirmed on 2026-09-23 with a Retro Fighters Defender (first model) on its PS1/PS2 receiver: `02 08 FF FF FF 00 00 00` on interrupt OUT starts both motors and `02 08 00 00 FF 00 00 00` stops them, both from a raw WinUSB write and from driver 3.12.0 in XInput mode. XInput left (large motor) is byte 3, XInput right (small motor) is byte 2. The interrupt OUT write completes in about 5-15 ms, also while the IN pipe is being read continuously.

While the adapter has no live controller on its PS2 side (the input report sits at `00 E0 0F 7F 7F 7F 7F` with all axes exactly centred), the same 8-byte interrupt OUT is never acknowledged. A capture with driver 3.12.0 showed the URB staying pending until the then-3-second send timeout cancelled it (`USBD_STATUS_CANCELED`, `0xC0010000`), so every queued output report cost 3 seconds on the output worker. That window was the pad paired to its USB dongle instead of the PS1/PS2 receiver. Whether the pad rumbles is decided by the pairing, not by the report layout.

The driver now bounds that case instead of waiting it out:

- Interrupt OUT for this device type times out after 250 ms. DualShock 3 keeps the 3 second bound. A linked adapter still completes in 5-15 ms.
- Once a send has failed, an identical 8-byte payload is not put on the bus again until one second has passed. That probe is what notices the controller linking again. A payload that changed (a new rumble strength) is sent immediately.
- The stall and the recovery are each logged once. While the stall lasts, a full output queue is event-logged once per episode rather than on every keep-alive.
- `DEVPKEY_DsHidMini_RO_OutputReportStatus` (property id 15, `DEVPROP_TYPE_NTSTATUS`) is `STATUS_SUCCESS` while output is being acknowledged and the failing status while it is not. It is initialized for every USB device and only changes for this device type. ControlApp reads it on the same poll as the battery and shows a warning while it is non-zero. A power-up (`D0Entry`) clears the stall.

| Offset | Content |
| --- | --- |
| 0 | `0x02` rumble message |
| 1 | `0x08` |
| 2 | Right motor, 0-255. DS3 small-motor magnitude (`LightCache`; the DS3 wire byte is only on/off) |
| 3 | Left motor, 0-255. DS3 large-motor byte |
| 4 | `0xFF` duration, including the stop report |
| 5-7 | `0x00` |

`02 08 FF FF 20 00 00 00` was accepted as a control SET_REPORT while `hidusb` was still bound. The driver sends that layout on interrupt OUT, with byte 4 always `0xFF`. LEDs are not sent. USB power-off does not send Feature `0xF4` or the 48-byte DS3 output report.

## Capabilities

| | |
| --- | --- |
| Rumble | Yes, both motors (Retro Fighters Defender on its PS1/PS2 receiver). Interrupt OUT is not acknowledged while no controller is linked; the driver then short-timeouts, stops repeating the same payload, and publishes `DEVPKEY_DsHidMini_RO_OutputReportStatus` |
| LEDs | No |
| Motion | Neutral only |
| Bluetooth / pairing | No. The driver publishes a synthesized device address so per-device configuration still has a stable key |
| HID modes | SDF, GPJ, SXS, DS4Windows, XInput, CGP, same as a DS3, with neutral motion |
