# DS3-identity PS1/PS2 USB adapter (VID_054C / PID_0268, bMaxPacketSize0 8)

USB device that enumerates as Sony DualShock 3 (`054C:0268`) but is a wired
PS1/PS2 adapter. The sample on the shelf is sold as **Ejoyous Controller
Adapter** (manufacturer Dongguan Tegezhe Electronics Co., Ltd.; Amazon FNSKU
`X002HXD4W7`). The same firmware is likely resold under other labels. The
label advertises P1/P2 controller use on PS3 and PC.

It is not a DualShock 3. It answers Feature `0x01`, `0xF2`, `0xEF`, and
`0xF4` with plausible DS3 payloads, so the HID class-request path cannot
tell it from a clone pad. DsHidMini classifies it as
`DsDeviceTypeDs3IdentityAdapter` from the USB device descriptor only:
`bMaxPacketSize0 != 64`. Every genuine DS3/SIXAXIS reports `64`. The
descriptor is already cached in `DsUsb_PrepareHardware`; no extra bus
traffic is required. Bluetooth is never this type (no descriptor, and the
adapter has no radio).

A secondary fingerprint, not required for classification: `iManufacturer`
`"Sony"` with `bLength 0x0C` (embedded NUL after the four ASCII characters).

The DS3 USB handshake is left intact. The reported Feature `0xF2` address
is kept as the per-device configuration key; ControlApp hides pairing
because it is not a real radio.

## Observed descriptors

Device descriptor (`bcdUSB 2.00`, `bcdDevice 0x0100`, `bMaxPacketSize0 8`):

| Field | Value |
| --- | --- |
| idVendor | `0x054C` |
| idProduct | `0x0268` |
| bMaxPacketSize0 | `0x08` (genuine DS3: `0x40`) |
| iManufacturer | `"Sony\0"` (`bLength 0x0C`) |
| iProduct | `"PLAYSTATION(R)3 Controller"` |
| MaxPower | 500 mA |

Configuration: HID interface, interrupt OUT `0x02` and interrupt IN `0x81`,
64-byte packets, 1 ms. HID report descriptor length `0x0094` (148 bytes),
same as a DualShock 3. Device qualifier is not implemented.

## Feature reports (live Ejoyous sample)

| Report | Result |
| --- | --- |
| Feature `0x01` | Clone identification blob: field list `01 02`, byte `0x29 == 0x64`. `IdentificationCloneHeuristic` is `True`. |
| Feature `0xF2` | `00:1B:FB:CB:00:F2` (ALPS OUI). Not synthesized; not a radio. |
| Feature `0xEF` | Plausible EEPROM page. Motion calibration source is live USB; values are fabricated. |

## Capabilities

| | |
| --- | --- |
| Rumble | Yes. Uses the normal 48-byte DS3 output path, not the ShanWan 8-byte adapter report. |
| LEDs | No. Settings are hidden. |
| Motion | Fabricated. Feature `0x01`/`0xEF` answers are treated as DS3 data; they are not a real IMU. |
| Bluetooth / pairing | No. The Feature `0xF2` address is a config key only. |
| HID modes | SDF, GPJ, SXS, DS4Windows, XInput, CGP, same as a DS3 |
| Output stall | Not used. `DEVPKEY_DsHidMini_RO_OutputReportStatus` stays `STATUS_SUCCESS`. |

Older ControlApp builds that only read VID/PID still show this device as
DualShock 3 / SIXAXIS. The published `DEVPKEY_DsHidMini_RO_DeviceType`
value is `6`.
