# <img src="../../assets/FireShock.png" align="left" /> Nefarius.DsHidMini.IPC

![Requirements](https://img.shields.io/badge/Requires-.NET%208.0-blue.svg)
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

Use it from any .NET 8 Windows application (desktop, service, or tray tool) to:

| Capability | Description |
|------------|-------------|
| **Read raw input** | Poll or wait for `DS3_RAW_INPUT_REPORT` (buttons, sticks, pressure, motion) |
| **Pair to host** | Set the Bluetooth host address the controller pairs to (`SetHostAddress`) |
| **Player index** | Set the player LED slot (1–7) with `SetPlayerIndex` |
| **Liveness check** | Verify driver is responsive with `SendPing` |

The SDK handles reconnection when the last device disconnects and the next one arrives, and exposes device arrival/removal via internal device notification handling.

---

## Requirements

| Requirement | Details |
|-------------|---------|
| **.NET** | .NET 8.0 (Windows-specific: `net8.0-windows`) |
| **OS** | Windows (named kernel objects and shared memory are Windows-only) |
| **Driver** | DsHidMini driver installed; at least one compatible controller connected and bound to the driver |
| **Elevation** | Required only for operations that duplicate the driver’s HID report wait handle (e.g. when using a timeout in `GetRawInputReport`). Other operations do not require admin. |

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
| **`bool GetRawInputReport(int deviceIndex, ref DS3_RAW_INPUT_REPORT report, TimeSpan? timeout)`** | Fills `report` with the last or next raw HID report. Use `timeout: null` for immediate read; use e.g. `TimeSpan.FromMilliseconds(20)` for event-based waiting. Returns `false` if the slot is empty. |
| **`void SendPing()`** | Sends a ping to the driver and waits for a reply (liveness check). |
| **`SetHostResult SetHostAddress(int deviceIndex, PhysicalAddress hostAddress)`** | Writes the new Bluetooth host address (pairing). Returns write/read NTSTATUS in `SetHostResult`. |
| **`uint SetPlayerIndex(int deviceIndex, byte playerIndex)`** | Sets the player LED index (1–7). Returns NTSTATUS. |

All device-indexed APIs use a **one-based** device index (see [Device index](#device-index)).

---

## Device index

- **Valid range:** `1` … `255` (inclusive).  
- **Convention:** One-based; e.g. first device is `1`, second is `2`.  
- **Invalid index:** APIs throw `DsHidMiniInteropInvalidDeviceIndexException` if `deviceIndex` is ≤ 0 or &gt; 255.  
- The driver exposes at most one device per index; use your own discovery (e.g. device list from your app or driver docs) to map physical controllers to indices.

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
| **`DsHidMiniInteropAccessDeniedException`** | Handle duplication failed (e.g. when using `GetRawInputReport` with a timeout without sufficient privileges). Run elevated if you need event-based reading. |
| **`DsHidMiniInteropUnexpectedReplyException`** | Driver replied with an unexpected or malformed message. |

Other failures (e.g. Win32 errors during handle duplication) may surface as `Win32Exception`.

---

## Thread safety

- **Single-threaded IPC:** Only one thread may perform an IPC operation at a time. Concurrent calls (e.g. two threads calling `SetHostAddress` or `GetRawInputReport` with timeout) will cause **`DsHidMiniInteropConcurrencyException`**.  
- **Recommendation:** Serialize all calls to `DsHidMiniInterop` (e.g. one dedicated thread, or a lock around the interop instance).

---

## Documentation

Full API reference (generated from XML docs):

**[API documentation](docs/index.md)**

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
xmldoc2md .\bin\Release\net8.0-windows\Nefarius.DsHidMini.IPC.dll .\docs\
```

Build first so the DLL and XML are up to date; then run `xmldoc2md` against the DLL and output folder above.
