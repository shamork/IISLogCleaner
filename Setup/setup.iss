; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "IISLogCleaner"
#define MyAppVersion "1.0"
#define MyAppPublisher "Shamork"
#define MyAppExeName "IISLogCleaner.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{78929A3E-A040-4C09-A8C0-E603686B2651}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf}\{#MyAppName}
DisableDirPage=false
;DefaultGroupName=IIS Log Cleaner
;DisableProgramGroupPage=yes
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\IISLogCleaner\bin\Release\IISLogCleaner.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\IISLogCleaner\bin\Release\IISLogCleaner.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\IISLogCleaner\bin\Release\IISLogCleaner.pdb"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Run]
Filename: {sys}\sc.exe; Parameters: "create {#MyAppName} start= auto binPath= ""{app}\IISLogCleaner.exe""" ; Flags: runhidden
Filename: {sys}\sc.exe; Parameters: "start {#MyAppName}" ; Flags: runhidden

[UninstallRun]
Filename: {sys}\sc.exe; Parameters: "stop {#MyAppName}" ; Flags: runhidden
Filename: {sys}\sc.exe; Parameters: "delete {#MyAppName}" ; Flags: runhidden

