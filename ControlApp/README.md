# <img src="../assets/FireShock.png" align="left" /> DsHidMini ControlApp

![Requirements](https://img.shields.io/badge/Requires-.NET%208.0-blue.svg)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-lightgrey.svg)

**Desktop control application for the DsHidMini driver.**  
Manage connected SIXAXIS/DualShock 3 (and compatible) controllers, configure HID modes and per-device settings, and maintain DsHidMini profiles—all from a modern WPF UI (WPF-UI / Fluent).

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

DsHidMini ControlApp is the official Windows desktop companion for the [DsHidMini](https://github.com/ViGEm/DsHidMini) kernel driver. It uses the [Nefarius.DsHidMini.IPC](https://www.nuget.org/packages/Nefarius.DsHidMini.IPC/) SDK to talk to the driver and provides:

- **Devices** — View connected controllers, edit per-device settings, pair to a new Bluetooth host, and reconnect devices when the global HID mode changes.
- **Profiles** — Manage DsHidMini configuration profiles (including a global default), assign profiles to devices, and edit profile-specific settings.
- **Settings** — Application options (theme, logging, update check, genuine-controller check) and access to the global profile.

Configuration is stored under **%AppData%**; the app does not require administrator rights to run, but some device actions (e.g. full editing, pairing) are available or more reliable when run elevated.

---

## Requirements

| Requirement | Details |
|-------------|---------|
| **.NET** | .NET 8.0 |
| **OS** | Windows 10 or later (targeting `net8.0-windows10.0.17763.0`) |
| **Driver** | [DsHidMini](https://github.com/ViGEm/DsHidMini) driver installed; at least one compatible controller connected for full functionality |
| **Architecture** | AnyCPU or x64 (NUKE publish uses win-x64) |

---

## Building

From the repository root or the `ControlApp` folder:

```powershell
dotnet build
```

Release build:

```powershell
dotnet build -c Release
```

Solution build (from repo root):

```powershell
dotnet build ControlAppSolution.sln
```

---

## Publishing

Production-ready, single-file, framework-dependent publish for Windows x64 is done via the NUKE build. From the repository root:

```powershell
.\build.ps1 PublishControlApp
```

This compiles the solution (if needed) and publishes the ControlApp to the solution `bin` folder. The NUKE task uses:

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
- **Per-device settings** — Edit settings for the selected device (when running with sufficient permissions):
  - **HID mode** — SDF, GPJ, SXS, DS4Windows, XInput.
  - **LEDs** — Mode and timing (e.g. battery indicator, player index).
  - **Wireless** — Pairing mode, custom host address, idle timeout, quick-disconnect combo.
  - **Sticks** — Deadzone and polar value.
  - **Rumble** — General on/off, left-motor rescale, alternative rumble mode.
  - **Output report** — Rate control and deduplication.
- **Reconnect** — Reconnect devices when the active HID mode no longer matches the driver config (e.g. after changing the global profile).
- **Pair to host** — Set the Bluetooth host address the controller pairs to (requires elevation for handle duplication).

### Profiles page

- **Profile list** — All DsHidMini profiles plus the built-in default; select one to view or edit.
- **Global profile** — Choose which profile is used for new devices and for devices using “global settings”.
- **Per-profile editing** — Same setting categories as per-device (HID mode, LEDs, wireless, sticks, rumble, output report); changes apply when the profile is used.

### Settings page

- **Theme** — Light or dark (WPF-UI application theme).
- **App version** — Displayed in the title bar / settings.
- **Application configuration** — Stored in %AppData%; see [Configuration](#configuration).

---

## Configuration

Application-level options are stored in **%AppData%** in a JSON file (and optional schema) named **ControlApp** (see `ApplicationConfiguration.GlobalConfigFileName`). The model is in `Models\ApplicationConfiguration.cs`. Examples:

| Property | Description | Default |
|----------|-------------|---------|
| **IsLoggingEnabled** | Enable log file output | `false` |
| **IsUpdateCheckEnabled** | Check for updates on startup | `true` |
| **IsGenuineCheckEnabled** | Validate controller MAC against known OUI list | `true` |
| **HasAcknowledgedDonationDialog** | User has dismissed the donation prompt | `false` |

DsHidMini driver configuration (profiles, global profile, per-device profile assignment) is managed by **DshmConfigManager** and persisted via its own user-data store; the ControlApp UI reads and writes that through the same manager.

---

## Elevation and permissions

- The app runs **as invoker** (no mandatory elevation). Normal users can open the app, view devices and profiles, and change application settings.
- **“Restart as Administrator”** — Shown in the title bar when not elevated; use it to get full device-editing and pairing support (e.g. handle duplication for raw input and pairing).
- **Device editing** — Some device operations (e.g. pairing to host, or reliable application of certain settings) require the process to have sufficient privileges; restarting as administrator is the supported way to obtain them.

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
