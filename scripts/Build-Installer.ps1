param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "DiscordVideoCompressor\bin\$Configuration\net8.0-windows\$RuntimeIdentifier\publish"
$issPath = Join-Path $repoRoot "installer\DiscordVideoCompressor.iss"

dotnet publish (Join-Path $repoRoot "DiscordVideoCompressor\DiscordVideoCompressor.csproj") `
  -c $Configuration `
  -r $RuntimeIdentifier `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true

$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    throw "Inno Setup compiler not found at '$iscc'. Install Inno Setup 6 first."
}

& $iscc "/DMyAppVersion=$Version" "/DPublishDir=$publishDir" $issPath
