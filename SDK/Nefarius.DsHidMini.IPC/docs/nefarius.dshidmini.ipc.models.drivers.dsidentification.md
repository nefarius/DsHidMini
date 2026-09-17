# DsIdentification

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

Parses the 64-byte Feature 0x01 identification blob. Rules match
 `DsIdentification_Parse` in the driver and `docs/MOTION.md`.

```csharp
public static class DsIdentification
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [DsIdentification](./nefarius.dshidmini.ipc.models.drivers.dsidentification.md)

## Fields

### <a id="fields-clonebyteoffset"/>**CloneByteOffset**

```csharp
public static int CloneByteOffset;
```

### <a id="fields-fieldcountoffset"/>**FieldCountOffset**

```csharp
public static int FieldCountOffset;
```

### <a id="fields-fieldlistoffset"/>**FieldListOffset**

```csharp
public static int FieldListOffset;
```

### <a id="fields-maxfields"/>**MaxFields**

```csharp
public static int MaxFields;
```

### <a id="fields-minparselength"/>**MinParseLength**

```csharp
public static int MinParseLength;
```

### <a id="fields-reportlength"/>**ReportLength**

```csharp
public static int ReportLength;
```

## Methods

### <a id="methods-tryparse"/>**TryParse(ReadOnlySpan&lt;Byte&gt;, ref DsIdentificationInfo)**

```csharp
public static bool TryParse(ReadOnlySpan<Byte> report, ref DsIdentificationInfo info)
```

#### Parameters

`report` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)<[Byte](https://learn.microsoft.com/dotnet/api/system.byte)><br>

`info` [DsIdentificationInfo&](./nefarius.dshidmini.ipc.models.drivers.dsidentificationinfo&.md)<br>

#### Returns

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)
