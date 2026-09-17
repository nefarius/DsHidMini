# DsMotionCalibrationSource

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

How a device instance's motion calibration was populated. Bluetooth never asks
 the pad for Feature 0x01/0xEF; it reads back whatever the pad's USB instance
 cached under [DsHidMiniDriver.MotionCalibrationDataProperty](./nefarius.dshidmini.ipc.models.drivers.dshidminidriver.md#motioncalibrationdataproperty).
 Matches `DS_MOTION_CALIBRATION_SOURCE` in the driver. See issue #217.

```csharp
public enum DsMotionCalibrationSource
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [Enum](https://learn.microsoft.com/dotnet/api/system.enum) → [DsMotionCalibrationSource](./nefarius.dshidmini.ipc.models.drivers.dsmotioncalibrationsource.md)<br>
Implements [IComparable](https://learn.microsoft.com/dotnet/api/system.icomparable), [ISpanFormattable](https://learn.microsoft.com/dotnet/api/system.ispanformattable), [IFormattable](https://learn.microsoft.com/dotnet/api/system.iformattable), [IConvertible](https://learn.microsoft.com/dotnet/api/system.iconvertible)<br>
Attributes [TypeConverterAttribute](https://learn.microsoft.com/dotnet/api/system.componentmodel.typeconverterattribute)

## Fields

| Name | Value | Description |
| --- | --: | --- |
| None | 0 | No calibration loaded; nominal 512/399 fallback is in effect. |
| LiveUsb | 1 | Read live from the pad over USB this session. |
| CachedFromUsb | 2 | Read from this pad's cached USB calibration; connected over Bluetooth. |
