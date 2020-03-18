# Copyright: (c) 2018, Jordan Borean (@jborean93) <jborean93@gmail.com>
# MIT License (see LICENSE or https://opensource.org/licenses/MIT)


Function Invoke-Win32Api {
    <#
    .SYNOPSIS
    Call a native Win32 API or a function exported in a DLL.

    .DESCRIPTION
    This method allows you to call a native Win32 API or DLL function
    without compiling C# code using Add-Type. The advantages of this over
    using Add-Type is that this is all generated in memory and no temporary
    files are created.

    The code has been created with great help from various sources. The main
    sources I used were;
    # http://www.leeholmes.com/blog/2007/10/02/managing-ini-files-with-powershell/
    # https://blogs.technet.microsoft.com/heyscriptingguy/2013/06/27/use-powershell-to-interact-with-the-windows-api-part-3/

    .PARAMETER DllName
    [String] The DLL to import the method from.

    .PARAMETER MethodName
    [String] The name of the method.

    .PARAMETER ReturnType
    [Type] The type of the return object returned by the method.

    .PARAMETER ParameterTypes
    [Type[]] Array of types that define the parameter types required by the
    method. The type index should match the index of the value in the
    Parameters parameter.

    If the parameter is a reference or an out parameter, use [Ref] as the type
    for that parameter.

    .PARAMETER Parameters
    [Object[]] Array of objects to supply for the parameter values required by
    the method. The value index should match the index of the value in the
    ParameterTypes parameter.

    If the parameter is a reference or an out parameter, the object should be a
    [Ref] of the parameter.

    .PARAMETER SetLastError
    [Bool] Whether to apply the SetLastError Dll attribute on the method,
    default is $false

    .PARAMETER CharSet
    [Runtime.InteropServices.CharSet] The charset to apply to the CharSet Dll
    attribute on the method, default is [Runtime.InteropServices.CharSet]::Auto

    .OUTPUTS
    [Object] The return result from the method, the type of this value is based
    on the ReturnType parameter.

    .EXAMPLE
    # Use the Win32 APIs to open a file handle
    $handle = Invoke-Win32Api -DllName kernel32.dll `
        -MethodName CreateFileW `
        -ReturnType Microsoft.Win32.SafeHandles.SafeFileHandle `
        -ParameterTypes @([String], [System.Security.AccessControl.FileSystemRights], [System.IO.FileShare], [IntPtr], [System.IO.FileMode], [UInt32], [IntPtr]) `
        -Parameters @(
            "\\?\C:\temp\test.txt",
            [System.Security.AccessControl.FileSystemRights]::FullControl,
            [System.IO.FileShare]::ReadWrite,
            [IntPtr]::Zero,
            [System.IO.FileMode]::OpenOrCreate,
            0,
            [IntPtr]::Zero) `
        -SetLastError $true `
        -CharSet Unicode
    if ($handle.IsInvalid) {
        $last_err = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
        throw [System.ComponentModel.Win32Exception]$last_err
    }
    $handle.Close()

    # Lookup the account name from a SID
    $sid_string = "S-1-5-18"
    $sid = New-Object -TypeName System.Security.Principal.SecurityIdentifier -ArgumentList $sid_string
    $sid_bytes = New-Object -TypeName byte[] -ArgumentList $sid.BinaryLength
    $sid.GetBinaryForm($sid_bytes, 0)

    $name = New-Object -TypeName System.Text.StringBuilder
    $name_length = 0
    $domain_name = New-Object -TypeName System.Text.StringBuilder
    $domain_name_length = 0

    $invoke_args = @{
        DllName = "Advapi32.dll"
        MethodName = "LookupAccountSidW"
        ReturnType = [bool]
        ParameterTypes = @([String], [byte[]], [System.Text.StringBuilder], [Ref], [System.Text.StringBuilder], [Ref], [Ref])
        Parameters = @(
            $null,
            $sid_bytes,
            $name,
            [Ref]$name_length,
            $domain_name,
            [Ref]$domain_name_length,
            [Ref][IntPtr]::Zero
        )
        SetLastError = $true
        CharSet = "Unicode"
    }

    $res = Invoke-Win32Api @invoke_args
    $name.EnsureCapacity($name_length)
    $domain_name.EnsureCapacity($domain_name_length)
    $res = Invoke-Win32Api @invoke_args
    if (-not $res) {
        $last_err = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
        throw [System.ComponentModel.Win32Exception]$last_err
    }
    Write-Output "SID: $sid_string, Domain: $($domain_name.ToString()), Name: $($name.ToString())"

    .NOTES
    The parameters to use for a method dynamically based on the method that is
    called. There is no cut and fast way to automatically convert the interface
    listed on the Microsoft docs. There are great resources to help you create
    the "P/Invoke" definition like pinvoke.net.
    #>
    [CmdletBinding()]
    [OutputType([Object])]
    param(
        [Parameter(Position = 0, Mandatory = $true)] [String]$DllName,
        [Parameter(Position = 1, Mandatory = $true)] [String]$MethodName,
        [Parameter(Position = 2, Mandatory = $true)] [Type]$ReturnType,
        [Parameter(Position = 3)] [Type[]]$ParameterTypes = [Type[]]@(),
        [Parameter(Position = 4)] [Object[]]$Parameters = [Object[]]@(),
        [Parameter()] [Bool]$SetLastError = $false,
        [Parameter()] [Runtime.InteropServices.CharSet]$CharSet = [Runtime.InteropServices.CharSet]::Auto
    )
    if ($ParameterTypes.Length -ne $Parameters.Length) {
        throw [System.ArgumentException]"ParameterType Count $($ParameterTypes.Length) not equal to Parameter Count $($Parameters.Length)"
    }

    # First step is to define the dynamic assembly in the current AppDomain
    $assembly = New-Object -TypeName System.Reflection.AssemblyName -ArgumentList "Win32ApiAssembly"
    $AssemblyBuilder = [System.Reflection.Assembly].Assembly.GetTypes() | Where-Object { $_.Name -eq 'AssemblyBuilder' }
    $dynamic_assembly = $AssemblyBuilder::DefineDynamicAssembly($assembly, [Reflection.Emit.AssemblyBuilderAccess]::Run)


    # Second step is to create the dynamic module and type/class that contains
    # the P/Invoke definition
    $dynamic_module = $dynamic_assembly.DefineDynamicModule("Win32Module", $false)
    $dynamic_type = $dynamic_module.DefineType("Win32Type", [Reflection.TypeAttributes]"Public, Class")

    # Need to manually get the reference type if the ParameterType is [Ref], we
    # define this based on the Parameter type at the same index
    $parameter_types = $ParameterTypes.Clone()
    for ($i = 0; $i -lt $ParameterTypes.Length; $i++) {
        if ($ParameterTypes[$i] -eq [Ref]) {
            $parameter_types[$i] = $Parameters[$i].Value.GetType().MakeByRefType()
        }
    }

    # Next, the method is created where we specify the name, parameters and
    # return type that is expected
    $dynamic_method = $dynamic_type.DefineMethod(
        $MethodName,
        [Reflection.MethodAttributes]"Public, Static",
        $ReturnType,
        $parameter_types
    )

    # Build the attributes (DllImport) part of the method where the DLL
    # SetLastError and CharSet are applied
    $constructor = [Runtime.InteropServices.DllImportAttribute].GetConstructor([String])
    $method_fields = [Reflection.FieldInfo[]]@(
        [Runtime.InteropServices.DllImportAttribute].GetField("SetLastError"),
        [Runtime.InteropServices.DllImportAttribute].GetField("CharSet")
    )
    $method_fields_values = [Object[]]@($SetLastError, $CharSet)
    $custom_attributes = New-Object -TypeName Reflection.Emit.CustomAttributeBuilder -ArgumentList @(
        $constructor,
        $DllName,
        $method_fields,
        $method_fields_values
    )
    $dynamic_method.SetCustomAttribute($custom_attributes)

    # Create the custom type/class based on what was configured above
    $win32_type = $dynamic_type.CreateType()

    # Invoke the method with the parameters supplied and return the result
    $result = $win32_type::$MethodName.Invoke($Parameters)
    return $result
}