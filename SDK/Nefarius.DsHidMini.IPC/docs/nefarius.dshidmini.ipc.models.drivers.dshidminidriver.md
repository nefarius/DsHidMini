# DsHidMiniDriver

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

Interface and property information about the DsHidMini driver.

```csharp
public static class DsHidMiniDriver
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [DsHidMiniDriver](./nefarius.dshidmini.ipc.models.drivers.dshidminidriver.md)<br>
Attributes [NullableContextAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.nullablecontextattribute), [NullableAttribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.nullableattribute)

## Properties

### <a id="properties-batterystatusproperty"/>**BatteryStatusProperty**

The last reported [DsBatteryStatus](./nefarius.dshidmini.ipc.models.drivers.dsbatterystatus.md) of the device.

```csharp
public static DevicePropertyKey BatteryStatusProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-bluetoothlastconnectedtimeproperty"/>**BluetoothLastConnectedTimeProperty**

Timestamp of last wireless connection.

```csharp
public static DevicePropertyKey BluetoothLastConnectedTimeProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-deviceaddressproperty"/>**DeviceAddressProperty**

The Bluetooth MAC address of the device itself.

```csharp
public static DevicePropertyKey DeviceAddressProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-deviceaddresssynthesizedproperty"/>**DeviceAddressSynthesizedProperty**

`true` if [DsHidMiniDriver.DeviceAddressProperty](./nefarius.dshidmini.ipc.models.drivers.dshidminidriver.md#deviceaddressproperty) was not reported by the hardware (device
 did not answer `GET Feature 0xF2`) and was synthesized by the driver instead. Bluetooth pairing is
 unavailable for such a device. See issue #321.

```csharp
public static DevicePropertyKey DeviceAddressSynthesizedProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-deviceinterfaceguid"/>**DeviceInterfaceGuid**

Interface GUID common to all devices the DsHidMini driver supports.

```csharp
public static Guid DeviceInterfaceGuid { get; }
```

#### Property Value

[Guid](https://learn.microsoft.com/dotnet/api/system.guid)<br>

### <a id="properties-devicetypeproperty"/>**DeviceTypeProperty**

Hardware family ([DsDeviceType](./nefarius.dshidmini.ipc.models.drivers.dsdevicetype.md)). Navigation has one LED and no rumble.

```csharp
public static DevicePropertyKey DeviceTypeProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-hiddevicemodeproperty"/>**HidDeviceModeProperty**

The currently active [DsHidDeviceMode](./nefarius.dshidmini.ipc.models.drivers.dshiddevicemode.md).

```csharp
public static DevicePropertyKey HidDeviceModeProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-hostaddressproperty"/>**HostAddressProperty**

The Bluetooth MAC address the device is currently paired to.

```csharp
public static DevicePropertyKey HostAddressProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-identificationcloneheuristicproperty"/>**IdentificationCloneHeuristicProperty**

`true` if Feature 0x01 matches the clone heuristic (field list
 `01 02` and byte `0x29 == 0x64`). Heuristic, not a verdict.

```csharp
public static DevicePropertyKey IdentificationCloneHeuristicProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-identificationdataproperty"/>**IdentificationDataProperty**

Raw 64-byte `GET Feature 0x01` identification blob. Published on a live USB
 read, or read back on Bluetooth from the matching USB instance's cache - no known
 Bluetooth host ever queries this feature itself. See issue #50 and #217.

```csharp
public static DevicePropertyKey IdentificationDataProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-identificationfirmwareproperty"/>**IdentificationFirmwareProperty**

Feature 0x01 firmware/board revision packed as `b2<<16 | b3<<8 | b4`.

```csharp
public static DevicePropertyKey IdentificationFirmwareProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-identificationmotionpathproperty"/>**IdentificationMotionPathProperty**

Feature 0x01 motion path derived from the calibration field list.

```csharp
public static DevicePropertyKey IdentificationMotionPathProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-identificationpadtypeproperty"/>**IdentificationPadTypeProperty**

Feature 0x01 pad/sensor type byte (offset 8). Informational only; not a gyro-path test.

```csharp
public static DevicePropertyKey IdentificationPadTypeProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-ipcslotindexproperty"/>**IpcSlotIndexProperty**

One-based driver IPC slot index (`deviceIndex` for shared memory, per-slot events, and IPC `TargetIndex`).

```csharp
public static DevicePropertyKey IpcSlotIndexProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-lasthostrequeststatusproperty"/>**LastHostRequestStatusProperty**

```csharp
public static DevicePropertyKey LastHostRequestStatusProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-lastpairingstatusproperty"/>**LastPairingStatusProperty**

```csharp
public static DevicePropertyKey LastPairingStatusProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-motioncalibrationdataproperty"/>**MotionCalibrationDataProperty**

Raw 64-byte `GET Feature 0xEF` page `0xA0` EEPROM blob. Only ever written
 by a live USB read; a Bluetooth instance reads it back from the matching USB
 instance instead of asking the pad. See issue #217.

```csharp
public static DevicePropertyKey MotionCalibrationDataProperty { get; }
```

#### Property Value

DevicePropertyKey<br>

### <a id="properties-motioncalibrationsourceproperty"/>**MotionCalibrationSourceProperty**

How [DsHidMiniDriver.MotionCalibrationDataProperty](./nefarius.dshidmini.ipc.models.drivers.dshidminidriver.md#motioncalibrationdataproperty) / the IPC motion snapshot's
 calibration was populated for this device instance. See issue #217.

```csharp
public static DevicePropertyKey MotionCalibrationSourceProperty { get; }
```

#### Property Value

DevicePropertyKey<br>
