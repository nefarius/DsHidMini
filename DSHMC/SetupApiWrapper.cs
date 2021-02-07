using System;
using System.Runtime.InteropServices;

namespace Nefarius.Devcon
{
    internal static class SetupApiWrapper
    {
        #region Constant and Structure Definitions

        internal const int DIGCF_PRESENT = 0x0002;
        internal const int DIGCF_DEVICEINTERFACE = 0x0010;

        internal const int DICD_GENERATE_ID = 0x0001;
        internal const int SPDRP_HARDWAREID = 0x0001;

        internal const int DIF_REMOVE = 0x0005;
        internal const int DIF_REGISTERDEVICE = 0x0019;

        internal const int DI_REMOVEDEVICE_GLOBAL = 0x0001;
        
        internal const int DI_NEEDRESTART = 0x00000080;
        internal const int DI_NEEDREBOOT = 0x00000100;
        
        internal const uint DIF_PROPERTYCHANGE = 0x12;
        internal const uint DICS_ENABLE = 1;
        internal const uint DICS_DISABLE = 2;  // disable device
        internal const uint DICS_FLAG_GLOBAL = 1; // not profile-specific
        internal const uint DIGCF_ALLCLASSES = 4;
        internal const uint ERROR_INVALID_DATA = 13;
        internal const uint ERROR_NO_MORE_ITEMS = 259;
        internal const uint ERROR_ELEMENT_NOT_FOUND = 1168;
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVINFO_DATA
        {
            internal int cbSize;
            internal readonly Guid ClassGuid;
            internal readonly uint DevInst;
            internal readonly IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_CLASSINSTALL_HEADER
        {
            internal int cbSize;
            internal int InstallFunction;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_PROPCHANGE_PARAMS
        {
            internal SP_CLASSINSTALL_HEADER ClassInstallHeader;
            internal UInt32 StateChange;
            internal UInt32 Scope;
            internal UInt32 HwProfile;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_REMOVEDEVICE_PARAMS
        {
            internal SP_CLASSINSTALL_HEADER ClassInstallHeader;
            internal int Scope;
            internal int HwProfile;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct DEVPROPKEY
        {
            public DEVPROPKEY(Guid guid, uint pid)
            {
                this.fmtid = guid;
                this.pid = pid;
            }
            public Guid fmtid;
            public UInt32 pid;
        }

        internal const uint CM_REENUMERATE_NORMAL = 0x00000000;

        internal const uint CM_REENUMERATE_SYNCHRONOUS = 0x00000001;

        // XP and later versions 
        internal const uint CM_REENUMERATE_RETRY_INSTALLATION = 0x00000002;

        internal const uint CM_REENUMERATE_ASYNCHRONOUS = 0x00000004;
        
        internal enum CM_LOCATE_DEVNODE_FLAG : uint
        {
            CM_LOCATE_DEVNODE_NORMAL = 0x00000000,
            CM_LOCATE_DEVNODE_PHANTOM = 0x00000001,
            CM_LOCATE_DEVNODE_CANCELREMOVE = 0x00000002,
            CM_LOCATE_DEVNODE_NOVALIDATION = 0x00000004,
            CM_LOCATE_DEVNODE_BITS = 0x00000007,
        }

        internal enum ConfigManagerResult : uint
        {
            Success = 0x00000000,
            Default = 0x00000001,
            OutOfMemory = 0x00000002,
            InvalidPointer = 0x00000003,
            InvalidFlag = 0x00000004,
            InvalidDevnode = 0x00000005,
            InvalidDevinst = InvalidDevnode,
            InvalidResDes = 0x00000006,
            InvalidLogConf = 0x00000007,
            InvalidArbitrator = 0x00000008,
            InvalidNodelist = 0x00000009,
            DevnodeHasReqs = 0x0000000A,
            DevinstHasReqs = DevnodeHasReqs,
            InvalidResourceid = 0x0000000B,
            NoSuchDevnode = 0x0000000D,
            NoSuchDevinst = NoSuchDevnode,
            NoMoreLogConf = 0x0000000E,
            NoMoreResDes = 0x0000000F,
            AlreadySuchDevnode = 0x00000010,
            AlreadySuchDevinst = AlreadySuchDevnode,
            InvalidRangeList = 0x00000011,
            InvalidRange = 0x00000012,
            Failure = 0x00000013,
            NoSuchLogicalDev = 0x00000014,
            CreateBlocked = 0x00000015,
            RemoveVetoed = 0x00000017,
            ApmVetoed = 0x00000018,
            InvalidLoadType = 0x00000019,
            BufferSmall = 0x0000001A,
            NoArbitrator = 0x0000001B,
            NoRegistryHandle = 0x0000001C,
            RegistryError = 0x0000001D,
            InvalidDeviceId = 0x0000001E,
            InvalidData = 0x0000001F,
            InvalidApi = 0x00000020,
            DevloaderNotReady = 0x00000021,
            NeedRestart = 0x00000022,
            NoMoreHwProfiles = 0x00000023,
            DeviceNotThere = 0x00000024,
            NoSuchValue = 0x00000025,
            WrongType = 0x00000026,
            InvalidPriority = 0x00000027,
            NotDisableable = 0x00000028,
            FreeResources = 0x00000029,
            QueryVetoed = 0x0000002A,
            CantShareIrq = 0x0000002B,
            NoDependent = 0x0000002C,
            SameResources = 0x0000002D,
            NoSuchRegistryKey = 0x0000002E,
            InvalidMachinename = 0x0000002F,   // NT ONLY
            RemoteCommFailure = 0x00000030,   // NT ONLY
            MachineUnavailable = 0x00000031,   // NT ONLY
            NoCmServices = 0x00000032,   // NT ONLY
            AccessDenied = 0x00000033,   // NT ONLY
            CallNotImplemented = 0x00000034,
            InvalidProperty = 0x00000035,
            DeviceInterfaceActive = 0x00000036,
            NoSuchDeviceInterface = 0x00000037,
            InvalidReferenceString = 0x00000038,
            InvalidConflictList = 0x00000039,
            InvalidIndex = 0x0000003A,
            InvalidStructureSize = 0x0000003B
        }

        internal const int DEVPROP_TYPEMOD_ARRAY                 =  0x00001000;  // array of fixed-sized data elements
        internal const int DEVPROP_TYPEMOD_LIST                  =  0x00002000;  // list of variable-sized data elements

        internal enum DevPropType : uint
        {
            DEVPROP_TYPE_EMPTY = 0x00000000,  // nothing, no property data
            DEVPROP_TYPE_NULL = 0x00000001,  // null property data
            DEVPROP_TYPE_SBYTE = 0x00000002,  // 8-bit signed int (SBYTE)
            DEVPROP_TYPE_BYTE = 0x00000003,  // 8-bit unsigned int (BYTE)
            DEVPROP_TYPE_INT16 = 0x00000004,  // 16-bit signed int (SHORT)
            DEVPROP_TYPE_UINT16 = 0x00000005,  // 16-bit unsigned int (USHORT)
            DEVPROP_TYPE_INT32 = 0x00000006,  // 32-bit signed int (LONG)
            DEVPROP_TYPE_UINT32 = 0x00000007,  // 32-bit unsigned int (ULONG)
            DEVPROP_TYPE_INT64 = 0x00000008,  // 64-bit signed int (LONG64)
            DEVPROP_TYPE_UINT64 = 0x00000009,  // 64-bit unsigned int (ULONG64)
            DEVPROP_TYPE_FLOAT = 0x0000000A,  // 32-bit floating-point (FLOAT)
            DEVPROP_TYPE_DOUBLE = 0x0000000B,  // 64-bit floating-point (DOUBLE)
            DEVPROP_TYPE_DECIMAL = 0x0000000C,  // 128-bit data (DECIMAL)
            DEVPROP_TYPE_GUID = 0x0000000D,  // 128-bit unique identifier (GUID)
            DEVPROP_TYPE_CURRENCY = 0x0000000E,  // 64 bit signed int currency value (CURRENCY)
            DEVPROP_TYPE_DATE = 0x0000000F,  // date (DATE)
            DEVPROP_TYPE_FILETIME = 0x00000010,  // file time (FILETIME)
            DEVPROP_TYPE_BOOLEAN = 0x00000011,  // 8-bit boolean (DEVPROP_BOOLEAN)
            DEVPROP_TYPE_STRING = 0x00000012,  // null-terminated string
            DEVPROP_TYPE_STRING_LIST = (DEVPROP_TYPE_STRING | DEVPROP_TYPEMOD_LIST), // multi-sz string list
            DEVPROP_TYPE_SECURITY_DESCRIPTOR = 0x00000013,  // self-relative binary SECURITY_DESCRIPTOR
            DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING = 0x00000014,  // security descriptor string (SDDL format)
            DEVPROP_TYPE_DEVPROPKEY = 0x00000015,  // device property key (DEVPROPKEY)
            DEVPROP_TYPE_DEVPROPTYPE = 0x00000016,  // device property type (DEVPROPTYPE)
            DEVPROP_TYPE_BINARY = (DEVPROP_TYPE_BYTE | DEVPROP_TYPEMOD_ARRAY),  // custom binary data
            DEVPROP_TYPE_ERROR = 0x00000017,  // 32-bit Win32 system error code
            DEVPROP_TYPE_NTSTATUS = 0x00000018,  // 32-bit NTSTATUS code
            DEVPROP_TYPE_STRING_INDIRECT = 0x00000019  // string resource (@[path\]<dllname>,-<strId>)
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DRVINFO_DATA
        {
            internal readonly uint cbSize;
            internal readonly uint DriverType;
            internal readonly IntPtr Reserved;
            internal readonly string Description;
            internal readonly string MfgName;
            internal readonly string ProviderName;
            internal readonly DateTime DriverDate;
            internal readonly ulong DriverVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            internal Int32    cbSize;
            internal Guid     interfaceClassGuid;
            internal Int32    flags;
            internal UIntPtr  reserved;
        }


        [Flags]
        internal enum DiFlags : uint
        {
            DIIDFLAG_SHOWSEARCHUI = 1,
            DIIDFLAG_NOFINISHINSTALLUI = 2,
            DIIDFLAG_INSTALLNULLDRIVER = 3
        }

        internal const uint DIIRFLAG_FORCE_INF = 0x00000002;

        internal const uint INSTALLFLAG_FORCE             = 0x00000001;  // Force the installation of the specified driver
        internal const uint INSTALLFLAG_READONLY          = 0x00000002;  // Do a read-only install (no file copy)
        internal const uint INSTALLFLAG_NONINTERACTIVE    = 0x00000004;

        #endregion

        #region Interop Definitions

        #region SetupAPI

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid ClassGuid, IntPtr hwndParent);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiCreateDeviceInfo(IntPtr DeviceInfoSet, string DeviceName, ref Guid ClassGuid,
            string DeviceDescription, IntPtr hwndParent, int CreationFlags, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiSetDeviceRegistryProperty(IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData, int Property, [MarshalAs(UnmanagedType.LPWStr)] string PropertyBuffer,
            int PropertyBufferSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiCallClassInstaller(int InstallFunction, IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);
        
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet,
            UInt32 memberIndex,
            [Out] out SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent,
            int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVINFO_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData,
            int DeviceInterfaceDetailDataSize,
            ref int RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiOpenDeviceInfo(IntPtr DeviceInfoSet, string DeviceInstanceId,
            IntPtr hwndParent, int Flags, ref SP_DEVINFO_DATA DeviceInfoData);
        
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiChangeState(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInterfaceData, ref SP_REMOVEDEVICE_PARAMS ClassInstallParams,
            int ClassInstallParamsSize);
        
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceInstallParams(
            IntPtr hDevInfo,
            ref SP_DEVINFO_DATA DeviceInfoData,
            IntPtr DeviceInstallParams
        );
        
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceProperty(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA DeviceInfoData,
            [In] ref DEVPROPKEY propertyKey,
            [Out] out UInt32 propertyType,
            IntPtr propertyBuffer,
            UInt32 propertyBufferSize,
            out UInt32 requiredSize,
            UInt32 flags);
        
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            [In] ref SP_DEVINFO_DATA  DeviceInfoData,
            UInt32 Property,
            [Out] out UInt32  PropertyRegDataType,
            IntPtr PropertyBuffer,
            UInt32 PropertyBufferSize,
            [In,Out] ref UInt32 RequiredSize
        );
        
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiRestartDevices(
            IntPtr DeviceInfoSet,
            [In] ref SP_DEVINFO_DATA DeviceInfoData
        );

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiOpenDeviceInterface(
            [Out] IntPtr DeviceInfoSet,
            [In] string DevicePath,
            [In] uint OpenFlags,
            [In] [Out] ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData
        );

        #endregion

        #region Cfgmgr32

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Get_Device_ID(
            uint DevInst,
            IntPtr Buffer,
            uint BufferLen,
            uint Flags
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Locate_DevNode(
            ref uint pdnDevInst,
            string pDeviceID,
            CM_LOCATE_DEVNODE_FLAG ulFlags
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Locate_DevNode_Ex(
            out uint pdnDevInst,
            IntPtr pDeviceID,
            uint ulFlags,
            IntPtr hMachine
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Reenumerate_DevNode_Ex(
            uint dnDevInst,
            uint ulFlags,
            IntPtr hMachine
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Get_Device_ID_Size(
            ref uint pulLen,
            uint dnDevInst,
            uint ulFlags
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Get_DevNode_Property_Keys(
            uint devInst,
            [Out] DEVPROPKEY[] propertyKeyArray,
            ref uint propertyKeyCount,
            uint zeroFlags
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Get_DevNode_Property(
            uint devInst,
            ref DEVPROPKEY PropertyKey,
            out DevPropType PropertyType,
            IntPtr PropertyBuffer,
            ref uint PropertyBufferSize,
            uint ulFlags // reserved
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Set_DevNode_Property(
            uint devInst,
            ref DEVPROPKEY PropertyKey,
            DevPropType PropertyType,
            IntPtr PropertyBuffer,
            uint PropertyBufferSize,
            uint ulFlags // reserved
        );

        [DllImport("Cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern ConfigManagerResult CM_Get_Device_Interface_Property(
            string pszDeviceInterface,
            ref DEVPROPKEY PropertyKey,
            out DevPropType PropertyType,
            IntPtr PropertyBuffer,
            ref uint PropertyBufferSize,
            uint ulFlags // reserved
        );

        #endregion

        #region Newdev

        [DllImport("newdev.dll", SetLastError = true)]
        internal static extern bool DiInstallDevice(
            IntPtr hParent,
            IntPtr lpInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_DRVINFO_DATA DriverInfoData,
            DiFlags Flags,
            ref bool NeedReboot);

        [DllImport("newdev.dll", SetLastError = true)]
        internal static extern bool DiInstallDriver(
            IntPtr hwndParent,
            string FullInfPath,
            uint Flags,
            out bool NeedReboot);

        [DllImport("newdev.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool UpdateDriverForPlugAndPlayDevices(
            [In, Optional]  IntPtr hwndParent,
            [In] string HardwareId,
            [In] string FullInfPath,
            [In] uint InstallFlags,
            [Out] out bool bRebootRequired
        );

        #endregion

        #endregion
    }
}
