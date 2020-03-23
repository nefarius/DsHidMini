Import-Module .\DsHidMini.psm1

$devicePath = ((Get-HidDevice)[0]).DeviceId

$handle = Invoke-CreateFileW $devicePath

Get-DsFeatureConnectionType $handle
Get-DsFeatureHostAddress $handle
Get-DsFeatureDeviceAddress $handle

$handle.Close()
