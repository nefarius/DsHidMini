> [!WARNING]
> **No other official website exists** for this project besides this GitHub repository (github.com/nefarius/DsHidMini) and sites hosted on `*.nefarius.at` (e.g. docs.nefarius.at, discord.nefarius.at).
>
> **Anyone claiming otherwise is a scammer and a fraud.** Do not trust other websites, download links, or people claiming to represent this project. Please be wary and only use the sources listed above.

---

# <img src="assets/FireShock.png" align="left" alt="DsHidMini logo" />DsHidMini

[![Build status](https://ci.appveyor.com/api/projects/status/vmf09i95d06c8mbh/branch/master?svg=true)](https://ci.appveyor.com/project/nefarius/dshidmini/branch/master) [![GitHub All Releases](https://img.shields.io/github/downloads/nefarius/DsHidMini/total)](https://somsubhra.github.io/github-release-stats/?username=nefarius&repository=DsHidMini) ![GitHub issues](https://img.shields.io/github/issues/nefarius/DsHidMini) [![Discord](https://img.shields.io/discord/346756263763378176.svg)](https://discord.nefarius.at/) [![Website](https://img.shields.io/website-up-down-green-red/https/docs.nefarius.at.svg?label=docs.nefarius.at)](https://docs.nefarius.at/)

Virtual HID Mini user-mode driver for Sony DualShock 3 controllers on Windows 10/11.

## Version 3 (Beta)

The next major version is [available for Beta-testing](https://github.com/nefarius/DsHidMini/releases). Highlights: new installer and configuration app, **ARM64** and **Windows 11** support, LED/dead-zone/rumble customization, Xbox One emulation, and more. Follow progress on [Discord](https://discord.nefarius.at/) or [Mastodon](https://fosstodon.org/@Nefarius).

## Repository activity

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://ghstats.api.nefarius.systems/widgets/github/nefarius/DsHidMini/changes/latest?foregroundColour=%23C4D1DE&maxCommits=10">
  <source media="(prefers-color-scheme: light)" srcset="https://ghstats.api.nefarius.systems/widgets/github/nefarius/DsHidMini/changes/latest?maxCommits=10">
  <img alt="Repository activity" src="https://ghstats.api.nefarius.systems/widgets/github/nefarius/DsHidMini/changes/latest?maxCommits=10">
</picture>

## Summary

DsHidMini is a self-contained [user-mode driver](https://docs.microsoft.com/en-us/windows-hardware/drivers/wdf/overview-of-the-umdf) for Windows 10/11 that presents Sony DualShock 3 controllers as configurable, standard-compliant HID devices. Games and apps can use [DirectInput](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416842(v=vs.85)), [Raw Input](https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input), the [HID API](https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/introduction-to-hid-concepts), or XInput (via the optional [XInput Bridge](XInputBridge/README.md)). The driver supports **USB** and **Bluetooth** (with [BthPS3](https://github.com/nefarius/BthPS3)); a configuration app lets you tune modes and behavior. Full documentation: [docs.nefarius.at/projects/DsHidMini](https://docs.nefarius.at/projects/DsHidMini/).

## Features

- **Bluetooth** with [BthPS3](https://github.com/nefarius/BthPS3) (v2.0.144+); auto-pairing and idle disconnect (5 min); quick disconnect: **L1 + R1 + PS** for 1+ second
- **HID modes:** single Gamepad (including pressure-sensitive buttons), split/multi device, Sony sixaxis emulation, **DualShock 4** (for [DS4Windows](https://github.com/Ryochan7/DS4Windows)), **Xbox / XInput** for modern games
- **Rumble** exposed as Force Feedback
- **LED** indicates battery (wired: charging 1–4; wireless: 4 = full, 1 = low)
- **Compatibility:** [PCSX2](https://pcsx2.net/), [RPCS3](https://rpcs3.net/), [DS4Windows](https://github.com/Ryochan7/DS4Windows) (v2.2.10+), [RetroArch](https://www.retroarch.com/), [x360ce](https://www.x360ce.com/), [Dolphin](https://dolphin-emu.org/), [DuckStation](https://github.com/stenzek/duckstation); see [issue #40](https://github.com/nefarius/DsHidMini/issues/40) for XInput/DS4 notes

## What's missing

See the [issue tracker](https://github.com/nefarius/DsHidMini/issues) for known bugs and in-progress work. Not currently supported (contributions welcome where noted):

- **Motion (SIXAXIS)** — gyro/accelerometer ([#217](https://github.com/nefarius/DsHidMini/issues/217))
- **Navigation Controller** — mostly done ([#48](https://github.com/nefarius/DsHidMini/issues/48))
- **Motion Controller** — not in scope

## Repository layout

| Path | Description |
|------|--------------|
| [driver/](driver/README.md) | UMDF driver: HID modes, USB/Bth, config, build (WDK, DMF) |
| [XInputBridge/](XInputBridge/README.md) | XInput proxy DLL (`XInput1_3.dll`), extended API for DS3 pressure data |
| [setup/](setup/README.md) | MSI installer (WixSharp), release packaging |
| [ControlApp/](ControlApp/) | Configuration app (WPF) |
| [docs/](docs/README.md) | R&D notes; official docs at [docs.nefarius.at](https://docs.nefarius.at/projects/DsHidMini/) |

For **how the driver works** (UMDF, DMF, config) and **build prerequisites** (Visual Studio, WDK, DMF), see [driver/README.md](driver/README.md). For the XInput Bridge build, see [XInputBridge/README.md](XInputBridge/README.md).

### Building

From the repo root, run `build.cmd` or open `dshidmini.sln` in Visual Studio. Driver and bridge build steps are in [driver/README.md](driver/README.md) and [XInputBridge/README.md](XInputBridge/README.md).

## Licensing

This solution contains **BSD-3-Clause** and other licensed components; see the individual `LICENSE` files. Community project, not affiliated with Sony Interactive Entertainment Inc. "PlayStation", "PSP", "PS2", "PS one", "DUALSHOCK", and "SIXAXIS" are registered trademarks of Sony Interactive Entertainment Inc.

## Documentation

[Project page](https://docs.nefarius.at/projects/DsHidMini/) — user guides, HID modes, installation.

## Installation

Pre-built binaries and instructions: [releases](https://github.com/nefarius/DsHidMini/releases). Installation steps: [How to Install](https://docs.nefarius.at/projects/DsHidMini/v2/How-to-Install/).

## Support

[Community support guidelines](https://docs.nefarius.at/Community-Support/).

## Sponsors

[<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/jetbrains/jetbrains-original.svg" title="JetBrains ReSharper" alt="JetBrains" width="120" height="120"/>](https://www.jetbrains.com/resharper/)

## Sources & credits

- **Related:** [ScpToolkit](https://github.com/nefarius/ScpToolkit), [FireShock](https://github.com/nefarius/FireShock), [AirBender](https://github.com/nefarius/AirBender), [WireShock](https://github.com/nefarius/WireShock), [EmuController](https://github.com/FirstPlatoLV/EmuController), [USB_Host_Shield_2.0 PS3 Info](https://github.com/felis/USB_Host_Shield_2.0/wiki/PS3-Information#USB)
- **Dependencies:** [DMF](https://github.com/microsoft/DMF), [cJSON](https://github.com/DaveGamble/cJSON), [HIDAPI](https://github.com/libusb/hidapi)
- **References:** [Eleccelerator DualShock 3](http://eleccelerator.com/wiki/index.php?title=DualShock_3), [HID Usage Tables](https://usb.org/sites/default/files/documents/hut1_12v2.pdf), [The HID Page](http://janaxelson.com/hidpage.htm), [CircumSpector/Research Sony DS3](https://github.com/CircumSpector/Research/tree/master/Sony%20DualShock%203), [linux hid-sony](https://github.com/torvalds/linux/blob/master/drivers/hid/hid-sony.c)
- **DevOps:** [AppVeyor](https://www.appveyor.com/), [NUKE](https://nuke.build/)
