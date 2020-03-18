Import-Module .\DsHidMini.psm1

$devicePath = "\\?\hid#vid_054c&pid_0268#9&2d65d5ae&7&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}"
#$devicePath = "\\?\hid#vid_054c&pid_0268#9&2d65d5ae&5&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}"

$handle = Invoke-CreateFileW $devicePath

$getConnectionType = [System.Activator]::CreateInstance([DS.DS_FEATURE_GET_CONNECTION_TYPE])
$getConnectionType.Init()
#$buffer = [System.Runtime.InteropServices.Marshal]::AllocHGlobal($getConnectionType.Header.Size)
#[System.Runtime.InteropServices.Marshal]::StructureToPtr($getConnectionType, $buffer, $false)

$payload = [DS.DsUtil]::StructureToByteArray($getConnectionType)

Get-HidFeature $handle ([ref]$payload) $getConnectionType.Header.Size

[DS.DsUtil]::ByteArrayToStructure($payload, [ref] $getConnectionType)

#$getConnectionType = [System.Runtime.InteropServices.Marshal]::PtrToStructure($buffer, [Type][DS.DS_FEATURE_GET_CONNECTION_TYPE])
$getConnectionType

#[System.Runtime.InteropServices.Marshal]::FreeHGlobal($buffer)

$handle.Close()
