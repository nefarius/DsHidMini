$signTool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
$timestampUrl = "http://timestamp.digicert.com"
$certName = "Nefarius Software Solutions e.U."

# List of files to sign
$files =    "`".\setup\*.msi`" "

# sign with only one certificate
Invoke-Expression "& `"$signTool`" sign /v /n `"$certName`" /tr $timestampUrl /fd sha256 /td sha256 $files"
