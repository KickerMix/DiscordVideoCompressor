# Discord Video Compressor

## Other Languages
- [Русская версия](./README_RU.md)

## Overview

The Discord Video Compressor is a Windows application designed to compress video files to meet Discord's file size limits. The tool utilizes `ffmpeg` to compress videos, offering pre-configured size presets and the option to specify custom sizes.

## Features

- **Drag and Drop Support:** Simply drag and drop your video files into the application to start the compression process.
- **Language Support:** The application supports English (EN) and Russian (RU) languages, which can be selected via a dropdown menu.
- **Dark Mode:** Automatically adjusts the application theme to match the system's dark mode settings.
- **ffmpeg Integration:** The application ships `ffmpeg.exe` as an embedded resource, so the user still gets a single app package without a separate dependency setup.
- **Preset and Custom Compression:** Users can use the built-in Discord preset (9 MB) or specify a custom target size for compression.
- **Progress Monitoring:** Displays the progress of the compression process with a progress bar.
- **Force Stop:** Users can forcibly stop the compression process if necessary.

## Installation

1. **Clone the repository:**

   ```
   sh
   git clone https://github.com/yourusername/DiscordVideoCompressor.git
   ```

2. **Build the project:**
   - Open the solution file (`DiscordVideoCompressor.sln`) in Visual Studio 2022 or build it with the .NET 8 SDK.
   - Build the project.

3. **Run the application:**
   - After building, run the `DiscordVideoCompressor.exe` from the output directory.

## Windows Installer

- The repository now includes an Inno Setup script at [installer/DiscordVideoCompressor.iss](./installer/DiscordVideoCompressor.iss).
- Build a release installer from [installer/Build-Installer.ps1](./installer/Build-Installer.ps1). The script publishes the single-file Win64 build first and then compiles `setup.exe`.
- The installer places the app in `Program Files`, registers an uninstaller, and creates a Start menu shortcut so the user can launch it through `Start`.
- `ffmpeg.exe` remains embedded inside the application binary. The installer does not add a separate ffmpeg dependency.

## Automatic Updates

- NetSparkle is wired to GitHub Releases through [DiscordVideoCompressor/AppUpdaterSettings.cs](./DiscordVideoCompressor/AppUpdaterSettings.cs).
- The app checks this URL for updates:
  - `https://github.com/KickerMix/DiscordVideoCompressor/releases/latest/download/appcast.xml`
- The repository includes the public Ed25519 key at [installer/keys/NetSparkle_Ed25519.pub](./installer/keys/NetSparkle_Ed25519.pub). The private key stays local and is ignored by git.
- Generate appcast files for each installer release with [installer/New-AppCast.ps1](./installer/New-AppCast.ps1).
- Upload the installer plus `appcast.xml` and `appcast.xml.signature` to a GitHub release with [installer/Publish-GitHubRelease.ps1](./installer/Publish-GitHubRelease.ps1). The script expects `GITHUB_TOKEN`.
- Recommended release flow:
  1. Build the installer with `installer/Build-Installer.ps1`.
  2. Generate the appcast for a tag, for example `1.1.3`, with `installer/New-AppCast.ps1`.
  3. Set `GITHUB_TOKEN` and upload the installer and appcast assets with `installer/Publish-GitHubRelease.ps1 -Tag 1.1.3`.
  4. After the release is published, installed copies will see the new version through GitHub Releases.

## GitHub Actions Release

- The repository can also publish releases automatically from Git tags through GitHub Actions.
- Recommended tag format is the current repository format without a prefix, for example `1.1.3`.
- The workflow should:
  - verify that the git tag matches the project version;
  - build and test on Windows;
  - create the installer;
  - restore the NetSparkle private key from a GitHub secret;
  - generate `appcast.xml`;
  - create/update the GitHub Release and upload all assets.

## Usage

1. **Select or Drag and Drop a Video File:**
   - You can drag and drop a video file into the application window or use the "Choose Media File" button to select a file.
   - Supported formats: `.mp4`, `.avi`, `.mkv`, `.webm`.

2. **Choose the Compression Size:**
   - Use the default 9 MB Discord preset or specify a custom size in MB.

3. **Start Compression:**
   - Click the "Convert" button to start the compression process.
   - The progress of the compression will be displayed in the progress bar.

4. **Force Stop (if needed):**
   - You can stop the compression at any time by clicking the "Force Stop" button.

5. **Language Selection:**
   - Change the language of the application between English and Russian using the dropdown in the top-right corner.

## Dependencies

- **ffmpeg:** The application deliberately keeps `ffmpeg.exe` as an embedded resource and extracts it automatically at runtime. No external installation is required, and the distributed build still includes everything needed for end users.
- **NetSparkle:** Automatic updates are prepared through NetSparkle and are expected to distribute the installer package in future releases.

## Contributing

If you would like to contribute to the project, feel free to fork the repository and submit a pull request.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.

## Troubleshooting

- **ffmpeg Not Found Error:** Ensure that `ffmpeg.exe` is available in the temporary directory. The application should automatically handle extraction and deletion, but if errors persist, check file permissions.
- **Video File Not Supported:** Ensure your video file is in one of the supported formats (`.mp4`, `.avi`, `.mkv`, `.webm`).
- **Compression Fails:** If the compression process fails repeatedly, try lowering the target size.

## Contact

For any issues or suggestions, please open an issue on GitHub or contact me at Discord: [KickerMix].
