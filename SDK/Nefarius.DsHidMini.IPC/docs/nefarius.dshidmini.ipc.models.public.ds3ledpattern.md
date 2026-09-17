# Ds3LedPattern

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Full DualShock 3 LED pattern: a flags byte plus four independent
 per-LED effect blocks. Used by [DsHidMiniInterop.SetLedPattern(Int32, Ds3LedPattern)](./nefarius.dshidmini.ipc.dshidminiinterop.md#setledpatternint32-ds3ledpattern).

```csharp
public struct Ds3LedPattern
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [Ds3LedPattern](./nefarius.dshidmini.ipc.models.public.ds3ledpattern.md)<br>
Attributes [IsReadOnlyAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.isreadonlyattribute)

## Fields

### <a id="fields-validflagsmask"/>**ValidFlagsMask**

Documented DS3 LED bits: physical LEDs 1-4 and the explicit off marker.

```csharp
public static byte ValidFlagsMask;
```

## Properties

### <a id="properties-flags"/>**Flags**

```csharp
public byte Flags { get; }
```

#### Property Value

[Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

### <a id="properties-player1"/>**Player1**

```csharp
public Ds3LedEffect Player1 { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

### <a id="properties-player2"/>**Player2**

```csharp
public Ds3LedEffect Player2 { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

### <a id="properties-player3"/>**Player3**

```csharp
public Ds3LedEffect Player3 { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

### <a id="properties-player4"/>**Player4**

```csharp
public Ds3LedEffect Player4 { get; }
```

#### Property Value

[Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

## Constructors

### <a id="constructors-.ctor"/>**Ds3LedPattern(Byte, Ds3LedEffect, Ds3LedEffect, Ds3LedEffect, Ds3LedEffect)**

```csharp
Ds3LedPattern(byte flags, Ds3LedEffect player1, Ds3LedEffect player2, Ds3LedEffect player3, Ds3LedEffect player4)
```

#### Parameters

`flags` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

`player1` [Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

`player2` [Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

`player3` [Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

`player4` [Ds3LedEffect](./nefarius.dshidmini.ipc.models.public.ds3ledeffect.md)<br>

## Methods

### <a id="methods-areflagsvalid"/>**AreFlagsValid(Byte)**

Returns `true` when `flags` uses only
 documented DS3 LED bits. Zero (all off) is valid.

```csharp
bool AreFlagsValid(byte flags)
```

#### Parameters

`flags` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

#### Returns

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="methods-tryfromplayerindex"/>**TryFromPlayerIndex(Byte, ref Ds3LedPattern)**

Static player-index pattern for slots 1-7 (same mapping as
 [DsHidMiniInterop.SetPlayerIndex(Int32, Byte)](./nefarius.dshidmini.ipc.dshidminiinterop.md#setplayerindexint32-byte)).

```csharp
bool TryFromPlayerIndex(byte playerIndex, ref Ds3LedPattern pattern)
```

#### Parameters

`playerIndex` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

`pattern` [Ds3LedPattern&](./nefarius.dshidmini.ipc.models.public.ds3ledpattern&.md)<br>

#### Returns

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)
