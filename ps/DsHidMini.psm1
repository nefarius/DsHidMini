Import-Module .\Invoke-Win32Api.psm1

Add-Type -TypeDefinition @"
namespace DS {
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.Win32.SafeHandles;
	
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
		//
		// Unknown connection type
		// 
		DS_CONNECTION_TYPE_UNKNOWN = 0x00,

		//
		// Connected via USB (wired)
		// 
		DS_CONNECTION_TYPE_USB = 0x01,

		//
		// Connected via Bluetooth (wireless)
		// 
		DS_CONNECTION_TYPE_BLUETOOTH = 0x02
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
		DS_FEATURE_TYPE_SET_DEVICE_CONFIG = 0xC6
	}
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct BD_ADDR
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=6)]
		public byte[] Address;
	}
	
	public enum DS_HID_DEVICE_MODE : uint
	{
		DS_HID_DEVICE_MODE_SINGLE,

		DS_HID_DEVICE_MODE_MULTI
	}
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct DS_DRIVER_CONFIGURATION
	{
		public DS_HID_DEVICE_MODE HidDeviceMode;

		public bool MuteDigitalPressureButtons;
	}
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct DS_FEATURE_HEADER
	{
		public byte ReportId;

		public UInt32 Size;
	}
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
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
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
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

	[StructLayout(LayoutKind.Sequential, Pack=1)]
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
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
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
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
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
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
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
	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
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

	public static class HID
	{
		[DllImport("hid.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
		public static extern bool HidD_GetFeature(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer, int reportBufferLength);
		
		[DllImport("hid.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
		public static extern bool HidD_SetFeature(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer, int reportBufferLength);
	}

	public static class DsUtil
	{
		public static byte [] StructureToByteArray(object obj)
		{
			int len = Marshal.SizeOf(obj);

			byte [] arr = new byte[len];

			IntPtr ptr = Marshal.AllocHGlobal(len);

			Marshal.StructureToPtr(obj, ptr, true);

			Marshal.Copy(ptr, arr, 0, len);

			Marshal.FreeHGlobal(ptr);

			return arr;
		}

		public static void ByteArrayToStructure(byte [] bytearray, ref object obj)
		{
			int len = Marshal.SizeOf(obj);

			IntPtr i = Marshal.AllocHGlobal(len);

			Marshal.Copy(bytearray,0, i,len);

			obj = Marshal.PtrToStructure(i, obj.GetType());

			Marshal.FreeHGlobal(i);
		}
	}
}
"@;

function Invoke-CreateFileW([string]$devicePath)
{
	$handle = Invoke-Win32Api -DllName kernel32.dll `
		-MethodName CreateFileW `
		-ReturnType Microsoft.Win32.SafeHandles.SafeFileHandle `
		-ParameterTypes @([String], [System.Security.AccessControl.FileSystemRights], [System.IO.FileShare], [IntPtr], [System.IO.FileMode], [UInt32], [IntPtr]) `
		-Parameters @(
			$devicePath,
			([System.Security.AccessControl.FileSystemRights]::Read, [System.Security.AccessControl.FileSystemRights]::Write),
			[System.IO.FileShare]::ReadWrite,
			[IntPtr]::Zero,
			[System.IO.FileMode]::Open,
			0,
			[IntPtr]::Zero) `
		-SetLastError $true `
		-CharSet Unicode
		
	if ($handle.IsInvalid) {
		$last_err = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
		throw [System.ComponentModel.Win32Exception]$last_err
	}
	
	return $handle
}

function Get-HidFeature([Microsoft.Win32.SafeHandles.SafeFileHandle]$handle, [byte[]]$buffer, [UInt32]$length)
{
	return [DS.HID]::HidD_GetFeature($handle, $buffer, $length)
}

function Set-HidFeature([Microsoft.Win32.SafeHandles.SafeFileHandle]$handle, [byte[]]$buffer, [UInt32]$length)
{
	return Invoke-Win32Api -DllName hid.dll `
		-MethodName HidD_SetFeature `
		-ReturnType System.Boolean `
		-ParameterTypes @([Microsoft.Win32.SafeHandles.SafeFileHandle], [byte[]], [UInt32]) `
		-Parameters @(
			$handle,
			$buffer,
			$length) `
		-SetLastError $true `
		-CharSet Unicode
}
