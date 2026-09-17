# Ds3LedEffect

Namespace: Nefarius.DsHidMini.IPC.Models.Public

One DualShock 3 LED effect block (duration and flash multipliers).
 Values match the driver's `DS_LED` / named effect macros.

```csharp
public struct Ds3LedEffect
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>
Attributes [IsReadOnlyAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.isreadonlyattribute)

## Properties

### <a id="properties-baseportionduration"/>**BasePortionDuration**

```csharp
public ushort BasePortionDuration { get; }
```

#### Property Value

[UInt16](https://learn.microsoft.com/dotnet/api/system.uint16)<br>

### <a id="properties-fastflash"/>**FastFlash**

Fast flash, used by the DS4Windows high-latency warning.

```csharp
public static Ds3LedEffect FastFlash { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

### <a id="properties-none"/>**None**

All-zero effect block for an unlit LED.

```csharp
public static Ds3LedEffect None { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

### <a id="properties-offportionmultiplier"/>**OffPortionMultiplier**

```csharp
public byte OffPortionMultiplier { get; }
```

#### Property Value

[Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

### <a id="properties-onportionmultiplier"/>**OnPortionMultiplier**

```csharp
public byte OnPortionMultiplier { get; }
```

#### Property Value

[Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

### <a id="properties-slowflash"/>**SlowFlash**

Slow flash, used for Low/Dying battery levels.

```csharp
public static Ds3LedEffect SlowFlash { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

### <a id="properties-static"/>**Static**

PS3-correct static effect: lasts forever, no flashing.

```csharp
public static Ds3LedEffect Static { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

### <a id="properties-totalduration"/>**TotalDuration**

```csharp
public byte TotalDuration { get; }
```

#### Property Value

[Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

## Constructors

### <a id="constructors-.ctor"/>**Ds3LedEffect(Byte, UInt16, Byte, Byte)**

```csharp
Ds3LedEffect(byte totalDuration, ushort basePortionDuration, byte offPortionMultiplier, byte onPortionMultiplier)
```

#### Parameters

`totalDuration` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

`basePortionDuration` [UInt16](https://learn.microsoft.com/dotnet/api/system.uint16)<br>

`offPortionMultiplier` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

`onPortionMultiplier` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>
