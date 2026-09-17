# PowerOffUsbResult

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Result of the console-style USB power-off sequence (issue #366).

```csharp
public struct PowerOffUsbResult
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [PowerOffUsbResult](./nefarius.dshidmini.ipc.models.public.poweroffusbresult.md)<br>
Attributes [IsReadOnlyAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.isreadonlyattribute)

## Properties

### <a id="properties-indicatorsoffstatus"/>**IndicatorsOffStatus**

NTSTATUS of the 48-byte zero output report that turns LEDs and rumble off.

```csharp
public uint IndicatorsOffStatus { get; set; }
```

#### Property Value

[UInt32](https://learn.microsoft.com/dotnet/api/system.uint32)<br>

### <a id="properties-shutdownstatus"/>**ShutdownStatus**

NTSTATUS of the Feature 0xF4 disable transfer that stops input reports.

```csharp
public uint ShutdownStatus { get; set; }
```

#### Property Value

[UInt32](https://learn.microsoft.com/dotnet/api/system.uint32)<br>

### <a id="properties-succeeded"/>**Succeeded**

`true` when both transfers completed successfully.

```csharp
public bool Succeeded { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

## Methods

### <a id="methods-isntsuccess"/>**IsNtSuccess(UInt32)**

Interprets a raw NTSTATUS value the same way the driver does (`NT_SUCCESS`).

```csharp
bool IsNtSuccess(uint status)
```

#### Parameters

`status` [UInt32](https://learn.microsoft.com/dotnet/api/system.uint32)<br>

#### Returns

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="methods-tostring"/>**ToString()**

```csharp
string ToString()
```

#### Returns

[String](https://learn.microsoft.com/dotnet/api/system.string)
