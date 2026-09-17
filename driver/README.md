# DsHidMini Driver

This directory contains the [DsHidMini](../README.md) user-mode driver for Sony
DualShock 3/SIXAXIS and Navigation controllers. It presents a physical
controller as one or more configurable HID devices over USB or Bluetooth.

## Overview

- **Driver model:** UMDF 2 (User-Mode Driver Framework) HID minidriver
- **Core HID:** [DMF Virtual Hid Mini](https://github.com/microsoft/DMF/blob/master/Dmf/Modules.Library/Dmf_VirtualHidMini.md)
  provides virtual HID report handling
- **Transports:** native USB and Bluetooth through
  [BthPS3](https://github.com/nefarius/BthPS3)
- **Platforms:** Windows 10 version 1809 or newer and Windows 11, on x64 and
  ARM64
- **Configuration:** `%ProgramData%\DsHidMini\DsHidMini.json`, with global and
  per-device settings plus runtime IPC used by the ControlApp and SDK

### How it works

`dshidmini.dll` runs in a user-mode driver host. It sits below the inbox
`mshidumdf.sys` HID class minidriver and communicates with the device through
the [User-Mode Driver Framework Reflector](https://learn.microsoft.com/windows-hardware/drivers/wdf/detailed-view-of-the-umdf-architecture).
The reflector and HID class components include kernel-mode Windows drivers, but
DsHidMini itself is a user-mode driver.

The driver translates USB or BthPS3 I/O into the selected virtual HID report
format. On Bluetooth, BthPS3 supplies the transport, so the legacy Shibari
service is not required.

## Capabilities

- Five HID modes: single custom gamepad (SDF), split gamepad and joystick
  (GPJ), Sony SIXAXIS-compatible (SXS), DualShock 4-compatible, and Xbox
  One/XInput-compatible HID
- Pressure-sensitive buttons, configurable D-pad exposure, stick dead zones,
  axis inversion, and per-mode settings
- Force Feedback rumble, runtime rumble control, alternative rumble behavior,
  output rate limiting, and output deduplication
- Battery-aware player LEDs, custom LED patterns, and runtime player-index and
  LED updates
- Automatic or custom USB pairing, pairing to the current Bluetooth host,
  wireless disconnect, idle timeout, quick-disconnect button combo, and wired
  USB power-off
- Device capability and identification reporting, including special handling
  for the one-LED, no-rumble Navigation Controller
- Calibrated accelerometer and gyroscope processing for supported DualShock 3
  controllers. USB calibration is persisted for reuse by the Bluetooth
  instance, and live motion snapshots are available through IPC. See
  [Motion support](../docs/MOTION.md)
- Runtime configuration reload and IPC for device status, HID input reports,
  motion data, pairing, disconnect, rumble, and LED commands. IPC can be
  enabled or disabled with the root-level `IPCEnabled` setting

Changing settings that alter the exposed HID descriptor requires the device to
restart. The driver can perform this automatically when
`AutoRestartOnHidModeMismatch` is enabled.

## Project layout

| Area | Description |
|------|-------------|
| `Driver.c`, `Device.c`, `DsHidMiniDrv.c` | WDF lifecycle, device setup, queues, and HID callbacks |
| `DsUsb.c`, `DsBth.c`, `DsBth.Timers.c` | USB and BthPS3 transports, initialization, and wireless timers |
| `Ds3.c`, `DsHid.c` | Controller protocol and HID translation |
| `DsIdentification.c`, `DsMotion.c` | Controller identification, calibration, and motion processing |
| `DsLed.c`, `OutputReport.c` | LEDs, rumble, and transport-specific output |
| `Configuration.c`, `Configuration.Json.c` | JSON defaults, validation, overrides, and runtime reload |
| `HID/` | HID report descriptors (GamePad, Joystick, Sixaxis, DS4, XInput) |
| `HID.Reports.c`, `InputReport.c`, `OutputReport.c` | Report assembly and handling |
| `HID.FeatureReport.c` | HID feature report handling |
| `PID/` | Physical Interface Device Force Feedback reports and types |
| `IPC.c`, `IPC.Device.c` | Shared-memory IPC, report events, and device commands |
| `JSON/` | [cJSON](https://github.com/DaveGamble/cJSON) for config parsing |
| `Power.c` | D0/D3 transitions and Bluetooth idle disconnect |
| `dshidmini.inf` | Device matching and Windows 10/11 UMDF installation |

## Build

### Prerequisites

- Visual Studio 2026 with MSBuild 18 and the C++ desktop workload
- Windows SDK and WDK 10.0.28000
- The repository submodules, including
  [Driver Module Framework (DMF)](https://github.com/microsoft/DMF)

### Building

From the repository root, run:

```powershell
.\build.cmd Compile
```

The NUKE build compiles the required user-mode `DmfU` libraries before
rebuilding the solution. Local builds default to Debug; pass
`--configuration Release` for a release build.

Useful validation targets include:

```powershell
.\build.cmd TestConfigParser
.\build.cmd TestControlApp
```

## Installation

Pre-built binaries and installation steps are on the
[releases page](https://github.com/nefarius/DsHidMini/releases) and in the
[main README](../README.md#installation). Do not install a self-built driver
unless you are testing or developing; use signed project releases instead.
Maintainer release steps are documented in
[docs/RELEASE.md](../docs/RELEASE.md).

## Documentation

- [Main README](../README.md) — project summary, features, and installation
- [Project docs](https://docs.nefarius.at/projects/DsHidMini/) — user and installation guides
- [Motion support](../docs/MOTION.md) — calibration paths and implementation notes
- [Navigation Controller](../docs/NAVIGATION_CONTROLLER.md) — hardware-specific behavior
