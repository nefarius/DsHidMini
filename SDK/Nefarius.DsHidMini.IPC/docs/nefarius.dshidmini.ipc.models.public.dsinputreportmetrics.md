# DsInputReportMetrics

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Versioned per-slot input-report arrival metrics. Pack=1, 28 bytes; must
 stay in sync with driver `IPC_INPUT_REPORT_METRICS_MESSAGE`.

The interval is host-side arrival spacing between successful USB interrupt
 completions or Bluetooth interrupt packets, not one-way packet latency.
 A value of `0` means no sufficient reports were observed.

```csharp
public struct DsInputReportMetrics
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [DsInputReportMetrics](./nefarius.dshidmini.ipc.models.public.dsinputreportmetrics.md)

## Fields

### <a id="fields-averageintervalus"/>**AverageIntervalUs**

```csharp
public uint AverageIntervalUs;
```

### <a id="fields-reportratehz"/>**ReportRateHz**

```csharp
public uint ReportRateHz;
```

### <a id="fields-reserved0"/>**Reserved0**

```csharp
public ushort Reserved0;
```

### <a id="fields-sequencenumber"/>**SequenceNumber**

```csharp
public int SequenceNumber;
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

## Fields

### <a id="fields-currentversion"/>**CurrentVersion**

```csharp
public const ushort CurrentVersion = 1;
```

### <a id="fields-size"/>**Size**

```csharp
public const int Size = 28;
```
