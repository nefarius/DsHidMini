# DsMotionSnapshotFlags

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Status bits for [DsMotionSnapshot](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshot.md). Must stay in sync with
 `DSHM_IPC_MOTION_FLAG_*` in the driver.

```csharp
public enum DsMotionSnapshotFlags
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [Enum](https://learn.microsoft.com/dotnet/api/system.enum) → [DsMotionSnapshotFlags](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshotflags.md)<br>
Implements [IComparable](https://learn.microsoft.com/dotnet/api/system.icomparable), [ISpanFormattable](https://learn.microsoft.com/dotnet/api/system.ispanformattable), [IFormattable](https://learn.microsoft.com/dotnet/api/system.iformattable), [IConvertible](https://learn.microsoft.com/dotnet/api/system.iconvertible)<br>
Attributes [FlagsAttribute](https://learn.microsoft.com/dotnet/api/system.flagsattribute)

## Fields (Flags)

| Name | Value | Description |
| --- | --: | --- |
| Available | 1 | The slot contains a processed sample. |
| Fallback | 2 | EEPROM page `0xA0` was not used. Nominal `zero=512`, `oneG=399` are in effect (failed or skipped Feature 0xEF). |
| HardwareCal | 4 | The driver is writing a hardware gyro cal byte on output reports. |
| Tracker | 8 | Sony's auto-zero tracker is running (`HW_CAL` / `SIXAXIS` hardware trim, or clone-heuristic `PLAIN_ZERO` software-only). |
| SoftwareZero | 16 | The tracker is in software-only mode: `zeroRef` follows rest and the hardware cal byte is never stepped. Used for clone-heuristic `PLAIN_ZERO` pads whose EEPROM gyro zero is a template. |
