# DsHidMini Driver

User-mode filter driver for Sony DualShock 3 controllers. This project builds the kernel component of [DsHidMini](../../README.md) that presents the controller as configurable HID devices (GamePad, Joystick, Sixaxis, DS4, XInput) over USB and Bluetooth.

## Overview

- **Framework:** [UMDF](https://docs.microsoft.com/en-us/windows-hardware/drivers/wdf/overview-of-the-umdf) (User-Mode Driver Framework), sitting below `mshidumdf.sys`
- **Core HID:** [DMF Virtual Hid Mini](https://github.com/microsoft/DMF/blob/master/Dmf/Modules.Library/Dmf_VirtualHidMini.md) for virtual HID report handling
- **Transports:** USB (`DsUsb`) and Bluetooth via [BthPS3](https://github.com/nefarius/BthPS3) (`DsBth`)
- **Configuration:** Primarily via a JSON config file and IPC (memory-mapped file) used by the configuration app

### How it works

The driver is a filter below `mshidumdf.sys`, acting as a function driver for USB and Bluetooth via the [User-mode Driver Framework Reflector](https://docs.microsoft.com/en-us/windows-hardware/drivers/wdf/detailed-view-of-the-umdf-architecture). It translates HID I/O to USB/Bluetooth I/O and presents the device as user-configurable HID modes. On Bluetooth with [BthPS3](https://github.com/nefarius/BthPS3) it replaces the need for [Shibari](https://github.com/nefarius/Shibari). Core HID handling is provided by [DMF Virtual Hid Mini](https://github.com/microsoft/DMF/blob/master/Dmf/Modules.Library/Dmf_VirtualHidMini.md).

### Environment

Designed for **Windows 10** version 1809 or newer (**x64**, **ARM64**). Dependencies are not available on Windows 7/8/8.1, so those are unsupported.

## Project layout

| Area | Description |
|------|-------------|
| `Driver.c` / `Device.c` | WDF driver/device lifecycle, device add, cleanup |
| `DsUsb.c`, `DsBth.c` | USB and Bluetooth transport; Bth uses BthPS3 stack |
| `Ds3.c`, `DsHid.c` | DualShock 3 protocol and HID translation |
| `Configuration.c` | Loading/saving device configuration (dead zones, LED, mode, etc.) |
| `HID/` | HID report descriptors (GamePad, Joystick, Sixaxis, DS4, XInput) |
| `HID.Reports.c`, `InputReport.c`, `OutputReport.c` | Report assembly and handling |
| `HID.FeatureReport.c` | Feature report handling (e.g. configuration) |
| `PID/` | Force Feedback (rumble) report definitions and types |
| `IPC.c`, `IPC.Device.c` | IPC for configuration app (shared memory, events) |
| `JSON/` | [cJSON](https://github.com/DaveGamble/cJSON) for config parsing |
| `Power.c` | Power state (D0/D3) and idle disconnect (Bluetooth) |

## Build

### Prerequisites

- Visual Studio 2026 (MSBuild 18). CI uses the `windows-2025-vs2026` image
- Windows SDK and WDK **10.0.28000** (the pair installed by the Build workflow)
- [Driver Module Framework (DMF)](https://github.com/microsoft/DMF) via the `DMF/` submodule (`nefarius` branch). `.\build.cmd Compile` builds `DmfU` automatically

### Building

Open the solution (e.g. `dshidmini.sln` in the repo root) in Visual Studio and build the **driver** project, or use the repo’s [build scripts](../../README.md#building). Supported platforms: **x64**, **ARM64**.

## Installation

Pre-built binaries and installation steps are on the [releases page](https://github.com/nefarius/DsHidMini/releases) and in the [main README](../../README.md#installation). Do not install a self-built driver unless you are testing or developing; use signed builds from the project. Maintainers producing those builds: [docs/RELEASE.md](../docs/RELEASE.md).

## Documentation

- [Main README](../../README.md) — project summary, features, build from root
- [Project docs](https://docs.nefarius.at/projects/DsHidMini/) — user and installation guides
