<img src="assets/FireShock.png" align="right" />

# DsHidMini

Virtual HID Mini-user-mode driver for Sony DualShock 3 Controllers

[![Build status](https://ci.appveyor.com/api/projects/status/vmf09i95d06c8mbh/branch/master?svg=true)](https://ci.appveyor.com/project/nefarius/dshidmini/branch/master) [![GitHub All Releases](https://img.shields.io/github/downloads/ViGEm/DsHidMini/total)](https://somsubhra.github.io/github-release-stats/?username=ViGEm&repository=DsHidMini) ![GitHub issues](https://img.shields.io/github/issues/ViGEm/DsHidMini) [![Discord](https://img.shields.io/discord/346756263763378176.svg)](https://discord.nefarius.at/) [![Website](https://img.shields.io/website-up-down-green-red/https/docs.nefarius.at.svg?label=docs.nefarius.at)](https://docs.nefarius.at/)

## 🚨 What to expect with Version 3 🚨

Next major version is in progress! Here's an overview of what you'll get:

- New driver installer
- New configuration app
- **ARM64 support**
- Full **Windows 11** compatibility
- Complete LED customization
- Customize dead-zone thresholds
- Customize HAT/POV behaviour
- Adjust rumble strengths
- Switch DPad to individual buttons
- Customize the Quick-Disconnect button combination
- Configure flipping/inverting axes
- ...and maybe more?

Stay tuned!

<!--
![](https://us-central1-progress-markdown.cloudfunctions.net/progress/50)
-->

## Summary

DsHidMini is a self-contained, low footprint and feature-rich [user-mode driver](https://docs.microsoft.com/en-us/windows-hardware/drivers/wdf/overview-of-the-umdf) for Microsoft Windows 10/11. It presents the controller as a configurable variety of fully standard-compliant HID devices to the system and all games built on common APIs like [DirectInput](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416842(v=vs.85)), [Raw Input](https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input) and the low-level [HID API](https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/introduction-to-hid-concepts). XInput-emulation further increases the support in modern games built with only Xbox controllers in mind. The driver supports both wired connections by handling USB communication and wireless connections by building upon the [BthPS3](https://github.com/ViGEm/BthPS3) driver suite. An optional .NET configuration tool is provided to alter driver behavior to fine-tune it to specific games or other use-cases.

## Features

- **Bluetooth support** if used in conjunction with [BthPS3](https://github.com/ViGEm/BthPS3) (requires *at least* `v2.0.144` or newer)
- Automatically pairs the controller to Windows Bluetooth (if Bluetooth host radio is present)
- Multiple configurable HID Report Descriptors for wide range of compatibility
  - Single Gamepad device exposing all controls including **pressure sensitive buttons**
  - Split/multi device emulation to overcome DirectInput axis limits
  - Sony `sixaxis.sys` emulation (both wired **and wireless**)
  - **DualShock 4 emulation** for compatibility with [DS4Windows](https://github.com/Ryochan7/DS4Windows)
  - **Xbox Controller emulation** (XInput) for best compatibility with most modern games
- Quick disconnect (on Bluetooth) by pressing `L1 + R1 + PS` together for over one second
- Automatic disconnect (on Bluetooth) after idle timeout (5 minutes) expired to conserve battery
- Custom LED states indicate battery charge level
  - Wired: Charging will cycle through 1 to 4, if fully charged will stay on 4
  - Wireless: 4 = Full, 3 = High, 2 = Medium/low, 1 = Low/dying
- **Rumble exposure via Force Feedback**
  - The rumble motors are exposed as Force Feedback effects, allowing for great game compatibility
- Supports the [**PCSX2 PlayStation 2 Emulator**](https://pcsx2.net/)
  - Controller gets picked up by LilyPad plugin with all device features
- Supports the [**RPCS3 PlayStation 3 Emulator**](https://rpcs3.net/)
  - Controller gets picked up by DualShock 3 handler with all device features
- Supports [**DS4Windows**](https://github.com/Ryochan7/DS4Windows) (requires *at least* Version 2.2.10 or newer)
  - Controller gets presented as a DualShock 4 compatible variant
  - Read [#40](../../issues/40) for details about XInput and DS4 emulation
- Supports [**RetroArch**](https://www.retroarch.com/) emulation platform
- Supports [**x360ce**](https://www.x360ce.com/) for XInput emulation
- Supports [**Dolphin Emulator**](https://dolphin-emu.org/)
- Supports [**DuckStation - PlayStation 1, aka. PSX Emulator**](https://github.com/stenzek/duckstation)

## What's missing

Check the ⚡ [**issue tracker**](https://github.com/ViGEm/DsHidMini/issues) ⚡ for work-in-progress or known bugs!

The following features are **not** available (and most probably won't in the near future until more contributors join the party):

- Motion controls a.k.a. SIXAXIS (Gyroscope, Accelerometer)
  - **Contributions welcome!**
  - See [#217](https://github.com/ViGEm/DsHidMini/issues/217)
- Navigation Controller
  - Majority is done
- Motion Controller
  - Not considered in design at all

For in-progress features and bug-fixes please consult the issue tracker.

<details>

<summary>Technical details</summary>

## How it works

DsHidMini is a filter driver sitting below `mshidumdf.sys` and acts as a function driver for USB and Bluetooth through the [User-mode Driver Framework Reflector](https://docs.microsoft.com/en-us/windows-hardware/drivers/wdf/detailed-view-of-the-umdf-architecture), handling translation of incoming HID I/O traffic to underlying USB/Bluetooth I/O and vice versa. On USB it replaces the Windows stock drivers for the Sony hardware and presents the device as a variety of user-configurable HID devices (see documentation). On Bluetooth in conjunction with BthPS3 it replaces the need for [Shibari](https://github.com/ViGEm/Shibari) as the driver directly communicates over wireless channels and takes care of the necessary translation logic. As a user-mode driver it has limited access to the registry, therefore device-specific settings are stored and retrieved using the [Unified Device Property Model](https://docs.microsoft.com/en-us/windows-hardware/drivers/install/unified-device-property-model--windows-vista-and-later-) API. Most of the core HID heavy lifting is done by the amazing [DMF_VirtualHidMini](https://github.com/microsoft/DMF/blob/master/Dmf/Modules.Library/Dmf_VirtualHidMini.md) module which greatly reduced the need for boilerplate code and sped up development tremendously.

## Environment

DsHidMini components (drivers, utilities) are designed for **Windows 10**, version 1809 or newer (x86, x64).

The dependencies used in DsHidMini don't exist in Windows 7/8/8.1 so they can't be supported.

## How to build

### Prerequisites

- [Step 1: Install Visual Studio 2022](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-1-install-visual-studio-2022)
- [Step 2: Install Windows 11, version 22H2 SDK](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-2-install-windows-11-version-22h2-sdk)
- [Step 3: Install Windows 11, version 22H2 WDK](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-3-install-windows-11-version-22h2-wdk)
- [Step 4: Clone the Driver Module Framework (DMF)](https://github.com/microsoft/DMF) into the same parent directory.
  - **Important:** requires *at least* [`v1.1.83`](https://github.com/microsoft/DMF/releases/tag/v1.1.83) or newer
  - Build the `DmfU` project with Release and Debug configurations for all architectures (x64 and Win32).

You can build individual projects of the solution within Visual Studio.

</details>

## Licensing

This solution contains **BSD-3-Clause** and other licensed components. For details, please consult the individual `LICENSE` files.

This is a community project and not affiliated with Sony Interactive Entertainment Inc. in any way.

"PlayStation", "PSP", "PS2", "PS one", "DUALSHOCK" and "SIXAXIS" are registered trademarks of Sony Interactive Entertainment Inc.

## Documentation

Take a look at the [project page](https://docs.nefarius.at/projects/DsHidMini/) for more information.

## Installation

Pre-built binaries and instructions are provided [on the releases page](../../releases).

## Support

To get support [please follow these guidelines](https://docs.nefarius.at/Community-Support/).

## Sponsors

[<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/jetbrains/jetbrains-original.svg" title="JetBrains ReSharper" alt="JetBrains" width="120" height="120"/>](https://www.jetbrains.com/resharper/)

## Sources & 3rd party credits

The following awesome resources have made this project possible.

### Related projects

- [nefarius/ScpToolkit](https://github.com/nefarius/ScpToolkit)
- [nefarius/FireShock](https://github.com/nefarius/FireShock)
- [nefarius/AirBender](https://github.com/nefarius/AirBender)
- [nefarius/WireShock](https://github.com/nefarius/WireShock)
- [FirstPlatoLV/EmuController](https://github.com/FirstPlatoLV/EmuController)
- [Arduino - felis/USB_Host_Shield_2.0 - PS3 Information](https://github.com/felis/USB_Host_Shield_2.0/wiki/PS3-Information#USB)

### Dependencies

- [Microsoft/Driver Module Framework (DMF)](https://github.com/microsoft/DMF)
- [Microsoft - DMF - HID minidriver module](https://github.com/microsoft/DMF/issues/69)
- [Microsoft - DMF - VHidMini2DmfK and VHidMini2DmfU Sample Drivers](https://github.com/microsoft/DMF/tree/master/DmfSamples/VHidMini2Dmf)
- [Ultralightweight JSON parser in ANSI C](https://github.com/DaveGamble/cJSON)
- [HIDAPI - Multi-Platform library for communication with HID devices](https://github.com/libusb/hidapi)

### Tools & references

- [Eleccelerator Wiki - DualShock 3](http://eleccelerator.com/wiki/index.php?title=DualShock_3)
- [Eleccelerator - USB Descriptor and Request Parser](http://eleccelerator.com/usbdescreqparser/)
- [HID Usage Tables](https://usb.org/sites/default/files/documents/hut1_12v2.pdf)
- [PS3 and Wiimote Game Controllers on the Arduino Host Shield: Part 2](https://web.archive.org/web/20160326093555/https://www.circuitsathome.com/mcu/ps3-and-wiimote-game-controllers-on-the-arduino-host-shield-part-2)
- [ribbotson/USB-Host](https://github.com/ribbotson/USB-Host/tree/master/ps3/PS3USB)
- [HID: sony: Update device ids](https://patchwork.kernel.org/patch/9367441/)
- [linux/drivers/hid/hid-sony.c](https://github.com/torvalds/linux/blob/master/drivers/hid/hid-sony.c)
- [The HID Page](http://janaxelson.com/hidpage.htm)
