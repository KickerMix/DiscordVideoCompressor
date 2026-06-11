# Discord Video Compressor

[Русская версия](./README_RU.md)

Windows desktop application that compresses videos to a selected file-size
limit using FFmpeg.

## Features

- MP4/H.264 and WebM/VP9 output.
- Preset 9 MB target and custom size limits.
- Drag and drop and Explorer context-menu conversion.
- Videos with or without an audio stream.
- Resolution, frame-rate, sample-rate and sample-format controls.
- Optional speed, glitch and datamosh effects.
- Progress stages, encoding speed, ETA and cancellation.
- English and Russian UI, system dark-mode integration.
- Signed automatic updates through NetSparkle.

## Requirements

- Windows 10 version 1809 or newer, x64.
- Visual Studio 2022 or .NET 8 SDK for source builds.
- Git LFS when cloning the repository because `ffmpeg.exe` is an LFS object.

The release installer is self-contained. End users do not need to install .NET
or FFmpeg separately.

## Build

```powershell
git lfs install
git clone https://github.com/KickerMix/DiscordVideoCompressor.git
cd DiscordVideoCompressor
dotnet build DiscordVideoCompressor.sln -c Release
dotnet test DiscordVideoCompressor.sln -c Release
```

Build the Windows installer with:

```powershell
.\installer\Build-Installer.ps1
```

Inno Setup 6 must be installed for the installer step.

## Usage

1. Select or drop an `.mp4`, `.avi`, `.mkv` or `.webm` file.
2. Choose MP4 or WebM and the target size.
3. Adjust optional encoding/effect settings.
4. Start conversion. Output is written beside the source file using a
   `_cnvrtd` suffix; an index is added instead of overwriting an existing file.

The installer can add an Explorer context-menu command. Context-menu conversion
uses the 9 MB MP4 preset and places the output file on the clipboard when done.

## FFmpeg Runtime

FFmpeg 8.1 is embedded in the application. At runtime it is extracted once to a
content-hashed cache file under `%TEMP%\DiscordVideoCompressor`. Obsolete cache
copies are removed automatically.

To update the pinned binary and license after reviewing a new build:

```powershell
.\scripts\Update-Ffmpeg.ps1
```

## Licensing

The application source is MIT licensed; see [LICENSE.txt](./LICENSE.txt).

The bundled FFmpeg executable is a separate GPLv3-or-later program. Its license
is in [FFMPEG-GPL-LICENSE.txt](./FFMPEG-GPL-LICENSE.txt), and exact build/source
information is in [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md). Each
GitHub release also includes `ffmpeg-corresponding-source.zip`.
