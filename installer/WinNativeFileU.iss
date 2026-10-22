; Original per-user installer. Payload is produced and hashed by scripts/package.ps1.
#ifndef PayloadDir
  #error PayloadDir is required
#endif
#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef NumericVersion
  #error NumericVersion is required
#endif
#ifdef TestInstall
  #define ProductId "WinNativeFileU.LocalInstallerTest"
  #define ProductName "WinNativeFileU installer test"
#else
  #define ProductId "WinNativeFileU"
  #define ProductName "WinNativeFileU"
#endif

[Setup]
AppId={#ProductId}
AppName={#ProductName}
AppVersion={#AppVersion}
AppPublisher=its-tony
VersionInfoVersion={#NumericVersion}
VersionInfoDescription=WinNativeFileU per-user installer
DefaultDirName={localappdata}\Programs\WinNativeFileU
DefaultGroupName=WinNativeFileU
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
SetupArchitecture=x64
MinVersion=10.0.22000
WizardStyle=modern
DisableDirPage=no
DisableProgramGroupPage=yes
AllowNoIcons=yes
UsePreviousAppDir=no
UsePreviousTasks=no
DirExistsWarning=no
CloseApplications=no
RestartApplications=no
UninstallDisplayIcon={app}\WinNativeFileU.App.exe
LicenseFile={#PayloadDir}\DISTRIBUTION-TERMS.txt
OutputBaseFilename=WinNativeFileU-{#AppVersion}-win-x64-setup
Compression=lzma2/fast
SolidCompression=yes
LZMANumBlockThreads=2
SetupLogging=yes

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
#ifndef TestInstall
Name: "{autoprograms}\WinNativeFileU"; Filename: "{app}\WinNativeFileU.App.exe"; WorkingDir: "{app}"
#endif

[Code]
function Running: Boolean;
begin
  Result := CheckForMutexes('Local\WinNativeFileU.Running');
end;

function InitializeSetup: Boolean;
begin
  Result := not Running;
  if not Result then begin
    Log('WinNativeFileU is running; installation was refused without closing it.');
    if not WizardSilent then
      MsgBox('Exit WinNativeFileU from its notification icon before installing.', mbError, MB_OK);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Search: TFindRec;
  More: Boolean;
begin
  Result := '';
  if Running then begin
    Result := 'Exit WinNativeFileU before installing.';
    exit;
  end;
  if RegKeyExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#ProductId}_is1') then begin
    Result := 'Uninstall the existing WinNativeFileU version before installing this beta. Settings and converted files are preserved.';
    exit;
  end;
  if FindFirst(AddBackslash(ExpandConstant('{app}')) + '*', Search) then begin
    try
      More := True;
      while More do begin
        if (Search.Name <> '.') and (Search.Name <> '..') then begin
          Result := 'Choose an empty folder. This beta preserves existing files and does not upgrade in place. Uninstall the previous version first; settings and converted files remain.';
          exit;
        end;
        More := FindNext(Search);
      end;
    finally
      FindClose(Search);
    end;
  end;
end;

function InitializeUninstall: Boolean;
begin
  Result := not Running;
  if not Result then begin
    Log('WinNativeFileU is running; uninstall was refused without closing it.');
    if not UninstallSilent then
      MsgBox('Exit WinNativeFileU before uninstalling.', mbError, MB_OK);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Current, Expected: String;
begin
  if CurUninstallStep = usUninstall then begin
    Expected := '"' + ExpandConstant('{app}\WinNativeFileU.App.exe') + '" --resident --resident-minimized';
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'WinNativeFileU', Current) then
      if Current = Expected then begin
        if not RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'WinNativeFileU') then
          Log('The matching sign-in value could not be removed.');
      end else
        Log('Different sign-in value preserved.');
  end;
end;
