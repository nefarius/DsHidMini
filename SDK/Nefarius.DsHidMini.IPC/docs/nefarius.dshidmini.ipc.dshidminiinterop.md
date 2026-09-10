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

### <a id="properties-isavailable"/>**IsAvailable**

Gets whether driver IPC is available.

```csharp
public static bool IsAvailable { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

### <a id="properties-hasmotiontelemetry"/>**HasMotionTelemetry**

`true` when this client mapped the driver’s motion telemetry region. Older drivers leave this `false`.

```csharp
public bool HasMotionTelemetry { get; }
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
No driver instance is available. Make sure that at least one
 device is connected and that the driver is installed and working properly. Call [DsHidMiniInterop.IsAvailable](./nefarius.dshidmini.ipc.dshidminiinterop.md#isavailable) prior to
 avoid this exception.

## Methods

### <a id="methods-dispose"/>**Dispose()**

```csharp
public void Dispose()
```

### <a id="methods-getrawinputreport"/>**GetRawInputReport(Int32, ref DS3_RAW_INPUT_REPORT, Nullable&lt;TimeSpan&gt;)**

Attempts to read the [DS3_RAW_INPUT_REPORT](./nefarius.dshidmini.ipc.models.public.ds3_raw_input_report.md) from a given device instance.

```csharp
public bool GetRawInputReport(int deviceIndex, ref DS3_RAW_INPUT_REPORT report, Nullable<TimeSpan> timeout)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`report` [DS3_RAW_INPUT_REPORT](./nefarius.dshidmini.ipc.models.public.ds3_raw_input_report.md)<br>
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
No driver instance is available. Make sure that at least one
 device is connected and that the driver is installed and working properly. Call [DsHidMiniInterop.IsAvailable](./nefarius.dshidmini.ipc.dshidminiinterop.md#isavailable) prior to
 avoid this exception.

**Remarks:**

If `timeout` is null, this method returns the last known input report copy immediately. If
 you use this call in a busy loop, you should set a timeout so this call becomes event-based, meaning the call will
 only return when the driver signaled that new data is available, otherwise you will just burn through CPU for no
 good reason. A new input report is typically available each average 5 milliseconds, depending on the connection
 (wired or wireless) so a timeout of 20 milliseconds should be a good recommendation.
 When `timeout` is set, the implementation waits on the driver's per-slot named manual-reset event
 (same DACL as other IPC objects); it does not require administrator elevation. Multiple clients can wait on the
 same slot without splitting wakeups.

### <a id="methods-getmotionsnapshot"/>**GetMotionSnapshot(Int32, out DsMotionSnapshot, Nullable&lt;TimeSpan&gt;)**

Attempts to read the current [DsMotionSnapshot](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshot.md) for a device slot.

```csharp
public bool GetMotionSnapshot(int deviceIndex, out DsMotionSnapshot snapshot, Nullable<TimeSpan> timeout)
```

#### Parameters

`deviceIndex` [Int32](https://learn.microsoft.com/dotnet/api/system.int32)<br>
The one-based device index.

`snapshot` [DsMotionSnapshot](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshot.md)<br>
Receives a stable seqlock copy when the method returns true.

`timeout` [Nullable](https://learn.microsoft.com/dotnet/api/system.nullable-1)<[TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)><br>
Optional timeout to wait for a snapshot update. Default invocation returns immediately.

#### Returns

TRUE if `snapshot` was filled, FALSE if motion telemetry is unavailable, the slot is empty, or a timeout expires.

**Remarks:**

Uses the same per-slot HID wait event as [GetRawInputReport](./nefarius.dshidmini.ipc.dshidminiinterop.md#methods-getrawinputreport). When the connected driver has no motion region, this returns `false`. Check [HasMotionTelemetry](./nefarius.dshidmini.ipc.dshidminiinterop.md#properties-hasmotiontelemetry).

### <a id="methods-reconnect"/>**Reconnect()**

Attempt re-initialization of IPC after all devices got disconnected.

```csharp
public void Reconnect()
```

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
No driver instance is available. Make sure that at least one
 device is connected and that the driver is installed and working properly. Call [DsHidMiniInterop.IsAvailable](./nefarius.dshidmini.ipc.dshidminiinterop.md#isavailable) prior to
 avoid this exception.

### <a id="methods-sendping"/>**SendPing()**

Send a PING to the driver and awaits the reply.

```csharp
public void SendPing()
```

#### Exceptions

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>
Driver IPC unavailable, make sure that at least one compatible
 controller is connected and operational.

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
Driver IPC unavailable, make sure that at least one compatible
 controller is connected and operational.

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

### <a id="methods-setplayerindex"/>**SetPlayerIndex(Int32, Byte)**

Overwrites the player slot indicator (player LEDs) of the given device.

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
Driver IPC unavailable, make sure that at least one compatible
 controller is connected and operational.

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
