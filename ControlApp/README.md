# <img src="../assets/FireShock.png" align="left" /> DsHidMini ControlApp

![Requirements](https://img.shields.io/badge/Requires-.NET%2010-blue.svg)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-lightgrey.svg)

**Desktop control application for the DsHidMini driver.**  
Manage connected SIXAXIS/DualShock 3, Navigation, and compatible controllers,
configure HID modes and per-device settings, and maintain DsHidMini
profiles—all from a modern WPF UI (WPF-UI / Fluent).

---

## Table of contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Building](#building)
- [Publishing](#publishing)
- [Features](#features)
- [Configuration](#configuration)
- [Elevation and permissions](#elevation-and-permissions)

---

## Overview

DsHidMini ControlApp is the official Windows desktop companion for the
[DsHidMini](https://github.com/nefarius/DsHidMini) user-mode driver. It uses
the [Nefarius.DsHidMini.IPC](https://www.nuget.org/packages/Nefarius.DsHidMini.IPC/)
SDK to communicate with the driver and provides:

- **Devices** — View connected controllers and their capabilities, edit
  per-device settings, pair or disconnect Bluetooth devices, control LEDs and
  rumble, power off a wired controller, inspect controller identification, and
  open the live motion viewer.
- **Profiles** — Manage DsHidMini configuration profiles (including a global default), assign profiles to devices, and edit profile-specific settings.
- **Settings** — HID-mode restart behavior, tray behavior, Defender BT
  auto-switching, driver IPC, and BthPS3 status/repair. The global profile is
  available from the main navigation.

Application preferences are stored under **%AppData%**. Driver configuration
and ControlApp profile data are stored under **%ProgramData%\DsHidMini**. The
app does not require administrator rights to run. After installation, a
standard user can edit the shared configuration. Device restart over USB,
Bluetooth-stack repair, and the driver trace still require elevation.

---

## Requirements

| Requirement | Details |
|-------------|---------|
| **.NET** | .NET 10 Desktop Runtime (x64) |
| **OS** | Windows 10 or later (targeting `net10.0-windows10.0.17763.0`) |
| **Driver** | [DsHidMini](https://github.com/nefarius/DsHidMini) driver installed; at least one compatible controller connected for full functionality |
| **Architecture** | AnyCPU or x64 (NUKE publish uses win-x64) |

---

## Building

Use the repository's NUKE build from the repository root:

```powershell
.\build.cmd Compile
```

Local builds default to Debug. Pass `--configuration Release` for a release
build. To run the managed ControlApp and IPC tests, use
`.\build.cmd TestControlApp`.

---

## Publishing

Production-ready, single-file, framework-dependent publish for Windows x64 is done via the NUKE build. From the repository root:

```powershell
.\build.ps1 PublishControlApp
```

This restores and publishes the ControlApp and its project dependencies to the
repository `bin` folder. The NUKE task uses:

| Option | Value |
|--------|--------|
| **Configuration** | Release |
| **Runtime** | win-x64 |
| **Self-contained** | false (framework-dependent) |
| **Single-file** | true |
| **Publish directory** | `bin\` (at repo root) |

---

## Features

### Devices page

- **Device list** — All DsHidMini devices currently connected; selection drives the detail panel.
- **Per-device settings** — Edit settings for the selected device when the shared configuration is writable:
  - **HID mode** — SDF, GPJ, SXS, DS4Windows, XInput.
  - **LEDs** — Mode and timing (e.g. battery indicator, player index).
  - **Wireless** — Pairing mode, custom host address, idle timeout, quick-disconnect combo.
  - **Sticks** — Deadzone and polar value.
  - **Rumble** — General on/off, left-motor rescale, alternative rumble mode.
  - **Output report** — Rate control, deduplication, and Bluetooth output transport.
- **Reconnect** — Reconnect devices when the active HID mode no longer matches the driver config (e.g. after changing the global profile).
- **Runtime actions** — Pair to a specified or current host, disconnect a
  wireless controller, apply LED and rumble output, or send the wired USB
  power-off sequence.
- **Identification and motion** — Show firmware, controller type, motion path,
  and clone heuristic data, and display calibrated accelerometer/gyroscope
  telemetry in a live 3D viewer.
- **Hardware-aware controls** — Navigation Controllers are treated as
  one-LED, no-rumble devices and incompatible controls are disabled.

### Profiles page

- **Profile list** — All DsHidMini profiles plus the built-in default; select one to view or edit.
- **Global profile** — Choose which profile is used for new devices and for devices using “global settings”.
- **Per-profile editing** — Same setting categories as per-device (HID mode, LEDs, wireless, sticks, rumble, output report); changes apply when the profile is used.

### Settings page

- **App version** — Displayed in the title bar and About page.
- **Automatically restart devices on HID mode mismatch** — Driver self-restart when the loaded HID mode does not match the mode Windows already probed.
- **Minimize to tray** — When enabled, Minimize and Close hide ControlApp to the notification area; use the tray icon to reopen or Exit to quit.
- **Automatically switch Defender BT** — Detect a Retro Fighters Defender
  Bluetooth Edition in DS4 mode and switch it to PS3 mode on arrival.
- **Enable driver IPC** — Persist the driver-wide `IPCEnabled` flag in `DsHidMini.json`. A current driver applies the change without a reload once a controller is connected, because the driver loads on demand. If a controller is connected and the saved setting or the driver file says IPC is on, but the running driver does not answer a ping, Settings says so. That usually means the previous driver is still loaded after an update until Windows restarts.
- **BthPS3 status** — Show the installed version and required Bluetooth stack
  settings, with an elevated repair action when needed.
- **Application configuration** — Stored in %AppData%; see [Configuration](#configuration).

---

## Configuration

Application-level options are stored in `%AppData%\ControlApp.json` (see
`ApplicationConfiguration.GlobalConfigFileName`). The model is in
`Models\ApplicationConfiguration.cs`. Examples:

| Property | Description | Default |
|----------|-------------|---------|
| **IsUpdateCheckEnabled** | Check for updates on startup | `true` |
| **AutoSwitchDefenderBtToPs3Mode** | Switch a detected Retro Fighters Defender Bluetooth Edition from DS4 to PS3 mode | `false` |
| **HasAcknowledgedDonationDialog** | User ticked that they already donated or will consider it, hiding the startup donation prompt | `false` |
| **MinimizeToTray** | Hide to the notification area on Minimize or Close | `false` |

The files managed by the app are:

- `%AppData%\ControlApp.json` — application preferences
- `%ProgramData%\DsHidMini\DsHidMini.json` — configuration consumed by the
  driver
- `%ProgramData%\DsHidMini\ControlApp\DshmUserData.json` — profiles, known
  devices, and ControlApp metadata

---

## Elevation and permissions

- The app runs **as invoker** (no mandatory elevation).
- **Shared configuration** — `%ProgramData%\DsHidMini\DsHidMini.json` and
  `%ProgramData%\DsHidMini\ControlApp` are writable by standard users after
  setup. The parent driver directory stays protected: users can update the
  driver file in place, and can create profile files only inside `ControlApp`.
  These files are machine-wide, so any local user who can sign in can change
  them. If the ACLs are missing or damaged, the related controls are disabled
  and **Restart as Administrator** repairs access for the next unelevated run.
- **Works without elevation** — View devices and profiles, change per-user
  preferences in `%AppData%`, use driver IPC (pairing, wireless disconnect,
  rumble, LEDs, and USB power-off), and export diagnostics.
- **Requires elevation** — Restart a USB controller or write its HID-mode
  device property, reset a USB port while switching a Retro Fighters Defender
  BT controller to PS3 mode, repair BthPS3 registry settings, and start the
  Bluetooth diagnostic ETW trace.
- **“Restart as Administrator”** — Shown in the title bar when the process
  token is not elevated.

---

## Project structure (reference)

| Area | Purpose |
|------|---------|
| **Views/Windows** | Main window (navigation, title bar, restart-as-admin). |
| **Views/Pages** | Devices, Profiles, Settings pages. |
| **Views/UserControls** | Device and profile cards, device settings editor. |
| **ViewModels/** | Page and control ViewModels (CommunityToolkit.Mvvm). |
| **Models/** | App config, DsHidMini config (DshmConfigManager, profiles, device settings), device enumeration (DshmDevMan). |
| **Services/** | Application host, snackbar/messaging. |
| **Helpers/** | Converters, bindings, localization. |

The app references **Nefarius.DsHidMini.IPC** (same repo, `SDK\Nefarius.DsHidMini.IPC`) for driver IPC.
