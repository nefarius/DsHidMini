# DsBatteryStatus

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

Battery status values.

```csharp
public enum DsBatteryStatus
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [ValueType](https://docs.microsoft.com/en-us/dotnet/api/system.valuetype) → [Enum](https://docs.microsoft.com/en-us/dotnet/api/system.enum) → [DsBatteryStatus](./nefarius.dshidmini.ipc.models.drivers.dsbatterystatus.md)<br>
Implements [IComparable](https://docs.microsoft.com/en-us/dotnet/api/system.icomparable), [ISpanFormattable](https://docs.microsoft.com/en-us/dotnet/api/system.ispanformattable), [IFormattable](https://docs.microsoft.com/en-us/dotnet/api/system.iformattable), [IConvertible](https://docs.microsoft.com/en-us/dotnet/api/system.iconvertible)

## Fields

| Name | Value | Description |
| --- | --: | --- |
| Unknown | 0 | Unknown/not yet reported. |
| Dying | 1 | Dying. Battery is so low the device is barely being kept on. |
| Low | 2 | Low. Device should be charged soon. |
| Medium | 3 | Medium. Will last for a while but should be charged soon. |
| High | 4 | High. Will last for a few sessions. |
| Full | 5 | Full. Status right after successful charging. |
| Charging | 238 | Charging. The default state while wired until [DsBatteryStatus.Charged](./nefarius.dshidmini.ipc.models.drivers.dsbatterystatus.md#charged) is reached. |
| Charged | 239 | Charged. While wired synonymous to [DsBatteryStatus.Full](./nefarius.dshidmini.ipc.models.drivers.dsbatterystatus.md#full) while wireless. |
