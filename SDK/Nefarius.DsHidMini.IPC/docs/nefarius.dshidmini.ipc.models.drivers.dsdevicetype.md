# DsDeviceType

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

Hardware family derived from USB/Bluetooth VID and PID. Matches `DS_DEVICE_TYPE` in the driver.

```csharp
public enum DsDeviceType
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [Enum](https://learn.microsoft.com/dotnet/api/system.enum) → [DsDeviceType](./nefarius.dshidmini.ipc.models.drivers.dsdevicetype.md)<br>
Implements [IComparable](https://learn.microsoft.com/dotnet/api/system.icomparable), [ISpanFormattable](https://learn.microsoft.com/dotnet/api/system.ispanformattable), [IFormattable](https://learn.microsoft.com/dotnet/api/system.iformattable), [IConvertible](https://learn.microsoft.com/dotnet/api/system.iconvertible)<br>
Attributes [TypeConverterAttribute](https://learn.microsoft.com/dotnet/api/system.componentmodel.typeconverterattribute)

## Fields

| Name | Value | Description |
| --- | --: | --- |
| Unknown | 0 | Unknown or unclassified device. |
| Sixaxis | 1 | Sony DualShock 3 / SIXAXIS. |
| Navigation | 2 | Sony Navigation Controller (CECH-ZCS1, PID 0x042F). One LED, no rumble. |
| Motion | 3 | Sony PlayStation Move Motion Controller. Not supported. |
| Wireless | 4 | Sony DualShock 4. Not supported as a DsHidMini target. |
