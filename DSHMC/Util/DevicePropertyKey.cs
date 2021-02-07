using System;
using Nefarius.Devcon;

namespace Nefarius.DsHidMini.Util
{
    /// <summary>
    ///     Describes a unified device property.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/windows-hardware/drivers/install/unified-device-property-model--windows-vista-and-later-</remarks>
    public abstract class DevicePropertyKey : IComparable
    {
        protected DevicePropertyKey(
            Guid categoryGuid,
            uint propertyIdentifier,
            Type propertyType
        )
        {
            CategoryGuid = categoryGuid;
            PropertyIdentifier = propertyIdentifier;
            PropertyType = propertyType;
        }

        public Guid CategoryGuid { get; }

        public uint PropertyIdentifier { get; }

        public Type PropertyType { get; }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        internal SetupApiWrapper.DEVPROPKEY ToNativeType()
        {
            return new SetupApiWrapper.DEVPROPKEY(CategoryGuid, PropertyIdentifier);
        }
    }

    /// <summary>
    ///     Describes a custom device property.
    /// </summary>
    public class CustomDeviceProperty : DevicePropertyKey
    {
        protected CustomDeviceProperty(Guid categoryGuid, uint propertyIdentifier, Type propertyType)
            : base(categoryGuid, propertyIdentifier, propertyType)
        {
        }

        public static DevicePropertyKey CreateCustomDeviceProperty(
            Guid categoryGuid,
            uint propertyIdentifier,
            Type propertyType
        )
        {
            return new CustomDeviceProperty(categoryGuid, propertyIdentifier, propertyType);
        }
    }

    public abstract class DevicePropertyDevice : DevicePropertyKey
    {
        public static DevicePropertyKey DeviceDesc = new DevicePropertyDeviceDeviceDesc();

        public static DevicePropertyKey HardwareIds = new DevicePropertyDeviceHardwareIds();

        public static DevicePropertyKey CompatibleIds = new DevicePropertyDeviceCompatibleIds();

        public static DevicePropertyKey Manufacturer = new DevicePropertyDeviceManufacturer();

        public static DevicePropertyKey FriendlyName = new DevicePropertyDeviceFriendlyName();

        public static DevicePropertyKey EnumeratorName = new DevicePropertyDeviceEnumeratorName();
        
        public static DevicePropertyKey InstanceId = new DevicePropertyDeviceInstanceId();

        private DevicePropertyDevice(uint propertyIdentifier, Type propertyType) : this(
            Guid.Parse("{0xa45c254e, 0xdf1c, 0x4efd, {0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0}}"),
            propertyIdentifier,
            propertyType
        )
        {
        }

        protected DevicePropertyDevice(Guid categoryGuid, uint propertyIdentifier, Type propertyType)
            : base(categoryGuid, propertyIdentifier, propertyType)
        {
        }

        private class DevicePropertyDeviceDeviceDesc : DevicePropertyDevice
        {
            public DevicePropertyDeviceDeviceDesc() : base(2, typeof(string))
            {
            }
        }

        private class DevicePropertyDeviceHardwareIds : DevicePropertyDevice
        {
            public DevicePropertyDeviceHardwareIds() : base(3, typeof(string[]))
            {
            }
        }

        private class DevicePropertyDeviceCompatibleIds : DevicePropertyDevice
        {
            public DevicePropertyDeviceCompatibleIds() : base(4, typeof(string[]))
            {
            }
        }

        private class DevicePropertyDeviceManufacturer : DevicePropertyDevice
        {
            public DevicePropertyDeviceManufacturer() : base(13, typeof(string))
            {
            }
        }

        private class DevicePropertyDeviceFriendlyName : DevicePropertyDevice
        {
            public DevicePropertyDeviceFriendlyName() : base(14, typeof(string))
            {
            }
        }

        private class DevicePropertyDeviceEnumeratorName : DevicePropertyDevice
        {
            public DevicePropertyDeviceEnumeratorName() : base(24, typeof(string))
            {
            }
        }

        private class DevicePropertyDeviceInstanceId : DevicePropertyDevice
        {
            public DevicePropertyDeviceInstanceId()
                : base(Guid.Parse("{0x78c34fc8, 0x104a, 0x4aca, {0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57}}"),
                    256, typeof(string))
            {
            }
        }
    }
}