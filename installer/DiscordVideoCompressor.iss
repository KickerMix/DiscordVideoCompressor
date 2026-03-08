#define MyAppName "Discord Video Compressor"
#define MyAppExeName "DiscordVideoCompressor.exe"
#ifndef MyAppVersion
  #define MyAppVersion "1.1.4"
#endif
#ifndef PublishDir
  #define PublishDir "..\DiscordVideoCompressor\bin\Release\net8.0-windows\win-x64\publish"
#endif

[Setup]
AppId={{E8641623-6F39-44EA-AF0A-1C566AF5A97D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=KickerMix
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=DiscordVideoCompressor-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
english.AdditionalTasks=Additional tasks:
russian.AdditionalTasks=Дополнительные задачи:
english.AddExplorerContextTask=Add Explorer context menu integration
russian.AddExplorerContextTask=Добавить интеграцию в контекстное меню Проводника
english.ConvertContextMenuTitle=Convert to Discord Size Limit
russian.ConvertContextMenuTitle=Конвертировать под лимит Discord

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "explorercontext"; Description: "{cm:AddExplorerContextTask}"; GroupDescription: "{cm:AdditionalTasks}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCR; Subkey: "SystemFileAssociations\.mp4\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueData: "{cm:ConvertContextMenuTitle}"; Flags: uninsdeletekey; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.mp4\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName}"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.mp4\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.mp4\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --convert ""%1"""; Tasks: explorercontext

Root: HKCR; Subkey: "SystemFileAssociations\.avi\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueData: "{cm:ConvertContextMenuTitle}"; Flags: uninsdeletekey; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.avi\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName}"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.avi\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.avi\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --convert ""%1"""; Tasks: explorercontext

Root: HKCR; Subkey: "SystemFileAssociations\.mkv\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueData: "{cm:ConvertContextMenuTitle}"; Flags: uninsdeletekey; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.mkv\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName}"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.mkv\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.mkv\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --convert ""%1"""; Tasks: explorercontext

Root: HKCR; Subkey: "SystemFileAssociations\.webm\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueData: "{cm:ConvertContextMenuTitle}"; Flags: uninsdeletekey; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.webm\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName}"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.webm\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"; Tasks: explorercontext
Root: HKCR; Subkey: "SystemFileAssociations\.webm\shell\DiscordVideoCompressor.ConvertToDiscordSizeLimit\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --convert ""%1"""; Tasks: explorercontext

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

