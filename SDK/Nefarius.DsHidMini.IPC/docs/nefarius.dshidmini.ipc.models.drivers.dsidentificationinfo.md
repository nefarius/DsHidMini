# DsIdentificationInfo

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

Decoded DualShock 3 / SIXAXIS Feature 0x01 identification report.

```csharp
public sealed class DsIdentificationInfo
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [DsIdentificationInfo](./nefarius.dshidmini.ipc.models.drivers.dsidentificationinfo.md)<br>
Attributes [NullableContextAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.nullablecontextattribute), [NullableAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.nullableattribute)

## Properties

### <a id="properties-calibrationfields"/>**CalibrationFields**

Calibration field IDs from offset `0x26`.

```csharp
public Byte[] CalibrationFields { get; }
```

#### Property Value

[Byte[]](https://learn.microsoft.com/dotnet/api/system.byte[])<br>

### <a id="properties-cloneheuristic"/>**CloneHeuristic**

`true` if the field list is exactly `01 02` and byte
 `0x29` is `0x64`. Heuristic, not a verdict.

```csharp
public bool CloneHeuristic { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

### <a id="properties-firmware"/>**Firmware**

Firmware/board revision packed as `b2<<16 | b3<<8 | b4`.

```csharp
public uint Firmware { get; }
```

#### Property Value

[UInt32](https://learn.microsoft.com/dotnet/api/system.uint32)<br>

### <a id="properties-firmwaredisplay"/>**FirmwareDisplay**

Firmware bytes formatted as space-separated hex, e.g. `04 00 08`.

```csharp
public string FirmwareDisplay { get; }
```

#### Property Value

[String](https://learn.microsoft.com/dotnet/api/system.string)<br>

### <a id="properties-motionpath"/>**MotionPath**

Motion path derived from the calibration field list. Field `0x07` wins
 over `PLAIN_ZERO`.

```csharp
public DsIdentificationMotionPath MotionPath { get; }
```

#### Property Value

[DsIdentificationMotionPath](./nefarius.dshidmini.ipc.models.drivers.dsidentificationmotionpath.md)<br>

### <a id="properties-padtype"/>**PadType**

First of the four pad/sensor type bytes at offsets 8-11. Informational only.

```csharp
public byte PadType { get; }
```

#### Property Value

[Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

## Constructors

### <a id="constructors-.ctor"/>**DsIdentificationInfo(UInt32, Byte, DsIdentificationMotionPath, Boolean, Byte[])**

```csharp
public DsIdentificationInfo(uint firmware, byte padType, DsIdentificationMotionPath motionPath, bool cloneHeuristic, Byte[] calibrationFields)
```

#### Parameters

`firmware` [UInt32](https://learn.microsoft.com/dotnet/api/system.uint32)<br>

`padType` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

`motionPath` [DsIdentificationMotionPath](./nefarius.dshidmini.ipc.models.drivers.dsidentificationmotionpath.md)<br>

`cloneHeuristic` [Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

`calibrationFields` [Byte[]](https://learn.microsoft.com/dotnet/api/system.byte[])<br>
