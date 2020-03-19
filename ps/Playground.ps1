Import-Module .\DsHidMini.psm1

$devicePath = "\\?\hid#vid_054c&pid_0268#9&2d65d5ae&7&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}"
#$devicePath = "\\?\hid#vid_054c&pid_0268#9&2d65d5ae&5&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}"

$handle = Invoke-CreateFileW $devicePath

Get-DsFeatureConnectionType $handle
Get-DsFeatureHostAddress $handle
Get-DsFeatureDeviceAddress $handle

$handle.Close()
