> [!WARNING]
> **No other official website exists** for this project besides this GitHub repository (github.com/nefarius/DsHidMini) and sites hosted on `*.nefarius.at` (e.g. docs.nefarius.at, discord.nefarius.at).
>
> **Anyone claiming otherwise is a scammer and a fraud.** Do not trust other websites, download links, or people claiming to represent this project. Please be wary and only use the sources listed above.

---

# <img src="assets/FireShock.png" align="left" alt="DsHidMini logo" height="140" />DsHidMini

[![Build status](https://github.com/nefarius/DsHidMini/actions/workflows/build.yml/badge.svg)](https://github.com/nefarius/DsHidMini/actions/workflows/build.yml) [![GitHub All Releases](https://img.shields.io/github/downloads/nefarius/DsHidMini/total)](https://somsubhra.github.io/github-release-stats/?username=nefarius&repository=DsHidMini) ![GitHub issues](https://img.shields.io/github/issues/nefarius/DsHidMini) [![Discord](https://img.shields.io/discord/346756263763378176.svg)](https://discord.nefarius.at/) [![Website](https://img.shields.io/website-up-down-green-red/https/docs.nefarius.at.svg?label=docs.nefarius.at)](https://docs.nefarius.at/)

Virtual HID Mini user-mode driver for Sony DualShock 3/SIXAXIS and Navigation
controllers, plus the ShanWan PS1/PS2 USB adapter, on Windows 10/11.

## Version 3 (Stable)

Version 3 is the current stable release. Get the latest build from [releases](https://github.com/nefarius/DsHidMini/releases). Highlights include a modern installer and configuration app, **ARM64** and **Windows 11** support, calibrated motion telemetry, LED/dead-zone/rumble customization, and Xbox One emulation. Support and updates are available on [Discord](https://discord.nefarius.at/) or [Mastodon](https://fosstodon.org/@Nefarius).

## Repository activity

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://ghstats.api.nefarius.systems/widgets/github/nefarius/DsHidMini/changes/latest?foregroundColour=%23C4D1DE&maxCommits=10">
  <source media="(prefers-color-scheme: light)" srcset="https://ghstats.api.nefarius.systems/widgets/github/nefarius/DsHidMini/changes/latest?maxCommits=10">
  <img alt="Repository activity" src="https://ghstats.api.nefarius.systems/widgets/github/nefarius/DsHidMini/changes/latest?maxCommits=10">
</picture>

## Summary

DsHidMini is a self-contained [user-mode driver](https://learn.microsoft.com/windows-hardware/drivers/wdf/overview-of-the-umdf) for Windows 10/11 that presents Sony DualShock 3/SIXAXIS and Navigation controllers, and the ShanWan PS1/PS2 USB adapter, as configurable, standard-compliant HID devices. Games and apps can use [DirectInput](https://learn.microsoft.com/previous-versions/windows/desktop/ee416842(v=vs.85)), [Raw Input](https://learn.microsoft.com/windows/win32/inputdev/raw-input), the [HID API](https://learn.microsoft.com/windows-hardware/drivers/hid/introduction-to-hid-concepts), or XInput (via the optional [XInput Bridge](XInputBridge/README.md)). The driver supports **USB** and **Bluetooth** through [BthPS3](https://github.com/nefarius/BthPS3); the ControlApp configures modes and device behavior. Full documentation: [docs.nefarius.at/projects/DsHidMini](https://docs.nefarius.at/projects/DsHidMini/).

## Features

- **Bluetooth** with [BthPS3](https://github.com/nefarius/BthPS3)
  (v2.0.144+); configurable pairing, idle disconnect, and quick-disconnect
  combo (defaults: 5 minutes and **L1 + R1 + PS** held for 1 second)
- **HID modes:** single Gamepad (including pressure-sensitive buttons), split/multi device, Sony sixaxis emulation, **DualShock 4** (for [DS4Windows](https://github.com/Ryochan7/DS4Windows)), **Xbox / XInput** for modern games, and **Common Gamepad (CGP)** — a single DirectInput-friendly device without pressure-sensitive button sliders, for older games with limited axis support (see [issue #68](https://github.com/nefarius/DsHidMini/issues/68))
- **Rumble** exposed as Force Feedback
- **LEDs** — player-index and bar-graph battery modes, charging animation,
  custom static/flashing patterns, and application-controlled output
- **Motion** — calibrated accelerometer and single-axis gyroscope processing,
  live telemetry, and a ControlApp viewer; Bluetooth reuses calibration cached
  by the controller's USB instance ([implementation notes](docs/MOTION.md))
- **Device management** — controller identification and capability reporting,
  pairing to the current host, Bluetooth disconnect, runtime rumble/LED
  controls, and wired USB power-off
- **Compatibility:** [PCSX2](https://pcsx2.net/), [RPCS3](https://rpcs3.net/), [DS4Windows](https://github.com/Ryochan7/DS4Windows) (v2.2.10+), [RetroArch](https://www.retroarch.com/), [x360ce](https://www.x360ce.com/), [Dolphin](https://dolphin-emu.org/), [DuckStation](https://github.com/stenzek/duckstation); see [issue #40](https://github.com/nefarius/DsHidMini/issues/40) for XInput/DS4 notes
- **Navigation Controller** — supported ([#48](https://github.com/nefarius/DsHidMini/issues/48)); one LED, no rumble; see [docs/NAVIGATION_CONTROLLER.md](docs/NAVIGATION_CONTROLLER.md)
- **ShanWan PS1/PS2 USB adapter** (`VID_2563` / `PID_0575`) — buttons, hat, sticks, pressure, and rumble; no LEDs or motion; see [docs/THIRD_PARTY_HID_ADAPTER.md](docs/THIRD_PARTY_HID_ADAPTER.md)

## Unsupported hardware

The PlayStation Move Motion Controller is not in scope. See the
[issue tracker](https://github.com/nefarius/DsHidMini/issues) for known bugs
and in-progress work.

## Repository layout

| Path | Description |
|------|--------------|
| [driver/](driver/README.md) | UMDF driver: HID modes, USB/Bth, config, build (WDK, DMF) |
| [XInputBridge/](XInputBridge/README.md) | XInput proxy DLL (`XInput1_3.dll`), extended API for DS3 pressure data |
| [setup/](setup/README.md) | MSI installer (WixSharp). Release procedure: [docs/RELEASE.md](docs/RELEASE.md) |
| [ControlApp/](ControlApp/README.md) | Configuration and motion-viewer app (WPF, .NET 10) |
| [SDK/Nefarius.DsHidMini.IPC/](SDK/Nefarius.DsHidMini.IPC/README.md) | .NET IPC SDK for reports, motion telemetry, and runtime commands |
| [DSHMC/](DSHMC/) | **Deprecated** legacy control utility. Use [ControlApp/](ControlApp/) |
| [docs/](docs/README.md) | R&D notes; official docs at [docs.nefarius.at](https://docs.nefarius.at/projects/DsHidMini/) |

For **how the driver works** (UMDF, DMF, config) and **build prerequisites** (Visual Studio, WDK, DMF), see [driver/README.md](driver/README.md). For the XInput Bridge build, see [XInputBridge/README.md](XInputBridge/README.md).

### Building

From the repository root, run `.\build.cmd Compile`. The NUKE build prepares
DMF and rebuilds the solution; pass `--configuration Release` for a release
build. Component-specific targets and tests are documented in
[driver/README.md](driver/README.md) and
[XInputBridge/README.md](XInputBridge/README.md). Tagged production releases
(Partner Center CAB, attested drivers, MSI) are documented in
[docs/RELEASE.md](docs/RELEASE.md).

## Licensing

This solution contains **BSD-3-Clause** and other licensed components; see the individual `LICENSE` files. Community project, not affiliated with Sony Interactive Entertainment Inc. "PlayStation", "PSP", "PS2", "PS one", "DUALSHOCK", and "SIXAXIS" are registered trademarks of Sony Interactive Entertainment Inc.

## Documentation

[Project page](https://docs.nefarius.at/projects/DsHidMini/) — user guides, HID modes, installation.

## Installation

Pre-built binaries and instructions: [releases](https://github.com/nefarius/DsHidMini/releases). Installation steps: [How to Install](https://docs.nefarius.at/projects/DsHidMini/v3/How-to-Install/).

## Support

[Community support guidelines](https://docs.nefarius.at/Community-Support/).

## Sponsors

[<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/jetbrains/jetbrains-original.svg" title="JetBrains ReSharper" alt="JetBrains" width="120" height="120"/>](https://www.jetbrains.com/resharper/)

## Sources & credits

- **Related:** [ScpToolkit](https://github.com/nefarius/ScpToolkit), [FireShock](https://github.com/nefarius/FireShock), [AirBender](https://github.com/nefarius/AirBender), [WireShock](https://github.com/nefarius/WireShock), [EmuController](https://github.com/FirstPlatoLV/EmuController), [USB_Host_Shield_2.0 PS3 Info](https://github.com/felis/USB_Host_Shield_2.0/wiki/PS3-Information#USB)
- **Dependencies:** [DMF](https://github.com/microsoft/DMF), [cJSON](https://github.com/DaveGamble/cJSON), [HIDAPI](https://github.com/libusb/hidapi)
- **References:** [Eleccelerator DualShock 3](http://eleccelerator.com/wiki/index.php?title=DualShock_3), [HID Usage Tables](https://usb.org/sites/default/files/documents/hut1_12v2.pdf), [The HID Page](http://janaxelson.com/hidpage.htm), [CircumSpector/Research Sony DS3](https://github.com/CircumSpector/Research/tree/master/Sony%20DualShock%203), [linux hid-sony](https://github.com/torvalds/linux/blob/master/drivers/hid/hid-sony.c)
- **DevOps:** [GitHub Actions](https://github.com/features/actions), [NUKE](https://nuke.build/)
