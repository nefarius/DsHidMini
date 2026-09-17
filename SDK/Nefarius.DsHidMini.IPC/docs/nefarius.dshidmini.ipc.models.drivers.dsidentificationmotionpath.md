# DsIdentificationMotionPath

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

Gyro / motion code path derived from the Feature 0x01 calibration field list.
 Type bytes 8-11 must not be used to pick this path (SIXAXIS-2 reports 0x18).

```csharp
public enum DsIdentificationMotionPath
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [Enum](https://learn.microsoft.com/dotnet/api/system.enum) → [DsIdentificationMotionPath](./nefarius.dshidmini.ipc.models.drivers.dsidentificationmotionpath.md)<br>
Implements [IComparable](https://learn.microsoft.com/dotnet/api/system.icomparable), [ISpanFormattable](https://learn.microsoft.com/dotnet/api/system.ispanformattable), [IFormattable](https://learn.microsoft.com/dotnet/api/system.iformattable), [IConvertible](https://learn.microsoft.com/dotnet/api/system.iconvertible)<br>
Attributes [TypeConverterAttribute](https://learn.microsoft.com/dotnet/api/system.componentmodel.typeconverterattribute)

## Fields

| Name | Value | Description |
| --- | --: | --- |
| Unknown | 0 | Report missing or field list could not be parsed. |
| PlainZero | 1 | Software zero against the EEPROM gyro zero. Field list starts `01 02` at index 0 or 1 and does not contain field `0x07`. |
| HwCal | 2 | Hardware-calibrated gyro. Field list contains `0x07`. |
| Sixaxis | 3 | Original SIXAXIS path. `PLAIN_ZERO` is clear (typically a single field `06`). |
