# Navigation Controller

Sony PlayStation Move Navigation Controller (CECH-ZCS1, `VID_054C` /
`PID_042F`) support for [issue #48](https://github.com/nefarius/DsHidMini/issues/48).

The pad speaks the same DualShock 3 feature and input report format. It
physically exposes a subset of that report: one analog stick, D-pad, a small
face-button set, L1/L2 (L2 is analog), and the PS button. Absent controls stay
neutral. There is one LED and no rumble motors.

## Recommended HID mode

**XInput** is the recommended mode. SDF, GPJ, SXS, and DS4Windows remain
available so existing profiles and emulators can still see a partial pad.
Those modes cannot invent missing rumble, extra LEDs, the right stick, or
missing pressure axes. DS4Windows LED color translation has nothing useful
to map onto one white LED.

## LED meanings

Battery indication always uses LED 1:

| State | LED 1 |
| --- | --- |
| Unknown | Off |
| Dying / low | Rapid flash |
| Charging | Slow flash |
| Medium / high / full / charged | Solid |

Custom patterns are clamped to LED 1. The DualShock 3 four-LED USB charging
chase does not run.

## Pairing

USB pairing uses the same `GET 0xF2` / `SET 0xF5` path as DualShock 3. If the
pad answers, ControlApp pairing stays enabled. If it does not, the driver
keeps the [issue #321](https://github.com/nefarius/DsHidMini/issues/321)
soft-fail behavior (synthesized address, pairing disabled).

Bluetooth requires [BthPS3](https://github.com/nefarius/BthPS3) and the
hardware ID
`BTHPS3BUS\{206F84FC-1615-4D9F-954D-21F5A5D388C5}&Dev&VID_054C&PID_042F`.

## Hardware matrix

Record USB and Bluetooth acceptance in
[research/navigation-controller-matrix.md](../research/navigation-controller-matrix.md).
