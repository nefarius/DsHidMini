# DsMotionSnapshot

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Versioned per-slot motion telemetry. Pack=1, 80 bytes; must stay in sync
 with driver `IPC_MOTION_SNAPSHOT_MESSAGE`.

```csharp
public struct DsMotionSnapshot
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [DsMotionSnapshot](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshot.md)

## Fields

### <a id="fields-accelmilligx"/>**AccelMilliGX**

```csharp
public int AccelMilliGX;
```

### <a id="fields-accelmilligy"/>**AccelMilliGY**

```csharp
public int AccelMilliGY;
```

### <a id="fields-accelmilligz"/>**AccelMilliGZ**

```csharp
public int AccelMilliGZ;
```

### <a id="fields-accelonegx"/>**AccelOneGX**

```csharp
public ushort AccelOneGX;
```

### <a id="fields-accelonegy"/>**AccelOneGY**

```csharp
public ushort AccelOneGY;
```

### <a id="fields-accelonegz"/>**AccelOneGZ**

```csharp
public ushort AccelOneGZ;
```

### <a id="fields-accelzerox"/>**AccelZeroX**

```csharp
public ushort AccelZeroX;
```

### <a id="fields-accelzeroy"/>**AccelZeroY**

```csharp
public ushort AccelZeroY;
```

### <a id="fields-accelzeroz"/>**AccelZeroZ**

```csharp
public ushort AccelZeroZ;
```

### <a id="fields-calaccelx"/>**CalAccelX**

```csharp
public short CalAccelX;
```

### <a id="fields-calaccely"/>**CalAccelY**

```csharp
public short CalAccelY;
```

### <a id="fields-calaccelz"/>**CalAccelZ**

```csharp
public short CalAccelZ;
```

### <a id="fields-calbyte"/>**CalByte**

```csharp
public byte CalByte;
```

### <a id="fields-calgyro"/>**CalGyro**

```csharp
public ushort CalGyro;
```

### <a id="fields-currentversion"/>**CurrentVersion**

```csharp
public static ushort CurrentVersion;
```

### <a id="fields-flags"/>**Flags**

```csharp
public DsMotionSnapshotFlags Flags;
```

### <a id="fields-gyroeepromcal"/>**GyroEepromCal**

```csharp
public ushort GyroEepromCal;
```

### <a id="fields-gyromillidps"/>**GyroMilliDps**

```csharp
public int GyroMilliDps;
```

### <a id="fields-gyrozero"/>**GyroZero**

```csharp
public ushort GyroZero;
```

### <a id="fields-motionpath"/>**MotionPath**

```csharp
public DsIdentificationMotionPath MotionPath;
```

### <a id="fields-rawaccelx"/>**RawAccelX**

```csharp
public ushort RawAccelX;
```

### <a id="fields-rawaccely"/>**RawAccelY**

```csharp
public ushort RawAccelY;
```

### <a id="fields-rawaccelz"/>**RawAccelZ**

```csharp
public ushort RawAccelZ;
```

### <a id="fields-rawgyro"/>**RawGyro**

```csharp
public ushort RawGyro;
```

### <a id="fields-reserved0"/>**Reserved0**

```csharp
public byte Reserved0;
```

### <a id="fields-reserved1"/>**Reserved1**

```csharp
public byte Reserved1;
```

### <a id="fields-sampleindex"/>**SampleIndex**

```csharp
public uint SampleIndex;
```

### <a id="fields-sequencenumber"/>**SequenceNumber**

```csharp
public int SequenceNumber;
```

### <a id="fields-size"/>**Size**

```csharp
public static int Size;
```

### <a id="fields-slotindex"/>**SlotIndex**

```csharp
public uint SlotIndex;
```

### <a id="fields-timestampqpc"/>**TimestampQpc**

```csharp
public ulong TimestampQpc;
```

### <a id="fields-version"/>**Version**

```csharp
public ushort Version;
```

### <a id="fields-zeroref"/>**ZeroRef**

```csharp
public int ZeroRef;
```

## Properties

### <a id="properties-hassoftwarezero"/>**HasSoftwareZero**

```csharp
public bool HasSoftwareZero { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

### <a id="properties-hastracker"/>**HasTracker**

```csharp
public bool HasTracker { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

### <a id="properties-isavailable"/>**IsAvailable**

```csharp
public bool IsAvailable { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

### <a id="properties-isfallback"/>**IsFallback**

```csharp
public bool IsFallback { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>
