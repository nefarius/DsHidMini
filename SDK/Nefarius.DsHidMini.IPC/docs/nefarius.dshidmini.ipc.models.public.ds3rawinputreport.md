# Ds3RawInputReport

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Raw input report as it is sent by the SIXAXIS/DualShock 3.

```csharp
public struct Ds3RawInputReport
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [ValueType](https://docs.microsoft.com/en-us/dotnet/api/system.valuetype) → [Ds3RawInputReport](./nefarius.dshidmini.ipc.models.public.ds3rawinputreport.md)

## Fields

### <a id="fields-accelerometerx"/>**AccelerometerX**

```csharp
public ushort AccelerometerX;
```

### <a id="fields-accelerometery"/>**AccelerometerY**

```csharp
public ushort AccelerometerY;
```

### <a id="fields-accelerometerz"/>**AccelerometerZ**

```csharp
public ushort AccelerometerZ;
```

### <a id="fields-batterystatus"/>**BatteryStatus**

Battery charge status.

```csharp
public byte BatteryStatus;
```

### <a id="fields-buttons"/>**Buttons**

The individual controller buttons.

```csharp
public ButtonUnion Buttons;
```

### <a id="fields-gyroscope"/>**Gyroscope**

```csharp
public ushort Gyroscope;
```

### <a id="fields-leftthumbx"/>**LeftThumbX**

Left Thumb X-axis.

```csharp
public byte LeftThumbX;
```

**Remarks:**

0x00 = left/bottom, 0x80 = centered, 0xFF = right/top

### <a id="fields-leftthumby"/>**LeftThumbY**

Left Thumb Y-axis.

```csharp
public byte LeftThumbY;
```

**Remarks:**

0x00 = left/bottom, 0x80 = centered, 0xFF = right/top

### <a id="fields-pressure"/>**Pressure**

Pressure button values.

```csharp
public PressureUnion Pressure;
```

### <a id="fields-reportid"/>**ReportId**

Report ID (always 1).

```csharp
public byte ReportId;
```

### <a id="fields-rightthumbx"/>**RightThumbX**

Right Thumb X-axis.

```csharp
public byte RightThumbX;
```

**Remarks:**

0x00 = left/bottom, 0x80 = centered, 0xFF = right/top

### <a id="fields-rightthumby"/>**RightThumbY**

Right Thumb Y-axis.

```csharp
public byte RightThumbY;
```

**Remarks:**

0x00 = left/bottom, 0x80 = centered, 0xFF = right/top
