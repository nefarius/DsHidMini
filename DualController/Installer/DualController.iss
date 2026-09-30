#define ProductVersion "0.1.0"

[Setup]
AppId={{134C5FB3-28AB-4336-A640-5661C826CE25}
AppName=Dual Controller (PS3 + PS4) — Experimental
AppVersion={#ProductVersion}
AppPublisher=headd16 community fork
AppPublisherURL=https://github.com/headd16/DsHidMini
DefaultDirName={autopf}\DualController
DefaultGroupName=Dual Controller
OutputDir=..\artifacts\installer
OutputBaseFilename=DualController-Setup-{#ProductVersion}-x64
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\THIRD-PARTY-NOTICES.txt
InfoBeforeFile=..\INSTALL.txt
UninstallDisplayIcon={app}\DualController.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Types]
Name: "full"; Description: "PS3 + PS4, USB and Bluetooth"
Name: "ps4"; Description: "PS4 bridge only (USB and Bluetooth)"
Name: "custom"; Description: "Choose components"; Flags: iscustom

[Components]
Name: "bridge"; Description: "PS4 Xbox input bridge and signed ViGEmBus driver"; Types: full ps4 custom; Flags: fixed
Name: "ps3"; Description: "PS3: signed DsHidMini driver, ControlApp and .NET 10 Desktop Runtime"; Types: full
Name: "ps3\bluetooth"; Description: "PS3 Bluetooth: signed BthPS3 drivers"; Types: full

[Tasks]
Name: "startup"; Description: "Start the PS4 bridge when I sign in to Windows"; Flags: unchecked
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\artifacts\app\DualController.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\INSTALL.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\*-LICENSE.txt"; DestDir: "{app}\licenses"; Flags: ignoreversion
Source: "..\DOTNET-THIRD-PARTY-NOTICES.txt"; DestDir: "{app}\licenses"; Flags: ignoreversion
Source: "..\artifacts\dependencies\verified-dependencies.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\artifacts\dependencies\ViGEmBus.exe"; Flags: dontcopy
Source: "..\artifacts\dependencies\DesktopRuntime.exe"; Flags: dontcopy
Source: "..\artifacts\dependencies\DsHidMini.msi"; Flags: dontcopy
Source: "..\artifacts\dependencies\BthPS3.msi"; Flags: dontcopy

[Icons]
Name: "{group}\Dual Controller"; Filename: "{app}\DualController.exe"
Name: "{group}\Controller instructions"; Filename: "{app}\INSTALL.txt"
Name: "{group}\Uninstall Dual Controller"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Dual Controller"; Filename: "{app}\DualController.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DualController"; ValueData: """{app}\DualController.exe"" --tray"; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\DualController.exe"; Description: "Open Dual Controller"; Flags: nowait postinstall skipifsilent runasoriginaluser
Filename: "{app}\INSTALL.txt"; Description: "Read PS3 and PS4 connection steps"; Flags: shellexec postinstall skipifsilent runasoriginaluser

[Code]
var
  DependencyRestart: Boolean;

function RunDependency(Name, Parameters: String; Msi: Boolean): String;
var
  ProgramPath, Arguments: String;
  ExitCode: Integer;
begin
  Result := '';
  ExtractTemporaryFile(Name);
  if Msi then begin
    ProgramPath := ExpandConstant('{sys}\msiexec.exe');
    Arguments := '/i "' + ExpandConstant('{tmp}\' + Name) + '" /passive /norestart /l*v "' + ExpandConstant('{tmp}\' + Name + '.log') + '"';
  end else begin
    ProgramPath := ExpandConstant('{tmp}\' + Name);
    Arguments := Parameters;
  end;
  WizardForm.StatusLabel.Caption := 'Installing ' + Name + '...';
  Log('Starting dependency: ' + Name);
  if not Exec(ProgramPath, Arguments, '', SW_SHOW, ewWaitUntilTerminated, ExitCode) then
    Result := 'Could not start ' + Name + '. ' + SysErrorMessage(ExitCode)
  else if (ExitCode = 3010) or (ExitCode = 1641) then DependencyRestart := True
  else if ExitCode <> 0 then
    Result := Name + ' failed with exit code ' + IntToStr(ExitCode) + '. Review the installer log in your TEMP folder before retrying.';
  if Result <> '' then Log(Result);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if WizardIsComponentSelected('ps3') then begin
    Result := RunDependency('DesktopRuntime.exe', '/install /quiet /norestart', False);
    if Result <> '' then Exit;
    Result := RunDependency('DsHidMini.msi', '', True);
    if Result <> '' then Exit;
    if WizardIsComponentSelected('ps3\bluetooth') then begin
      Result := RunDependency('BthPS3.msi', '', True);
      if Result <> '' then Exit;
    end;
  end;
  Result := RunDependency('ViGEmBus.exe', '/install /quiet /norestart', False);
end;

function NeedRestart(): Boolean;
begin
  Result := DependencyRestart;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    MsgBox('Dual Controller has been removed. Shared DsHidMini, BthPS3, ViGEmBus and .NET components remain installed. Remove them individually from Windows Apps if you no longer use them.', mbInformation, MB_OK);
end;
