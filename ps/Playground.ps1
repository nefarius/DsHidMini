
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



$handle.Close()