; Inno Setup script for Clarion Debugger
; Builds a per-user installer for the self-contained single-file build in .\portable\

#define AppName "Clarion Debugger"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppExe "ClarionDbg.exe"
#define AppPublisher "Roberto Renz"
#define AppUrl "https://github.com/robertorenz/Clariondebugger1"

[Setup]
AppId={{B3F1C2A4-7D58-4E9B-A6C1-9F0D2E3A4B5C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
DefaultDirName={autopf}\ClarionDebugger
DefaultGroupName=Clarion Debugger
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=output
OutputBaseFilename=ClarionDebuggerSetup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x86 x64
PrivilegesRequired=lowest
DisableProgramGroupPage=yes

[Files]
Source: "portable\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Clarion Debugger"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall Clarion Debugger"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Clarion Debugger"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch Clarion Debugger"; Flags: nowait postinstall skipifsilent
