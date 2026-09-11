# PS3 DualShock 3 USB startup sequence

Findings from six real-world PS3-console-to-DS3 USB link-layer captures
([CircumSpector/Research, "Sony DualShock 3"](https://github.com/CircumSpector/Research/tree/master/Sony%20DualShock%203):
CECHZC2E-A1, CECHZC2U-A2 and SIXAXIS consoles), analysed with a local
`tshark` (the Wireshark MCP `analyze_pcap` tool cannot dissect USB pcaps -
see the recipe at the bottom).
This informed the soft-fail MAC discovery and USB startup rework for
[issue #321](https://github.com/nefarius/DsHidMini/issues/321); nothing here
changes behaviour for a genuine, fully-compliant controller.

## Sequence

Identical across all three consoles and all six samples:

1. `SET_IDLE` (class request `0x0A`). A genuine DS3 **STALLs** this - not
   worth replicating.
2. `GET_REPORT Feature 0x01` (identification, see
   [issue #50](https://github.com/nefarius/DsHidMini/issues/50)).
3. `GET_REPORT Feature 0xF2` (device Bluetooth MAC address).
4. `GET_REPORT Feature 0xF5` (host Bluetooth MAC address the device is
   currently paired to).
5. Only when the host radio address differs from what the device already
   has: `SET_REPORT Feature 0xF5` (pairing request), then a verifying
   `GET_REPORT Feature 0xF5` 27-75 ms later (varies by sample).
6. `0xEF` / `0xF8` calibration-page reads (motion sensor calibration data;
   DsHidMini now soft-reads page `0xA0` via Feature `0xEF` on USB start, see
   [`MOTION.md`](MOTION.md#what-dshidmini-does-today); `0xF8` is still unused).
7. **`SET_REPORT Output 0x01` on EP0 (control endpoint), 48 bytes, no report
   ID, all zeros.** This is the pre-enable output report and the reason
   DsHidMini now sends an equivalent EP0 report during
   `DsUsb_PrepareHardware` instead of the historical interrupt-OUT write.
8. `GET_REPORT Feature 0xF7` (unknown purpose, not emulated).
9. The console waits for the PS button to be pressed. The disabled pad
   emits exactly **one** input report during this wait (Report ID `0x01`,
   PS-button bit set), then goes quiet again - no other feature or output
   traffic happens while waiting.
10. `SET_REPORT Feature 0xF4 42 0C 00 00` (enable/start streaming).
11. LED-state report on EP0 (`SET_REPORT Output 0x01`), sent **twice** in a
    row, mirrored by DsHidMini as a single post-enable EP0 report in
    `DsUsb_D0Entry`.
12. From here on, everything (LEDs, rumble) is sent on interrupt OUT only:
    ~2900 output transfers observed per sample, locked to the console's
    60 Hz vsync, and **resent every frame even when unchanged** - the
    console never rate-limits or diffs output reports. Two EP0 state
    refreshes recur on ~5 s and ~9 s timers for the whole session (see LED
    timing below); no other feature requests occur after `0xF4`.
    Interrupt OUT sees zero NAKs or retries in any sample.
13. On unplug/disable: `SET_REPORT Feature 0xF4 42 0B 00 00`, preceded by
    one more all-zero EP0 report (same shape as step 7).

No `SET_IDLE`, `0xF7`, or `0xF8` traffic is emulated by DsHidMini. Feature
`0xEF` page `0xA0` is soft-read on USB start for motion calibration (failure
does not abort `PrepareHardware`). They remain documented here so future
quirk work does not need to re-derive them from the pcaps.

## Report layouts

- EP0 (control endpoint) output report: **48 bytes**, no report ID byte.
- Interrupt OUT output report: **49 bytes**, byte 0 is report ID `0x01`
  (`DS3_USB_HID_OUTPUT_REPORT_SIZE` / `G_Ds3UsbHidOutputReport`).
- Input reports: observed at roughly 100 Hz while streaming.

## Output byte semantics (from the console's own traffic)

- Motor durations (interrupt OUT bytes 2 and 4) are **always `0x96`**,
  never `0xFF`, across every sample and every rumble instruction observed.
- The small (right) motor strength byte is strictly `0x00` or `0x01`
  (on/off only); the big (left) motor strength byte is a full 0-255 power
  value.
- Bytes `[6..7]` of the interrupt OUT report (observed as `ff 77`, `ff 7f`,
  or `00 00` depending on sample) are an inserted `0xFF` followed by the
  EEPROM gyro calibration byte (page `0xA0` offset 14-15), not a verbatim
  copy of that page. One console/pad pairing (B1) reads these back as zero,
  so DsHidMini sending zeros here is a valid, real-world value, not a gap.
  A SIXAXIS gets them at bytes `[4..5]` instead - see
  [`MOTION.md`](MOTION.md#gyroscope).
- Unused LED pattern blocks (for player slots that are off) are all-zero.

## LED timing

- Initial LED effect (set immediately at enable): `ff 00 01 00 01`.
- Roughly 5 seconds after enable, the console switches to
  `ff 27 10 32 32` and stays there for the remainder of the session. This
  is relevant to the (separate) LED-handling-unification plan's "static
  effect" assumption - not changed by this work, just recorded here.

## Aftermarket / non-genuine controller behaviour

- **Retro Fighters Defender (OG)**, in its native `054C:0CDA` USB mode,
  exposes **only an interrupt IN endpoint** - there is no interrupt OUT
  pipe at all. This is the primary real-world case
  `DsUsb_PrepareHardware`'s pipe validation now tolerates (falls back to
  `DsUsbOutputReportTransportControlEndpoint`, see `driver/DsUsb.c` and
  `driver/DsCommon.h`). `054C:0CDA` is not a DualShock 3 identity at all -
  it is the **PlayStation Classic controller** identity (49-byte report
  descriptor, one IN endpoint, digital buttons only). It already works on
  Windows as a plain HID gamepad without DsHidMini, and binding DsHidMini
  to it would misparse PS-Classic input reports as DS3 reports, so it is
  intentionally **not** added to `dshidmini.inf`. The dongle's other two
  modes, XInput (`045E:028E`) and generic ShanWan DirectInput
  (`2563:0575`, 137-byte descriptor with the `0x2621` PS3 "magic" feature),
  also already work without DsHidMini. Whether the OG dongle can ever
  present a `054C:0268` DS3 identity on a real PS3 (like the BT variant
  below) is unconfirmed and would need a hardware capture to settle.
- **Retro Fighters Defender (Bluetooth Edition)** starts a PS3 USB session
  enumerated as a **DualShock 4** (`054C:05C4`, `bcdDevice 0x0221`,
  467-byte report descriptor - a genuine DS4 is `bcdDevice 0x0100` with a
  483-byte descriptor, so the two are distinguishable without touching the
  controller). In that identity it answers both `0xF2` and `0xF5`
  correctly, never STALLs `SET_IDLE`, and starts streaming input reports
  before the console ever sends `0xF4`. The actual host-detection trick,
  confirmed from a real PS3 capture
  (`2024-05-14_PS3-plugin-and-rumble.pcap`,
  [CircumSpector/Research](https://github.com/CircumSpector/Research)), is
  a DS4 HID feature report:
  1. PS3 does `SET_PROTOCOL`, `SET_IDLE`, reads one interrupt IN report
     from the DS4 identity, then sends
     **`SET_REPORT Feature 0x14`, 17 bytes:
     `14 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00`.**
  2. The device stops answering interrupt IN, drops off the bus, and
     roughly 1 second later re-enumerates as a **DualShock 3**
     (`054C:0268`, `bcdUSB 1.10`, `bcdDevice 0x0100`, 148-byte descriptor,
     interrupt EP1 IN + EP2 OUT).
  3. In DS3 mode it answers `GET 0xF5` with its stored host MAC and
     otherwise follows the normal DS3 startup sequence documented above
     (`0xF4`, EP0 output report, interrupt OUT at ~16 ms).

  `2024-05-14_DS4Rev1-on-PS3.pcap` confirms the PS3 sends the exact same
  `0x14` report to a **genuine** DS4 every ~1 second for the whole
  session - the DS4 simply ignores it - so replaying this report from a
  Windows-side tool is harmless to real DS4 controllers.
  `2024-05-04_Windows-PC-plugin-capture.pcapng` confirms Windows itself
  never sends `Feature 0x14`, which is the only reason the Defender BT
  never leaves DS4 mode when plugged into a PC. Replaying that report via
  `HidD_SetFeature` is the right USB packet (Feature, report ID `0x14`,
  17 bytes starting `14 02 …`), but a successful `HidD_SetFeature` only
  means the transfer was ACKed. A live Defender in DS4 mode (`054C:05C4`
  `bcdDevice 0x0221`) can ACK the report and stay on the bus; GET Feature
  `0x14` times out (`ERROR_SEM_TIMEOUT` / 121). The PS3 sent the probe a
  few milliseconds after `SET_IDLE`, before sustained interrupt IN. A
  late probe from an already-streaming Windows session is often ignored.
  ControlApp therefore treats disappearance of the DS4 identity plus
  appearance of `USB\VID_054C&PID_0268` as the only success signal, sends
  the probe immediately on HID arrival (auto-switch or a pending retry),
  and cycles the USB port (administrator) when a late button click is
  ignored so the next enumeration can be probed while it still looks like
  a PS3 attach. `dshidmini.inf` already binds the DS3 identity.
  A follow-up on the same capture pads, with `054C:05C4` `REV_0221`
  Zadig-bound to WinUSB (service `WinUSB`, no HidUsb child, no interrupt
  IN started), sent the PS3 EP0 sequence with
  `research/ds3-motion/probe/WinUsbRaw.cs`: `SET_PROTOCOL` (report
  protocol), `SET_IDLE`, then Feature `0x14` (17 bytes `14 02 00…`).
  `WinUsb_ControlTransfer` succeeded for that sequence and for the
  variants Feature `0x14` alone, `SET_PROTOCOL` + `0x14`, and one
  interrupt IN (64-byte DS4 input starting `01 80 80 80…`) then `0x14`.
  None of them detached the DualShock 4 identity or produced
  `USB\VID_054C&PID_0268`. So a late Windows host - even when it owns
  EP0 and does not start HID polling - is not equivalent to the PS3
  attach window. The remaining gap is whatever Windows already sent
  during the original hidusb / Zadig enumeration (and possibly
  `bcdUSB 2.00` vs the PS3 session), not a mangled `0x14` payload.

  The relevant `tshark` filters used against the captures above (run
  locally; see the `analyze_pcap` limitation note below):
  ```
  tshark -r 2024-05-14_PS3-plugin-and-rumble.pcap -Y "usb.device_address == <addr> && usb.control_transfer" -T fields -e frame.number -e usb.device_address -e usb.setup.bRequest -e usb.setup.wValue -e usb.capdata
  tshark -r 2024-05-14_PS3-plugin-and-rumble.pcap -Y "usb.src == 'host' && usbll.data && usbll.pid == 0x2d" -T fields -e frame.number -e usbll.data
  ```
- **Linux `hid-sony`** and **SDL** both treat a failed `0xF2` as fatal to
  Bluetooth-address discovery (but not to basic HID functionality), never
  send `Feature 0xF4` over USB at all, force *all* USB output through EP0
  without a report ID, and defer their very first output report until
  after the first input report has been seen - because some ShanWan-brand
  clones rumble continuously and unprompted on interrupt OUT the moment
  the pipe is opened.

These observations are why DsHidMini's fallback for a device that never
answers `0xF2` synthesizes a deterministic MAC address (FNV-1a hash of the
device's PnP instance ID, locally-administered bit set) instead of failing
device start, and why the interrupt OUT pipe itself is now optional.

## `tshark` recipe

The bundled Wireshark MCP `analyze_pcap` tool could not dissect these
captures (USB link-layer, not IP/TCP) and mis-quoted the `tshark.exe` path
on this machine. Using a local Wireshark install's `tshark.exe` directly
worked well:

```powershell
# Dump every USB frame with the fields that matter for HID class requests
& 'C:\Program Files\Wireshark\tshark.exe' -r capture.pcapng `
    -Y usb `
    -T fields `
    -e frame.number -e frame.time_relative -e usbll.src -e usbll.dst `
    -e usb.setup.bRequest -e usb.setup.wValue -e usb.setup.wLength `
    -e usbhid.setup.ReportType -e usbhid.setup.ReportID `
    -e usbll.data

# usbll.src of the form "<address>.1" (endpoint 1) identifies interrupt IN
# input reports; PowerShell-side filtering on that pattern was more
# reliable than trying to express it as a tshark display filter directly
# (embedded quoting gets mangled by PowerShell either way).
```

Field notes:

- `usb.setup.bRequest` / `usb.setup.wValue` decode the HID class request
  (`GET_REPORT` = `0x01`, `SET_REPORT` = `0x09`; `wValue` high byte is the
  report type - `0x03` Feature, `0x01` Output - low byte the report ID).
- `usbhid.setup.ReportID` / `usb.setup.wLength` are the quickest way to
  spot the 48-byte, ID-less EP0 output reports versus the 49-byte,
  ID-prefixed interrupt OUT ones.
- `usbll.data` gives the raw payload bytes for manual inspection once a
  frame of interest has been located.
