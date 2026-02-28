param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$ProjectPath = "..\DiscordVideoCompressor\DiscordVideoCompressor.csproj",
    [string]$PublishProfile = "..\DiscordVideoCompressor\Properties\PublishProfiles\Win64SingleFile.pubxml",
    [string]$InnoScriptPath = ".\DiscordVideoCompressor.iss",
    [string]$InnoCompilerPath
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $ProjectPath))
$publishProfileFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $PublishProfile))
$innoScriptFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $InnoScriptPath))

[xml]$projectXml = Get-Content -Path $projectFullPath
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Version property not found in $projectFullPath"
}

Write-Host "Publishing version $version..."
dotnet publish $projectFullPath -c $Configuration -r $RuntimeIdentifier /p:PublishProfile=$publishProfileFullPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$resolvedInnoCompilerPath = $InnoCompilerPath
if ([string]::IsNullOrWhiteSpace($resolvedInnoCompilerPath)) {
    $inPath = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
    if ($inPath) {
        $resolvedInnoCompilerPath = $inPath.Source
    }
}

if ([string]::IsNullOrWhiteSpace($resolvedInnoCompilerPath)) {
    $defaultCandidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $defaultCandidates) {
        if (Test-Path $candidate) {
            $resolvedInnoCompilerPath = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($resolvedInnoCompilerPath)) {
    throw "Inno Setup Compiler (ISCC.exe) was not found. Install Inno Setup 6 or pass -InnoCompilerPath."
}

Write-Host "Building installer..."
& $resolvedInnoCompilerPath "/DMyAppVersion=$version" $innoScriptFullPath
if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe failed with exit code $LASTEXITCODE"
}

Write-Host "Installer created in $scriptRoot\output"
