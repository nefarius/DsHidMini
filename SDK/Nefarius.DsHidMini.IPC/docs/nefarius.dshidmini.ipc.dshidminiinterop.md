# DsHidMiniInterop

Namespace: Nefarius.DsHidMini.IPC

Connects to the drivers shared memory region and keeps it locked to a single instance until disposed.

```csharp
public sealed class DsHidMiniInterop : System.IDisposable
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [DsHidMiniInterop](./nefarius.dshidmini.ipc.dshidminiinterop.md)<br>
Implements [IDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable)

## Properties

### <a id="properties-isavailable"/>**IsAvailable**

Gets whether driver IPC is available.

```csharp
public static bool IsAvailable { get; }
```

#### Property Value

[Boolean](https://docs.microsoft.com/en-us/dotnet/api/system.boolean)<br>

## Constructors

### <a id="constructors-.ctor"/>**DsHidMiniInterop()**

Creates a new [DsHidMiniInterop](./nefarius.dshidmini.ipc.dshidminiinterop.md) instance by connecting to the driver IPC mechanism.

```csharp
public DsHidMiniInterop()
```

#### Exceptions

[DsHidMiniInteropExclusiveAccessException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropexclusiveaccessexception.md)<br>

[DsHidMiniInteropUnavailableException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunavailableexception.md)<br>

## Methods

### <a id="methods-dispose"/>**Dispose()**

```csharp
public void Dispose()
```

### <a id="methods-getrawinputreport"/>**GetRawInputReport(Int32, Nullable&lt;TimeSpan&gt;)**

Attempts to read the [Ds3RawInputReport](./nefarius.dshidmini.ipc.models.public.ds3rawinputreport.md) from a given device instance.

```csharp
public Nullable<Ds3RawInputReport> GetRawInputReport(int deviceIndex, Nullable<TimeSpan> timeout)
```

#### Parameters

`deviceIndex` [Int32](https://docs.microsoft.com/en-us/dotnet/api/system.int32)<br>
The one-based device index.

`timeout` [Nullable&lt;TimeSpan&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.nullable-1)<br>
Optional timeout to wait for a report update to arrive. Default invocation returns immediately.

#### Returns

The [Ds3RawInputReport](./nefarius.dshidmini.ipc.models.public.ds3rawinputreport.md) or null if the given  is not occupied.

**Remarks:**

If  is null, this method returns the last known input report copy immediately. If
 you use this call in a busy loop, you should set a timeout so this call becomes event-based, meaning the call will
 only return when the driver signaled that new data is available, otherwise you will just burn through CPU for no
 good reason. A new input report is typically available each average 5 milliseconds, depending on the connection
 (wired or wireless) so a timeout of 20 milliseconds should be a good recommendation.

### <a id="methods-sendping"/>**SendPing()**

Send a PING to the driver and awaits the reply.

```csharp
public void SendPing()
```

#### Exceptions

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>

### <a id="methods-sethostaddress"/>**SetHostAddress(Int32, PhysicalAddress)**

Writes a new host address to the given device.

```csharp
public SetHostResult SetHostAddress(int deviceIndex, PhysicalAddress hostAddress)
```

#### Parameters

`deviceIndex` [Int32](https://docs.microsoft.com/en-us/dotnet/api/system.int32)<br>
The one-based device index.

`hostAddress` PhysicalAddress<br>
The new host address.

#### Returns

A [SetHostResult](./nefarius.dshidmini.ipc.models.public.sethostresult.md).

#### Exceptions

[DsHidMiniInteropInvalidDeviceIndexException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropinvaliddeviceindexexception.md)<br>

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>

**Remarks:**

This is synonymous with "pairing" to a new Bluetooth host.

### <a id="methods-setplayerindex"/>**SetPlayerIndex(Int32, Byte)**



```csharp
public uint SetPlayerIndex(int deviceIndex, byte playerIndex)
```

#### Parameters

`deviceIndex` [Int32](https://docs.microsoft.com/en-us/dotnet/api/system.int32)<br>
The one-based device index.

`playerIndex` [Byte](https://docs.microsoft.com/en-us/dotnet/api/system.byte)<br>
The player index to set to. Valid values include 1 to 7.

#### Returns

[UInt32](https://docs.microsoft.com/en-us/dotnet/api/system.uint32)

#### Exceptions

[ArgumentOutOfRangeException](https://docs.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception)<br>

[DsHidMiniInteropConcurrencyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropconcurrencyexception.md)<br>

[DsHidMiniInteropReplyTimeoutException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropreplytimeoutexception.md)<br>

[DsHidMiniInteropUnexpectedReplyException](./nefarius.dshidmini.ipc.exceptions.dshidminiinteropunexpectedreplyexception.md)<br>
