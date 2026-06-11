# Third-Party Notices

## FFmpeg

Discord Video Compressor includes an unmodified `ffmpeg.exe` command-line
program as an embedded application resource. The application starts FFmpeg as
a separate process and does not link to FFmpeg libraries.

- Binary package: `ffmpeg-n8.1-latest-win64-gpl-8.1.zip`
- Build release: `autobuild-2026-06-10-17-02`
- Distributor/build recipes: <https://github.com/BtbN/FFmpeg-Builds>
- Build recipes commit: `a9410e4be2b332e535000004a8ebf304d9b46689`
- FFmpeg source commit: `e4c7fbf6c0a922297d4df7288d7aea665af15e24`
- Embedded executable SHA-256: `CA7E2992D973AA5B042BE093EDA6FDCE43CA41AAFDD647148278589B1D753F60`
- Download archive SHA-256: `D8227EB85F9327F4FCDF3CFBC84B8B14492C9DBB7C324B4DE32654F1CCDDD75E`

This build was configured with `--enable-gpl` and `--enable-version3`, and
contains GPL components including libx264. The FFmpeg executable is therefore
distributed under GPL version 3 or later. Its license is included in
`FFMPEG-GPL-LICENSE.txt`.

The release workflow publishes `ffmpeg-corresponding-source.zip` beside every
installer. It contains the exact FFmpeg source snapshot and the pinned build
recipes used to obtain dependency sources and reproduce the Windows build.

FFmpeg project: <https://ffmpeg.org/>

FFmpeg legal information: <https://ffmpeg.org/legal.html>

## NetSparkleUpdater

The application uses NetSparkleUpdater for signed application updates.

- Project: <https://github.com/NetSparkleUpdater/NetSparkle>
- License: MIT
