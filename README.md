# Discord Video Compressor

## Overview

The Discord Video Compressor is a Windows application designed to compress video files to meet Discord's file size limits. The tool utilizes `ffmpeg` to compress videos, offering pre-configured size presets and the option to specify custom sizes.

## Features

- **Drag and Drop Support:** Simply drag and drop your video files into the application to start the compression process.
- **Language Support:** The application supports English (EN) and Russian (RU) languages, which can be selected via a dropdown menu.
- **Dark Mode:** Automatically adjusts the application theme to match the system's dark mode settings.
- **ffmpeg Integration:** The application uses `ffmpeg` for video compression, with the binary automatically extracted and managed within the application.
- **Preset and Custom Compression:** Users can choose between a preset size for Discord (24MB) or specify a custom target size for compression.
- **Progress Monitoring:** Displays the progress of the compression process with a progress bar.
- **Force Stop:** Users can forcibly stop the compression process if necessary.

## Installation

1. **Clone the repository:**

   ```
   sh
   git clone https://github.com/yourusername/DiscordVideoCompressor.git
   ```

2. **Build the project:**
   - Open the solution file (`DiscordVideoCompressor.sln`) in Visual Studio.
   - Build the project.

3. **Run the application:**
   - After building, run the `DiscordVideoCompressor.exe` from the output directory.

## Usage

1. **Select or Drag and Drop a Video File:**
   - You can drag and drop a video file into the application window or use the "Choose Media File" button to select a file.
   - Supported formats: `.mp4`, `.avi`, `.mkv`, `.webm`.

2. **Choose the Compression Size:**
   - Use the default 24MB Discord preset or specify a custom size in MB.

3. **Start Compression:**
   - Click the "Convert" button to start the compression process.
   - The progress of the compression will be displayed in the progress bar.

4. **Force Stop (if needed):**
   - You can stop the compression at any time by clicking the "Force Stop" button.

5. **Language Selection:**
   - Change the language of the application between English and Russian using the dropdown in the top-right corner.

## Dependencies

- **ffmpeg:** The application includes a version of `ffmpeg` that is automatically extracted and managed. No external installation is required.

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