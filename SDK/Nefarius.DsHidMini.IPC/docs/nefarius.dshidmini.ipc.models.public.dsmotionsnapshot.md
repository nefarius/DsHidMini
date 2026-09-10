# DsMotionSnapshot

Namespace: Nefarius.DsHidMini.IPC.Models.Public

Versioned per-slot motion telemetry. Pack=1, 80 bytes; must stay in sync with driver `IPC_MOTION_SNAPSHOT_MESSAGE`.

```csharp
public struct DsMotionSnapshot
```

## Fields

| Name | Type | Description |
|------|------|-------------|
| `SlotIndex` | `UInt32` | One-based IPC slot. |
| `SequenceNumber` | `Int32` | Seqlock generation (odd = write in progress). |
| `Version` | `UInt16` | Snapshot layout version (`CurrentVersion` = 1). |
| `Flags` | `DsMotionSnapshotFlags` | Availability / fallback / tracker bits. |
| `AccelZeroX/Y/Z` | `UInt16` | EEPROM 0 g readings. |
| `AccelOneGX/Y/Z` | `UInt16` | EEPROM −1 g readings. |
| `GyroZero` | `UInt16` | EEPROM gyro rest reading. |
| `GyroEepromCal` | `UInt16` | EEPROM factory cal byte. |
| `RawAccelX/Y/Z`, `RawGyro` | `UInt16` | Host-order raw sensor values. |
| `CalAccelX/Y/Z` | `Int16` | Sony-calibrated accelerometer (X mirrored). |
| `CalGyro` | `UInt16` | Sony-calibrated yaw gyro. |
| `AccelMilliGX/Y/Z` | `Int32` | Calibrated acceleration in milli-g. |
| `GyroMilliDps` | `Int32` | Calibrated yaw rate in milli-deg/s. |
| `ZeroRef` | `Int32` | Tracker or EEPROM gyro zero. |
| `SampleIndex` | `UInt32` | Monotonic sample counter. |
| `TimestampQpc` | `UInt64` | `QueryPerformanceCounter` timestamp. |
| `CalByte` | `Byte` | Active output cal byte. |
| `MotionPath` | `DsIdentificationMotionPath` | Gyro path. |

## Properties

- `IsAvailable`, `IsFallback`, `HasTracker`
- `CurrentVersion` = 1
- `Size` = 80
