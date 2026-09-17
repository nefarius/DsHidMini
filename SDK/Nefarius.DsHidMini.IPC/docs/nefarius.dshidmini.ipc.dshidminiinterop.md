# DsHidMiniInterop

Namespace: Nefarius.DsHidMini.IPC

Connects to the drivers shared memory region and offers utility methods for data exchange.

```csharp
public sealed class DsHidMiniInterop : System.IDisposable
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [DsHidMiniInterop](./nefarius.dshidmini.ipc.dshidminiinterop.md)<br>
Implements [IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)<br>
Attributes [NullableContextAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.nullablecontextattribute), [NullableAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.nullableattribute)

## Properties

### <a id="properties-hasmotiontelemetry"/>**HasMotionTelemetry**

`true` when this client mapped the driver's motion
 telemetry region. Older drivers leave this `false`.

```csharp
public bool HasMotionTelemetry { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

### <a id="properties-isavailable"/>**IsAvailable**

Gets whether the required command mutex, read/write events, and shared-memory mapping can all be opened.

```csharp
public static bool IsAvailable { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

## Constructors

### <a id="constructors-.ctor"/>**DsHidMiniInterop()**

Creates a new [DsHidMiniInterop](./nefarius.dshidmini.ipc.dshidminiinterop.md) instance by connecting to the driver IPC mechanism.

```csharp
public DsHidMiniInterop()
```

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable. Make sure the driver is loaded with IPC enabled. Call
 [DsHidMiniInterop.IsAvailable](./nefarius.dshidmini.ipc.dshidminiinterop.md#isavailable) first to avoid this exception.

## Methods

### <a id="methods-disconnectbluetoothdevice"/>**DisconnectBluetoothDevice(Int32)**

Disconnects a currently wireless device from the host radio.
 Wired devices return `STATUS_NOT_SUPPORTED`.

```csharp
public uint DisconnectBluetoothDevice(int deviceIndex)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

#### Returns

The NTSTATUS returned by the driver.

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside the valid range 1..255.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-dispose"/>**Dispose()**

```csharp
public void Dispose()
```

### <a id="methods-getmotionsnapshot"/>**GetMotionSnapshot(Int32, ref DsMotionSnapshot, Nullable&lt;TimeSpan&gt;)**

Attempts to read the current [DsMotionSnapshot](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshot.md) for a device slot.

```csharp
public bool GetMotionSnapshot(int deviceIndex, ref DsMotionSnapshot snapshot, Nullable<TimeSpan> timeout)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`snapshot` [DsMotionSnapshot&](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshot&.md)<br>
Receives a stable seqlock copy when the method returns true.

`timeout` [Nullable](https://learn.microsoft.com/dotnet/api/system.nullable-1)<[TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)><br>
Optional timeout to wait for a snapshot update. Default invocation returns immediately.

#### Returns

TRUE if `snapshot` was filled, FALSE if motion telemetry
 is unavailable, the slot is empty, or a timeout expired.

**Remarks:**

Uses the same per-slot HID wait event as [DsHidMiniInterop.GetRawInputReport(Int32, ref DS3_RAW_INPUT_REPORT, Nullable&lt;TimeSpan&gt;)](./nefarius.dshidmini.ipc.dshidminiinterop.md#getrawinputreportint32-ref-ds3_raw_input_report-nullabletimespan).
 When the connected driver has no motion region (older builds), this
 returns `false`. Check [DsHidMiniInterop.HasMotionTelemetry](./nefarius.dshidmini.ipc.dshidminiinterop.md#hasmotiontelemetry).

### <a id="methods-getrawinputreport"/>**GetRawInputReport(Int32, ref DS3_RAW_INPUT_REPORT, Nullable&lt;TimeSpan&gt;)**

Attempts to read the [DS3_RAW_INPUT_REPORT](./nefarius.dshidmini.ipc.models.public.ds3_raw_input_report.md) from a given device instance.

```csharp
public bool GetRawInputReport(int deviceIndex, ref DS3_RAW_INPUT_REPORT report, Nullable<TimeSpan> timeout)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`report` [DS3_RAW_INPUT_REPORT&](./nefarius.dshidmini.ipc.models.public.ds3_raw_input_report&.md)<br>
The [DS3_RAW_INPUT_REPORT](./nefarius.dshidmini.ipc.models.public.ds3_raw_input_report.md) to populate.

`timeout` [Nullable](https://learn.microsoft.com/dotnet/api/system.nullable-1)<[TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)><br>
Optional timeout to wait for a report update to arrive. Default invocation returns immediately.

#### Returns

TRUE if `report` got filled in or FALSE if the given `deviceIndex` is not
 occupied, if `timeout` is used and the named wait event for that slot does not exist (no device
 in that slot), or if `timeout` expires before a new report generation arrives.

#### Exceptions

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable. Check [DsHidMiniInterop.IsAvailable](./nefarius.dshidmini.ipc.dshidminiinterop.md#isavailable) before calling.

**Remarks:**

If `timeout` is null, this method returns the last known input report copy immediately. If
 you use this call in a busy loop, you should set a timeout so this call becomes event-based, meaning the call will
 only return when the driver signaled that new data is available, otherwise you will just burn through CPU for no
 good reason. A new input report is typically available each average 5 milliseconds, depending on the connection
 (wired or wireless) so a timeout of 20 milliseconds should be a good recommendation.
 When `timeout` is set, the implementation waits on the driver's per-slot named manual-reset event
 (same DACL as other IPC objects); it does not require administrator elevation. Multiple clients can wait on the
 same slot without splitting wakeups.

### <a id="methods-pairtocurrenthost"/>**PairToCurrentHost(Int32)**

Pairs the given device to the active local Bluetooth radio.
 Does not persist pairing mode or overwrite the JSON host address.
 Wired devices only.

```csharp
public SetHostResult PairToCurrentHost(int deviceIndex)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

#### Returns

A [SetHostResult](./nefarius.dshidmini.ipc.models.public.sethostresult.md) containing write/read NTSTATUS values.

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside the valid range 1..255.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-poweroffusbdevice"/>**PowerOffUsbDevice(Int32)**

Sends the PlayStation 3 USB power-off sequence to a wired device: a 48-byte zero
 output report, then Feature 0xF4 disable. The controller stays enumerated.

```csharp
public PowerOffUsbResult PowerOffUsbDevice(int deviceIndex)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

#### Returns

[PowerOffUsbResult](./nefarius.dshidmini.ipc.models.public.poweroffusbresult.md)

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside the valid range 1..255.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-reconnect"/>**Reconnect()**

Attempt re-initialization of IPC after all devices got disconnected.

```csharp
public void Reconnect()
```

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
The command mutex, read/write events, or shared-memory mapping are unavailable. Make sure the driver is loaded
 with IPC enabled.

### <a id="methods-sendping"/>**SendPing()**

Send a PING to the driver and awaits the reply.

```csharp
public void SendPing()
```

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-setalternaterumblemode"/>**SetAlternateRumbleMode(Int32, Boolean)**

Enables or disables alternative rumble mode for the current session only.
 The persisted JSON configuration is not updated; a driver config reload
 restores the file value.

```csharp
public uint SetAlternateRumbleMode(int deviceIndex, bool enabled)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`enabled` [Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>
`true` to enable alternative rumble mode.

#### Returns

The NTSTATUS returned by the driver.

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside the valid range 1..255.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-sethostaddress"/>**SetHostAddress(Int32, PhysicalAddress)**

Writes a new host address to the given device.

```csharp
public SetHostResult SetHostAddress(int deviceIndex, PhysicalAddress hostAddress)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`hostAddress` [PhysicalAddress](https://learn.microsoft.com/dotnet/api/system.net.networkinformation.physicaladdress)<br>
The new host address.

#### Returns

A [SetHostResult](./nefarius.dshidmini.ipc.models.public.sethostresult.md) containing success (or error) details.

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside a valid
 range.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

**Remarks:**

This is synonymous with "pairing" to a new Bluetooth host.

### <a id="methods-setledpattern"/>**SetLedPattern(Int32, Ds3LedPattern)**

Applies a full LED pattern (flags plus four independent effect blocks).
 The change is volatile: Automatic LED authority is handed to the application
 for the rest of the session. Returns `STATUS_ACCESS_DENIED` when LED
 authority is configured as Driver, and `STATUS_INVALID_PARAMETER` for
 reserved flag bits.

```csharp
public uint SetLedPattern(int deviceIndex, Ds3LedPattern pattern)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`pattern` [Ds3LedPattern](./nefarius.dshidmini.ipc.models.public.ds3ledpattern.md)<br>
The flags and per-LED effects to apply.

#### Returns

The NTSTATUS returned by the driver.

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside the valid range 1..255.

[ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)<br>
`pattern` uses reserved LED flag bits.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-setplayerindex"/>**SetPlayerIndex(Int32, Byte)**

Overwrites the player slot indicator (player LEDs) of the given device.
 The change is volatile: Automatic LED authority is handed to the application
 for the rest of the session so driver battery refreshes do not overwrite it.
 Returns `STATUS_ACCESS_DENIED` when LED authority is configured as Driver.

```csharp
public uint SetPlayerIndex(int deviceIndex, byte playerIndex)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`playerIndex` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>
The player index to set to. Valid values include 1 to 7.

#### Returns

[UInt32](https://learn.microsoft.com/dotnet/api/system.uint32)

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside the valid range 1..255.

[ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)<br>
The `playerIndex` was outside the valid range 1..7.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-setrumble"/>**SetRumble(Int32, Byte, Byte)**

Sets rumble motor strengths on a device. Values are processed through the driver's
 existing rescale, alternative-mode, keep-alive, and Navigation-controller rules.
 The change is volatile and is not written to the JSON configuration.

```csharp
public uint SetRumble(int deviceIndex, byte largeMotor, byte smallMotor)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`largeMotor` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>
Heavy / left motor strength (0-255).

`smallMotor` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>
Light / right motor strength (0-255).

#### Returns

The NTSTATUS returned by the driver.

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
One or more required driver IPC objects are unavailable.

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>
The `deviceIndex` was outside the valid range 1..255.

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>
A different thread is currently performing a data exchange.

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>
The driver didn't respond within an expected period.

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
The driver returned unexpected or malformed data.

### <a id="methods-trygetipcslotindex"/>**TryGetIpcSlotIndex(PnPDevice)**

Reads the driver's one-based IPC slot for a device when available (see [DsHidMiniDriver.IpcSlotIndexProperty](./nefarius.dshidmini.ipc.models.drivers.dshidminidriver.md#ipcslotindexproperty)).

```csharp
public static Nullable<Int32> TryGetIpcSlotIndex(PnPDevice device)
```

#### Parameters

`device` PnPDevice<br>

#### Returns

The slot index, or `null` if the property is missing (older driver) or invalid.
