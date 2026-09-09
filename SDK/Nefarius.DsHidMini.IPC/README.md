# <img src="https://raw.githubusercontent.com/nefarius/DsHidMini/master/assets/FireShock.png" align="left" /> Nefarius.DsHidMini.IPC

![Requirements](https://img.shields.io/badge/Requires-.NET%20Standard%202.0%2B-blue.svg)
[![NuGet Version](https://img.shields.io/nuget/v/Nefarius.DsHidMini.IPC)](https://www.nuget.org/packages/Nefarius.DsHidMini.IPC/)
[![NuGet](https://img.shields.io/nuget/dt/Nefarius.DsHidMini.IPC)](https://www.nuget.org/packages/Nefarius.DsHidMini.IPC/)

**Inter-process communication (IPC) SDK for the DsHidMini driver.**  
Connect your .NET application to DsHidMini via shared memory and named synchronization objects to read controller input, change host pairing, set player LEDs, and more.

---

## Table of contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [API overview](#api-overview)
- [Device index](#device-index)
- [Key types](#key-types)
- [Error handling](#error-handling)
- [Thread safety](#thread-safety)
- [Documentation](#documentation)
- [Regenerating API docs](#regenerating-api-docs)

---

## Overview

DsHidMini is a Windows kernel-mode driver that enables SIXAXIS/DualShock 3 (and compatible) controllers to work as HID or XInput devices. This library is the **client-side SDK** that talks to the driver over:

- **Shared memory** — command channel and HID input report data
- **Named events and mutex** — request/response synchronization

Use it from a .NET Standard 2.0 consumer or a .NET 10 Windows application (desktop, service, or tray tool) to:

| Capability | Description |
|------------|-------------|
| **Read raw input** | Poll or wait for `DS3_RAW_INPUT_REPORT` (buttons, sticks, pressure, motion) |
| **Pair to host** | Set the Bluetooth host address the controller pairs to (`SetHostAddress`) or pair to the active local radio (`PairToCurrentHost`) |
| **Player index** | Set the player LED slot (1–7) with `SetPlayerIndex` (volatile) |
| **LED pattern** | Apply flags plus four independent effect blocks with `SetLedPattern` (volatile) |
| **Rumble** | Send motor strengths with `SetRumble` (volatile; uses driver rescale / keep-alive) |
| **Alternate rumble** | Toggle alternative rumble mode with `SetAlternateRumbleMode` (volatile; not written to JSON) |
| **Bluetooth disconnect** | Disconnect a wireless device with `DisconnectBluetoothDevice` |
| **USB power-off** | Send the console USB power-off sequence with `PowerOffUsbDevice` |
| **Liveness check** | Verify driver is responsive with `SendPing` |

The SDK handles reconnection when the last device disconnects and the next one arrives, and exposes device arrival/removal via internal device notification handling.

---

## Requirements

| Requirement | Details |
|-------------|---------|
| **.NET** | .NET Standard 2.0 or newer (covers .NET Framework 4.6.2+ and all modern .NET). An explicit `net10.0-windows` build is also shipped. Windows-only at runtime. |
| **OS** | Windows (named kernel objects and shared memory are Windows-only) |
| **Driver** | DsHidMini driver installed; at least one compatible controller connected and bound to the driver |
| **Elevation** | Not required for normal use. IPC uses named objects (mutex, events, shared memory, and per-device HID wait events) with DACLs that allow authenticated users, including event-based `GetRawInputReport`. |

Before creating a `DsHidMiniInterop` instance, check **`DsHidMiniInterop.IsAvailable`** to avoid throwing when no device is present.

---

## Installation

Install the package from [NuGet](https://www.nuget.org/packages/Nefarius.DsHidMini.IPC/):

```bash
dotnet add package Nefarius.DsHidMini.IPC
```

Or with Package Manager:

```powershell
Install-Package Nefarius.DsHidMini.IPC
```

---

## Quick start

```csharp
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

// 1. Check that the driver is present (e.g. at least one controller is connected)
if (!DsHidMiniInterop.IsAvailable)
{
    Console.WriteLine("No DsHidMini device found. Connect a controller.");
    return;
}

// 2. Create the interop client (connects to shared memory and events)
using var interop = new DsHidMiniInterop();

// 3. Optional: ping the driver
interop.SendPing();

// 4. Read raw input from device index 1 (first device)
var report = default(DS3_RAW_INPUT_REPORT);
bool gotReport = interop.GetRawInputReport(1, ref report, timeout: null);

if (gotReport)
{
    // Use report.Buttons (Select, Start, L1, R1, Triangle, etc.)
    // report.LeftThumbX/Y, report.RightThumbX/Y
    // report.Pressure, report.BatteryStatus, report.AccelerometerX/Y/Z, etc.
}
```

---

## API overview

### Main type: `DsHidMiniInterop`

| Member | Description |
|--------|-------------|
| **`static bool IsAvailable`** | `true` if the driver’s shared memory is present (at least one device active). Check this before constructing. |
| **`DsHidMiniInterop()`** | Connects to the driver IPC. Throws if not available. Subscribes to device arrival/removal for reconnection. |
| **`void Dispose()`** | Releases mapped views, file mapping, and events. Implement `IDisposable` and dispose when done. |
| **`void Reconnect()`** | Re-opens mutex, events, and shared memory (e.g. after all devices were removed). Throws if still no device. |
| **`bool GetRawInputReport(int deviceIndex, ref DS3_RAW_INPUT_REPORT report, TimeSpan? timeout)`** | Fills `report` with the last or next raw HID report. Use `timeout: null` for immediate read; use e.g. `TimeSpan.FromMilliseconds(20)` for event-based waiting on the driver’s named per-slot manual-reset event (`Global\DsHidMiniHidReportEvent` + index). Multiple clients can wait on the same slot. Returns `false` if the slot is empty, or if a timeout was requested and no wait object exists for that slot (nothing connected there). |
| **`void SendPing()`** | Sends a ping to the driver and waits for a reply (liveness check). |
| **`SetHostResult SetHostAddress(int deviceIndex, PhysicalAddress hostAddress)`** | Writes the new Bluetooth host address (pairing). Does not persist pairing mode. Returns write/read NTSTATUS in `SetHostResult`. |
| **`SetHostResult PairToCurrentHost(int deviceIndex)`** | Pairs the device to the active local Bluetooth radio. Wired devices only; does not persist pairing mode. |
| **`uint DisconnectBluetoothDevice(int deviceIndex)`** | Disconnects a currently wireless device. Returns NTSTATUS (`STATUS_NOT_SUPPORTED` when the device is wired). |
| **`uint SetPlayerIndex(int deviceIndex, byte playerIndex)`** | Sets the player LED index (1–7). Volatile: Automatic LED authority is handed to the application so driver battery refreshes do not overwrite the slot. Returns NTSTATUS (`STATUS_ACCESS_DENIED` when LED authority is Driver). |
| **`uint SetLedPattern(int deviceIndex, Ds3LedPattern pattern)`** | Applies a full LED pattern (flags + four effect blocks). Volatile; same authority rules as `SetPlayerIndex`. Reserved flag bits throw before the driver is called. |
| **`uint SetRumble(int deviceIndex, byte largeMotor, byte smallMotor)`** | Sets heavy/left and light/right motor strengths (0–255). Volatile; processed through rescale, alternative mode, keep-alive, and Navigation suppression. Returns NTSTATUS. |
| **`uint SetAlternateRumbleMode(int deviceIndex, bool enabled)`** | Enables or disables alternative rumble mode for the current session only. A config reload restores the JSON value. Returns NTSTATUS. |
| **`PowerOffUsbResult PowerOffUsbDevice(int deviceIndex)`** | Sends the PlayStation 3 USB power-off sequence (zero output report, then Feature 0xF4 disable). Wired devices only; the controller stays enumerated. |

All device-indexed APIs use a **one-based** device index (see [Device index](#device-index)).

---

## Device index

- **Valid range:** `1` … `255` (inclusive).  
- **Meaning:** The index is the driver’s **IPC slot** (`SlotIndex`): shared HID memory, per-slot wait events (`Global\DsHidMiniHidReportEvent` + index), and IPC `TargetIndex` all use this same one-based value.  
- **Discovery:** Read the read-only device property **`DsHidMiniDriver.IpcSlotIndexProperty`** (`DEVPROP_TYPE_UINT32`, same value the driver publishes after claiming a slot). Enumerate DsHidMini device interfaces and query this property per `PnPDevice`—do **not** assume SetupAPI / `CM_Get_Device_Interface_List` ordering matches slot order (e.g. after a middle device disconnects, remaining devices may occupy non-contiguous slots such as `1` and `3`).  
- **Older drivers:** If the property is absent, fall back to your own mapping; ordering-only heuristics may be wrong when slots are not contiguous.  
- **Invalid index:** APIs throw `DsHidMiniInteropInvalidDeviceIndexException` if `deviceIndex` is ≤ 0 or &gt; 255.

---

## Key types

### `DS3_RAW_INPUT_REPORT` (raw HID report)

- **Buttons:** `report.Buttons` — e.g. `Select`, `Start`, `L1`, `R1`, `L2`, `R2`, `L3`, `R3`, `Up`, `Down`, `Left`, `Right`, `Triangle`, `Circle`, `Cross`, `Square`, `PS`.  
- **Sticks:** `LeftThumbX/Y`, `RightThumbX/Y` (0x00 = min, 0x80 = center, 0xFF = max).  
- **Pressure:** `report.Pressure.Values` — per-button pressure (Up, Down, Left, Right, L1, R1, L2, R2, Triangle, Circle, Cross, Square).  
- **Battery:** `BatteryStatus` (see `DsBatteryStatus` in API docs).  
- **Motion:** `AccelerometerX/Y/Z`, `Gyroscope`.

### `SetHostResult` (pairing result)

- **`WriteStatus`** — NTSTATUS of the “pair to host” write.  
- **`ReadStatus`** — NTSTATUS of the subsequent read-back of the address.
- **`Succeeded`** — `true` when both the write and the verify read completed successfully.

### `Ds3PlayerLeds` (player LED mapping)

- **`TryGetFlags(byte playerIndex, out byte flags)`** — maps 1–7 to the DS3 LED mask (`0x02`, `0x04`, `0x08`, `0x10`, plus 5=`1+4`, 6=`2+4`, 7=`3+4`).
- **`LedOff`** — explicit all-off marker (`0x20`).

### `Ds3LedPattern` / `Ds3LedEffect` (direct LED output)

- **`Ds3LedEffect`** — duration and flash multipliers, with named `Static`, `SlowFlash`, `FastFlash`, and `None` presets.
- **`Ds3LedPattern`** — flags plus four independent per-LED effect blocks.
- **`AreFlagsValid(byte flags)`** — accepts only documented DS3 LED bits (1–4 and off). Zero is valid.
- **`TryFromPlayerIndex(byte playerIndex, out Ds3LedPattern pattern)`** — builds a static player-index pattern.

### `PowerOffUsbResult` (USB power-off)

- **`IndicatorsOffStatus`** — NTSTATUS of the 48-byte zero output report.  
- **`ShutdownStatus`** — NTSTATUS of the Feature 0xF4 disable transfer.  
- **`Succeeded`** — `true` when both transfers completed successfully.

### Driver / model enums (see API docs)

- **`DsBatteryStatus`** — Unknown, Dying, Low, Medium, High, Full, Charging, Charged.  
- **`DsHidDeviceMode`** — SDF, GPJ, SXS, DS4W, XInput (driver mode; useful when integrating with device properties).  
- **`DsHidMiniDriver`** — Static class with `DeviceInterfaceGuid` and device property keys (e.g. battery, host address, mode) for use with plug-and-play APIs.

---

## Error handling

The SDK uses dedicated exception types so you can handle driver and usage errors explicitly:

| Exception | When it is thrown |
|-----------|-------------------|
| **`DsHidMiniInteropUnavailableException`** | No driver instance (no device connected or driver not loaded). Check `IsAvailable` before constructing or calling APIs. |
| **`DsHidMiniInteropInvalidDeviceIndexException`** | `deviceIndex` not in 1…255. |
| **`DsHidMiniInteropReplyTimeoutException`** | Driver did not respond within the expected time (e.g. ping or command). |
| **`DsHidMiniInteropConcurrencyException`** | Another thread is already performing an IPC call; only one at a time is allowed. |
| **`DsHidMiniInteropUnexpectedReplyException`** | Driver replied with an unexpected or malformed message. |

Other failures may surface as `Win32Exception` where low-level Win32 calls apply.

---

## Thread safety

- **Single-threaded IPC:** Only one thread may perform an IPC operation at a time. Concurrent calls (e.g. two threads calling `SetHostAddress` or `GetRawInputReport` with timeout) will cause **`DsHidMiniInteropConcurrencyException`**.  
- **Recommendation:** Serialize all calls to `DsHidMiniInterop` (e.g. one dedicated thread, or a lock around the interop instance).

---

## Documentation

Full API reference (generated from XML docs):

**[API documentation](https://github.com/nefarius/DsHidMini/blob/master/SDK/Nefarius.DsHidMini.IPC/docs/index.md)**

Includes:

- `DsHidMiniInterop` — constructors, properties, methods, exceptions  
- Exceptions — all `DsHidMiniInterop*Exception` types  
- Models — `DS3_RAW_INPUT_REPORT`, `SetHostResult`, driver enums and property keys  
- Utilities — `EnumDescriptionTypeConverter`

---

## Regenerating API docs

To regenerate the `docs/` markdown from the built assembly:

```bash
dotnet build -c Release
dotnet tool install -g Nefarius.Tools.XMLDoc2Markdown
xmldoc2md .\bin\Release\net10.0-windows\Nefarius.DsHidMini.IPC.dll .\docs\
```

Build first so the DLL and XML are up to date; then run `xmldoc2md` against the DLL and output folder above.
