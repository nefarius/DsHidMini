# Twin USB Joystick PS1/PS2 adapter (VID_0810 / PID_0001)

USB device `Twin USB Joystick` (`0810:0001`). This is the GreenAsia / PantherLord
chipset used by cheap dual-port PS1/PS2-to-USB adapters and by many generic
"Twin USB Gamepad" pads that reuse the same ID. Linux handles it in `hid-pl.c`
with `HID_QUIRK_MULTI_INPUT | HID_QUIRK_SKIP_OUTPUT_REPORTS`.

DsHidMini does **not** bind this device. ControlApp shows a warning when one is
present. The inbox `hidusb` stack stays bound so both ports remain visible in
Game Controllers (`joy.cpl`).

## Identification (live sample)

| Field | Value |
| --- | --- |
| idVendor | `0x0810` |
| idProduct | `0x0001` |
| bcdUSB | 1.00 |
| Device bus speed | Low-Speed |
| bMaxPacketSize0 | `0x08` |
| iProduct | `"Twin USB Joystick"` |
| MaxPower | 500 mA |
| Interfaces | 1 HID (`03 00 00`) |
| Endpoints | Interrupt IN `0x81` only, 8-byte packets, 10 ms |
| HID report descriptor | 202 bytes (`0x00CA`) |

The report descriptor is two identical top-level Joystick collections with
report IDs 1 and 2 (one per PS2 port). Windows therefore creates
`HID\VID_0810&PID_0001&COL01` and `COL02`, and `joy.cpl` lists two
"Twin USB Joystick" devices.

There is no interrupt OUT pipe. Feature and output reports go through the
control endpoint.

## Per-port input report

One report ID plus a short digital-pad payload. DualShock 2 pressure is not
forwarded.

| Offset | Content |
| --- | --- |
| 0 | Report ID (`1` = port 1, `2` = port 2) |
| 1-5 | Axes: X, Y, Z, Z, Rz (8-bit) |
| 6 | Hat in the low nibble |
| 7-8 | 12 digital buttons |
| 9 | Vendor / padding |

## Output / rumble

Linux `hid-pl` sends one output report per port as SET_REPORT on the control
pipe. The typical single-field layout is four 16-bit `ff00.0002` values:
bytes 0-1 unused, `value[2]` strong (left) motor, `value[3]` weak (right)
motor, max `0x7F`.

## Why DsHidMini does not bind it

1. **Two ports in one USB function.** DsHidMini is one FDO, one
   `DEVICE_CONTEXT`, one IPC slot, and one config key
   (`DsDevice_AssignDeviceType` in `driver/Device.c`). Binding would either
   drop port 2 (which the inbox driver already exposes) or require a new
   multi-pad architecture.
2. **Generic VID/PID.** `0810:0001` is reused by countless non-adapter
   gamepads. An INF match would hijack all of them, and there is no runtime
   way to hand a device back to `hidusb`. ShanWan `2563:0575` had much
   narrower reuse.
3. **Capability floor.** Digital buttons only, 8-bit axes at 10 ms, no PS
   button, motion, LEDs, or battery. XInput/DS4 emulation would be the only
   gain.

## "No input" is on the PS2 side

If `hidusb` is bound and both collections appear in `joy.cpl`, but buttons
and sticks do nothing, the adapter firmware is not talking to the controller.
A USB host driver cannot fix that. Common causes on this chipset:

- The controller is probed only at USB power-up. Attach the pad (or
  receiver) **before** plugging the adapter into the PC, then replug USB.
- A frequently omitted diode (D3) leaves controller VCC unpowered or weak.
  Wireless receivers are hit hardest; see the repair write-up at
  [benryves.com](https://www.benryves.com/journal/3763149).
- Tight / non-standard PS2 polling and no analog-enable sequence. Third-party
  wireless receivers (including some Retro Fighters Defender models) often
  do not answer.

Supported adapters that DsHidMini **does** bind:

- ShanWan (`VID_2563` / `PID_0575`) — [THIRD_PARTY_HID_ADAPTER.md](THIRD_PARTY_HID_ADAPTER.md)
- DS3-identity (`VID_054C` / `PID_0268`, `bMaxPacketSize0 8`) — [DS3_IDENTITY_ADAPTER.md](DS3_IDENTITY_ADAPTER.md)
