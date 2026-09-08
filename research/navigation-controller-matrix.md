# Navigation Controller hardware matrix

Use this checklist before closing [issue #48](https://github.com/nefarius/DsHidMini/issues/48).
Exercise a genuine CECH-ZCS1 (`VID_054C` / `PID_042F`) on USB and Bluetooth.

## Environment

| Field | Value |
| --- | --- |
| DsHidMini build | `feat/48-navigation-controller` — `.\build.cmd Compile` succeeded 2026-09-08; ControlApp.Tests 123 passed |
| BthPS3 version | |
| Adapter | |
| Windows | |
| Date | |

## USB

| Case | Result | Notes |
| --- | --- | --- |
| INF match / device start | | |
| ControlApp shows Navigation model | | |
| Device MAC read (`0xF2`) | | |
| Host MAC read / auto pair (`0xF5`) | | |
| Custom pairing address | | |
| Input: stick, D-pad, present buttons, L2 analog, PS | | |
| Absent controls stay neutral | | |
| LED: charging slow flash | | |
| LED: full / charged solid | | |
| LED: low / dying rapid flash | | |
| No rumble side effects on output | | |
| XInput | | |
| SXS | | |
| DS4Windows | | |
| SDF | | |
| GPJ | | |
| Disconnect / hot reload | | |
| Sleep / resume | | |

## Bluetooth

| Case | Result | Notes |
| --- | --- | --- |
| BthPS3 enumerate / device start | | |
| Reconnect after USB pair | | |
| Input matches USB set | | |
| LED cadence matches USB | | |
| No rumble side effects | | |
| XInput | | |
| SXS | | |
| DS4Windows | | |
| SDF | | |
| GPJ | | |
| Disconnect / hot reload | | |
| Sleep / resume | | |

## Traces

Capture driver traces for any failed row. Do not close #48 until both USB and
Bluetooth columns are clean.
