# Bluetooth output transport hardware matrix

Record control-transport baseline results, then repeat with interrupt transport.
Keep `BluetoothOutputReportTransport` at `Control` as the shipped default until
every exercised row is clean on both settings.

Related: [PR #460](https://github.com/nefarius/DsHidMini/pull/460) (interrupt
channel for Bluetooth rumble).

## Automated verification (2026-09-08)

- NUKE `Compile` succeeded on `fix/bth-output-transport` (DMF + driver + ControlApp).
- `ControlApp.Tests`: 72 passed, including default `Control`, Interrupt round-trip,
  legacy JSON without the key, and overlay/copy coverage.
- GitHub x86/x64/ARM64 CI runs when this branch is pushed. Local NUKE does not
  replace those platform legs.

## Default decision

Do **not** change the shipped default to Interrupt until this matrix is clean.
Control remains the default so established pads keep today's Bluetooth behavior.

2026-09-17: four genuine DualShock 3 revisions failed Bluetooth rumble on
Interrupt. The default stays Control.

## Environment

| Field | Value |
| --- | --- |
| DsHidMini build | |
| BthPS3 version | |
| Adapter | |
| Windows | |
| Date | 2026-09-17 (genuine Sony Interrupt pass) |

## Recorded results

### 2026-09-17 — genuine Sony, Bluetooth Interrupt

Physical genuine DualShock 3 collection. Question: does Bluetooth output over
the interrupt channel (`0xA2` + `IOCTL_BTHPS3_HID_INTERRUPT_WRITE`) actuate
rumble?

| Controller (model/rev) | Short | BTH Interrupt rumble |
| --- | --- | --- |
| CECHZC2E-A1 | 2E A1 | No |
| CECHZC2E | 2E | No |
| CECHZC2U-A2 | 2U A2 | No |
| CECHZC2E-B1 | 2E B1 | No |

Control baseline, USB, HID-mode sweep, LED/authority, rate-limit, and
reconnect were not recorded in this pass.

## Cases

For each controller: set HID mode, connect, run the case on **Control**, then
hot-reload to **Interrupt** and repeat. Switching back to Control must restore
the baseline without reinstalling the driver.

| Controller (model/rev) | Connection | HID mode | Transport | Rumble both motors | Finite / sustain / stop | LEDs / authority | Rate-limit / keep-alive | Reconnect / USB-BT / sleep | Result |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| CECHZC2E-A1 (genuine) | Bluetooth | | Interrupt | No | | | | | Fail |
| CECHZC2E (genuine) | Bluetooth | | Interrupt | No | | | | | Fail |
| CECHZC2U-A2 (genuine) | Bluetooth | | Interrupt | No | | | | | Fail |
| CECHZC2E-B1 (genuine) | Bluetooth | | Interrupt | No | | | | | Fail |
| | USB | XInput | n/a | | | | | | |
| | Bluetooth | XInput | Control | | | | | | |
| | Bluetooth | XInput | Interrupt | | | | | | |
| | Bluetooth | DS4Windows | Control | | | | | | |
| | Bluetooth | DS4Windows | Interrupt | | | | | | |
| | Bluetooth | SXS | Control | | | | | | |
| | Bluetooth | SXS | Interrupt | | | | | | |
| | Bluetooth | SDF | Control | | | | | | |
| | Bluetooth | SDF | Interrupt | | | | | | |
| | Bluetooth | GPJ | Control | | | | | | |
| | Bluetooth | GPJ | Interrupt | | | | | | |

Include at least: multiple genuine DS3/SIXAXIS revisions, a Navigation
controller, and one third-party clone. Devices without rumble still need
unchanged LEDs, input, and connection behavior.

Still needed: SIXAXIS, Navigation, and a third-party clone. Control baseline
and HID-mode sweep for the four genuine pads above.

## Stress

- Rapid rumble on/off and LED changes around the 150 ms rate limit
- Deduplication / keep-alive while rumble is held
- Disconnect while rumbling
- Capture driver traces and, when available, Bluetooth traffic showing
  `0x52` + `IOCTL_BTHPS3_HID_CONTROL_WRITE` or `0xA2` +
  `IOCTL_BTHPS3_HID_INTERRUPT_WRITE`

## Acceptance

- Interrupt must fix the affected pad(s) and not regress the per-device
  Control baseline.
- Reverting to Control must restore established behavior without reinstalling.
- Changing the default from Control to Interrupt is a separate review after
  this matrix is clean.
- 2026-09-17: Interrupt is **not** clean on genuine CECHZC2E-A1, CECHZC2E,
  CECHZC2U-A2, or CECHZC2E-B1. Do not flip the default.
