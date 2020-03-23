using System;
using System.Runtime.InteropServices;

namespace DsHidMiniModule.DS
{
	public enum DS_DEVICE_TYPE : uint
	{
		//
		// Unknown device type
		// 
		DS_DEVICE_TYPE_UNKNOWN = 0x00,

		//
		// Sony DualShock 3 Controller
		// 
		DS_DEVICE_TYPE_PS3_DUALSHOCK,

		//
		// Sony DualShock 4 Controller
		// 
		DS_DEVICE_TYPE_PS4_DUALSHOCK,

		//
		// Sony Navigation Controller
		// 
		DS_DEVICE_TYPE_PS3_NAVIGATION,

		//
		// Sony Motion Controller
		// 
		DS_DEVICE_TYPE_PS3_MOTION
	}

	public enum DS_CONNECTION_TYPE : uint
	{
		DsDeviceConnectionTypeUnknown = 0x00,
		DsDeviceConnectionTypeUsb,
		DsDeviceConnectionTypeBth
	}

	public enum DS_FEATURE_TYPE : uint
	{
		//
		// Receive controller-embedded Bluetooth host address
		// 
		DS_FEATURE_TYPE_GET_HOST_BD_ADDR = 0xC0,

		//
		// Update controller-embedded Bluetooth host address
		// 
		DS_FEATURE_TYPE_SET_HOST_BD_ADDR = 0xC1,

		//
		// Receive controller-embedded Bluetooth address
		// 
		DS_FEATURE_TYPE_GET_DEVICE_BD_ADDR = 0xC2,

		//
		// Receive device type (DS3, DS4, ...)
		// 
		DS_FEATURE_TYPE_GET_DEVICE_TYPE = 0xC3,

		//
		// Receive device connection type (USB, Bluetooth)
		// 
		DS_FEATURE_TYPE_GET_CONNECTION_TYPE = 0xC4,

		//
		// Receive current volatile configuration properties
		// 
		DS_FEATURE_TYPE_GET_DEVICE_CONFIG = 0xC5,

		//
		// Update current volatile configuration properties
		// 
		DS_FEATURE_TYPE_SET_DEVICE_CONFIG = 0xC6,

		//
		// Receive current battery status
		// 
		DS_FEATURE_TYPE_GET_BATTERY_STATUS = 0xC7
	}

	public enum DS_BATTERY_STATUS : uint
	{
		DsBatteryStatusNone = 0x00,
		DsBatteryStatusDying = 0x01,
		DsBatteryStatusLow = 0x02,
		DsBatteryStatusMedium = 0x03,
		DsBatteryStatusHigh = 0x04,
		DsBatteryStatusFull = 0x05,
		DsBatteryStatusCharging = 0xEE,
		DsBatteryStatusCharged = 0xEF
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BD_ADDR
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
		public byte[] Address;
	}

	public enum DS_HID_DEVICE_MODE : uint
	{
		DsHidMiniDeviceModeUnknown = 0x00,
		DsHidMiniDeviceModeSingle,
		DsHidMiniDeviceModeMulti,
		DsHidMiniDeviceModeSixaxisCompatible
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_DRIVER_CONFIGURATION
	{
		public DS_HID_DEVICE_MODE HidDeviceMode;

		public bool MuteDigitalPressureButtons;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_HEADER
	{
		public byte ReportId;

		public UInt32 Size;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_GET_HOST_BD_ADDR
	{
		public DS_FEATURE_HEADER Header;

		public BD_ADDR HostAddress;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_GET_HOST_BD_ADDR;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_SET_HOST_BD_ADDR
	{
		public DS_FEATURE_HEADER Header;

		public BD_ADDR HostAddress;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_SET_HOST_BD_ADDR;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_GET_DEVICE_BD_ADDR
	{
		public DS_FEATURE_HEADER Header;

		public BD_ADDR DeviceAddress;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_GET_DEVICE_BD_ADDR;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_GET_DEVICE_TYPE
	{
		public DS_FEATURE_HEADER Header;

		public DS_DEVICE_TYPE DeviceType;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_GET_DEVICE_TYPE;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_GET_CONNECTION_TYPE
	{
		public DS_FEATURE_HEADER Header;

		public DS_CONNECTION_TYPE ConnectionType;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_GET_CONNECTION_TYPE;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_GET_DEVICE_CONFIG
	{
		public DS_FEATURE_HEADER Header;

		public DS_DRIVER_CONFIGURATION Configuration;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_GET_DEVICE_CONFIG;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_SET_DEVICE_CONFIG
	{
		public DS_FEATURE_HEADER Header;

		public DS_DRIVER_CONFIGURATION Configuration;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_SET_DEVICE_CONFIG;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DS_FEATURE_GET_BATTERY_STATUS
	{
		public DS_FEATURE_HEADER Header;

		public DS_BATTERY_STATUS BatteryStatus;

		public void Init()
		{
			this.Header.Size = (UInt32)Marshal.SizeOf(this.GetType());
			this.Header.ReportId = (byte)DS_FEATURE_TYPE.DS_FEATURE_TYPE_GET_BATTERY_STATUS;
		}
	}
}
