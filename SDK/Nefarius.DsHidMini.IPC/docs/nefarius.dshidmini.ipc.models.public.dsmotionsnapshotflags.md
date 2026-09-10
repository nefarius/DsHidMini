# DsMotionSnapshotFlags

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Status bits for [DsMotionSnapshot](./nefarius.dshidmini.ipc.models.public.dsmotionsnapshot.md). Must stay in sync with `DSHM_IPC_MOTION_FLAG_*` in the driver.

```csharp
[Flags]
public enum DsMotionSnapshotFlags : ushort
```

| Name | Value | Description |
|------|-------|-------------|
| `None` | 0 | No flags. |
| `Available` | 0x0001 | The slot contains a processed sample. |
| `Fallback` | 0x0002 | Nominal calibration is in effect. |
| `HardwareCal` | 0x0004 | The driver writes a hardware gyro cal byte. |
| `Tracker` | 0x0008 | Sony's auto-zero tracker is running. |
