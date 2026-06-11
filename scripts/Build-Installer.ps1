param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$InnoCompilerPath
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $repoRoot "installer\Build-Installer.ps1"
& $buildScript `
    -Configuration $Configuration `
    -RuntimeIdentifier $RuntimeIdentifier `
    -InnoCompilerPath $InnoCompilerPath

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
