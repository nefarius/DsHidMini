Import-Module .\Invoke-Win32Api.psm1
Import-Module .\PSReflect.psm1

$devicePath = "\\?\hid#vid_054c&pid_0268#9&2d65d5ae&5&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}"

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

$buffer = [System.Runtime.InteropServices.Marshal]::AllocHGlobal(11)
[System.Runtime.InteropServices.Marshal]::WriteByte($buffer, 0, 192)

function Get-HidFeature([Microsoft.Win32.SafeHandles.SafeFileHandle]$handle, [IntPtr]$buffer, [UInt32]$length)
{
	return Invoke-Win32Api -DllName hid.dll `
		-MethodName HidD_GetFeature `
		-ReturnType System.Boolean `
		-ParameterTypes @([Microsoft.Win32.SafeHandles.SafeFileHandle], [IntPtr], [UInt32]) `
		-Parameters @(
			$handle,
			$buffer,
			$length) `
		-SetLastError $true `
		-CharSet Unicode
}

function Set-HidFeature([Microsoft.Win32.SafeHandles.SafeFileHandle]$handle, [IntPtr]$buffer, [UInt32]$length)
{
	return Invoke-Win32Api -DllName hid.dll `
		-MethodName HidD_SetFeature `
		-ReturnType System.Boolean `
		-ParameterTypes @([Microsoft.Win32.SafeHandles.SafeFileHandle], [IntPtr], [UInt32]) `
		-Parameters @(
			$handle,
			$buffer,
			$length) `
		-SetLastError $true `
		-CharSet Unicode
}

$mod = New-InMemoryModule

$DsConnectionType = psenum $mod DS.DS_CONNECTION_TYPE UInt32 @{
	DS_CONNECTION_TYPE_UNKNOWN = 	0x00
	DS_CONNECTION_TYPE_USB =		0x01
	DS_CONNECTION_TYPE_BLUETOOTH = 	0x02
}

$GetConnectionType = struct $mod DS.DS_FEATURE_GET_CONNECTION_TYPE @{
	ReportId =			field 0 Byte
	Size =				field 1 UInt32
	ConnectionType = 	field 2 $DsConnectionType
}

$test = [System.Activator]::CreateInstance($GetConnectionType)
[System.Runtime.InteropServices.Marshal]::SizeOf([Type]$DsConnectionType)
$test.Size = [System.Runtime.InteropServices.Marshal]::SizeOf([Type]$GetConnectionType)


$handle.Close()