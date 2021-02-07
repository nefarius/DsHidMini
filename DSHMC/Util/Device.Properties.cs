using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Nefarius.Devcon;

namespace Nefarius.DsHidMini.Util
{
    public partial class Device
    {
        private static readonly IDictionary<SetupApiWrapper.DevPropType, Type> NativeToManagedTypeMap =
            new Dictionary<SetupApiWrapper.DevPropType, Type>
            {
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_SBYTE, typeof(sbyte)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_BYTE, typeof(byte)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_INT16, typeof(short)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_UINT16, typeof(ushort)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_INT32, typeof(int)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_UINT32, typeof(uint)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_INT64, typeof(long)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_UINT64, typeof(ulong)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_FLOAT, typeof(float)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DOUBLE, typeof(double)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DECIMAL, typeof(decimal)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_GUID, typeof(Guid)},
                // DEVPROP_TYPE_CURRENCY
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DATE, typeof(DateTime)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_FILETIME, typeof(DateTimeOffset)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_BOOLEAN, typeof(bool)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_STRING, typeof(string)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_STRING_LIST, typeof(string[])},
                // DEVPROP_TYPE_SECURITY_DESCRIPTOR
                // DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DEVPROPKEY, typeof(SetupApiWrapper.DEVPROPKEY)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DEVPROPTYPE, typeof(SetupApiWrapper.DevPropType)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_BINARY, typeof(byte[])},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_ERROR, typeof(int)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_NTSTATUS, typeof(int)}
                // DEVPROP_TYPE_STRING_INDIRECT
            };

        /// <summary>
        ///     Returns a device instance property identified by <see cref="DevicePropertyKey" />.
        /// </summary>
        /// <typeparam name="T">The managed type of the fetched porperty value.</typeparam>
        /// <param name="propertyKey">The <see cref="DevicePropertyKey" /> to query for.</param>
        /// <returns>On success, the value of the queried property.</returns>
        public T GetProperty<T>(DevicePropertyKey propertyKey)
        {
            if (typeof(T) != propertyKey.PropertyType)
                throw new ArgumentException(
                    "The supplied object type doesn't match the property type.",
                    nameof(propertyKey)
                );

            var buffer = IntPtr.Zero;

            try
            {
                if (GetProperty(
                    propertyKey.ToNativeType(),
                    out var propertyType,
                    out buffer,
                    out var size
                ) != SetupApiWrapper.ConfigManagerResult.Success)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!NativeToManagedTypeMap.TryGetValue(propertyType, out var managedType))
                    throw new ArgumentException(
                        "Unknown property type.",
                        nameof(propertyKey)
                    );

                if (typeof(T) != managedType)
                    throw new ArgumentException(
                        "The supplied object type doesn't match the property type.",
                        nameof(propertyKey)
                    );

                #region Don't look, nasty trickery

                /*
                 * Handle some native to managed conversions
                 */

                // Regular strings
                if (managedType == typeof(string))
                {
                    var value = Marshal.PtrToStringUni(buffer);
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                // Double-null-terminated string to list
                if (managedType == typeof(string[]))
                    return (T) (object) Marshal.PtrToStringUni(buffer, (int) size / 2).TrimEnd('\0').Split('\0')
                        .ToArray();

                // Byte & SByte
                if (managedType == typeof(sbyte)
                    || managedType == typeof(byte))
                    return (T) (object) Marshal.ReadByte(buffer);

                // (U)Int16
                if (managedType == typeof(short)
                    || managedType == typeof(ushort))
                    return (T) (object) Marshal.ReadInt16(buffer);

                // (U)Int32
                if (managedType == typeof(int)
                    || managedType == typeof(uint))
                    return (T) (object) Marshal.ReadInt32(buffer);

                // (U)Int64
                if (managedType == typeof(long)
                    || managedType == typeof(ulong))
                    return (T) (object) Marshal.ReadInt64(buffer);

                // FILETIME/DateTimeOffset
                if (managedType == typeof(DateTimeOffset))
                    return (T) (object) DateTimeOffset.FromFileTime(Marshal.ReadInt64(buffer));

                #endregion

                throw new NotImplementedException("Type not supported.");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private SetupApiWrapper.ConfigManagerResult GetProperty(
            SetupApiWrapper.DEVPROPKEY propertyKey,
            out SetupApiWrapper.DevPropType propertyType,
            out IntPtr valueBuffer,
            out uint valueBufferSize
        )
        {
            valueBufferSize = 2018;

            valueBuffer = Marshal.AllocHGlobal((int) valueBufferSize);

            var ret = SetupApiWrapper.CM_Get_DevNode_Property(
                _instanceHandle,
                ref propertyKey,
                out propertyType,
                valueBuffer,
                ref valueBufferSize,
                0
            );

            if (ret == SetupApiWrapper.ConfigManagerResult.Success) return ret;
            Marshal.FreeHGlobal(valueBuffer);
            valueBuffer = IntPtr.Zero;
            return ret;
        }
    }
}