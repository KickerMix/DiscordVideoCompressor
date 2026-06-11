param(
    [string]$OutputDirectory = ".\appcast",
    [string]$FfmpegCommit = "e4c7fbf6c0a922297d4df7288d7aea665af15e24",
    [string]$BuildRecipesCommit = "a9410e4be2b332e535000004a8ebf304d9b46689"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$outputDirectoryFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $OutputDirectory))
$workingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("DiscordVideoCompressor-sources-" + [guid]::NewGuid().ToString("N"))
$bundleDirectory = Join-Path $workingDirectory "ffmpeg-corresponding-source"
$bundlePath = Join-Path $outputDirectoryFullPath "ffmpeg-corresponding-source.zip"

try {
    New-Item -ItemType Directory -Path $bundleDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $outputDirectoryFullPath -Force | Out-Null

    Invoke-WebRequest -UseBasicParsing `
        -Uri "https://github.com/FFmpeg/FFmpeg/archive/$FfmpegCommit.zip" `
        -OutFile (Join-Path $bundleDirectory "ffmpeg-$FfmpegCommit.zip")

    Invoke-WebRequest -UseBasicParsing `
        -Uri "https://github.com/BtbN/FFmpeg-Builds/archive/$BuildRecipesCommit.zip" `
        -OutFile (Join-Path $bundleDirectory "ffmpeg-build-recipes-$BuildRecipesCommit.zip")

    Copy-Item -LiteralPath (Join-Path $repoRoot "THIRD-PARTY-NOTICES.md") -Destination $bundleDirectory
    Copy-Item -LiteralPath (Join-Path $repoRoot "FFMPEG-GPL-LICENSE.txt") -Destination $bundleDirectory

    if (Test-Path $bundlePath) {
        Remove-Item -LiteralPath $bundlePath -Force
    }

    Compress-Archive -Path (Join-Path $bundleDirectory "*") -DestinationPath $bundlePath -CompressionLevel Optimal
    Write-Host "Created FFmpeg corresponding source bundle: $bundlePath"
}
finally {
    if (Test-Path $workingDirectory) {
        Remove-Item -LiteralPath $workingDirectory -Recurse -Force
    }
}
