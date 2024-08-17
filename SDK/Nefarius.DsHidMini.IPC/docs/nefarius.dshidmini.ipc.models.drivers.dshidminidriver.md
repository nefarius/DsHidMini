# DsHidMiniDriver

Namespace: Nefarius.DsHidMini.IPC.Models.Drivers

Interface and property information about the DsHidMini driver.

```csharp
public static class DsHidMiniDriver
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [DsHidMiniDriver](./nefarius.dshidmini.ipc.models.drivers.dshidminidriver.md)

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

### <a id="properties-deviceinterfaceguid"/>**DeviceInterfaceGuid**

Interface GUID common to all devices the DsHidMini driver supports.

```csharp
public static Guid DeviceInterfaceGuid { get; }
```

#### Property Value

[Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid)<br>

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
