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

Designed for **Windows 10** version 1809 or newer (x86, x64, ARM64). Dependencies are not available on Windows 7/8/8.1, so those are unsupported.

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

- [Visual Studio 2022](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-1-install-visual-studio-2022)
- [Windows 11 22H2 SDK](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-2-install-windows-11-version-22h2-sdk)
- [Windows 11 22H2 WDK](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-3-install-windows-11-version-22h2-wdk)
- [Driver Module Framework (DMF)](https://github.com/microsoft/DMF) **v1.1.83** or newer, cloned as a sibling of the DsHidMini repo and built (e.g. build `DmfU` for Release/Debug, x64 and Win32 as needed)

### Building

Open the solution (e.g. `dshidmini.sln` in the repo root) in Visual Studio and build the **driver** project, or use the repo’s [build scripts](../../README.md#building). Supported platforms: **x64**, **ARM64**.

## Installation

Pre-built binaries and installation steps are on the [releases page](https://github.com/nefarius/DsHidMini/releases) and in the [main README](../../README.md#installation). Do not install a self-built driver unless you are testing or developing; use signed builds from the project.

## Documentation

- [Main README](../../README.md) — project summary, features, build from root
- [Project docs](https://docs.nefarius.at/projects/DsHidMini/) — user and installation guides
