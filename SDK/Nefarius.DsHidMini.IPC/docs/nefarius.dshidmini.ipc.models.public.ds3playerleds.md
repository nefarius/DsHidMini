# Ds3PlayerLeds

Namespace: Nefarius.DsHidMini.IPC.Models.Public

DualShock 3 player-index to LED flags mapping used by
 [DsHidMiniInterop.SetPlayerIndex(Int32, Byte)](./nefarius.dshidmini.ipc.dshidminiinterop.md#setplayerindexint32-byte) (issue #379).

```csharp
public static class Ds3PlayerLeds
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [Ds3PlayerLeds](./nefarius.dshidmini.ipc.models.public.ds3playerleds.md)

## Fields

### <a id="fields-led1"/>**Led1**

Physical LED 1 bit.

```csharp
public static byte Led1;
```

### <a id="fields-led2"/>**Led2**

Physical LED 2 bit.

```csharp
public static byte Led2;
```

### <a id="fields-led3"/>**Led3**

Physical LED 3 bit.

```csharp
public static byte Led3;
```

### <a id="fields-led4"/>**Led4**

Physical LED 4 bit.

```csharp
public static byte Led4;
```

### <a id="fields-ledoff"/>**LedOff**

Explicit all-off marker used by the hardware and custom patterns.

```csharp
public static byte LedOff;
```

## Methods

### <a id="methods-trygetflags"/>**TryGetFlags(Byte, ref Byte)**

Maps a player index (1-7) to the DS3 LED flags byte. Indices 5-7 use the
 extra combinations the hardware uses once four physical LEDs are exhausted.

```csharp
public static bool TryGetFlags(byte playerIndex, ref Byte flags)
```

#### Parameters

`playerIndex` [Byte](https://learn.microsoft.com/dotnet/api/system.byte)<br>

`flags` [Byte&](https://learn.microsoft.com/dotnet/api/system.byte&)<br>

#### Returns

`true` when `playerIndex` is 1-7;
 `flags` is then the matching LED mask.
