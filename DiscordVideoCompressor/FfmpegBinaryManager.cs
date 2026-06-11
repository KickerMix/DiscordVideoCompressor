using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace DiscordVideoCompressor
{
    internal static class FfmpegBinaryManager
    {
        private static readonly object Sync = new object();
        private static string cachedPath;

        public static string GetOrExtract(Assembly assembly, string resourceName)
        {
            lock (Sync)
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
                if (IsValidExecutable(cachedPath))
                {
                    CleanupOldBinaries(tempDirectory, cachedPath);
                    return cachedPath;
                }

                Directory.CreateDirectory(tempDirectory);

                string hash = ComputeResourceHash(assembly, resourceName);
                string ffmpegPath = Path.Combine(tempDirectory, $"ffmpeg_{hash}.exe");
                if (!IsValidExecutable(ffmpegPath))
                {
                    ExtractResourceAtomically(assembly, resourceName, ffmpegPath);
                }

                CleanupOldBinaries(tempDirectory, ffmpegPath);
                cachedPath = ffmpegPath;
                return cachedPath;
            }
        }

        internal static bool IsValidExecutable(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream.Length >= 2 && stream.ReadByte() == 'M' && stream.ReadByte() == 'Z';
        }

        private static string ComputeResourceHash(Assembly assembly, string resourceName)
        {
            using Stream stream = OpenResource(assembly, resourceName);
            using SHA256 sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
        }

        private static void ExtractResourceAtomically(Assembly assembly, string resourceName, string destinationPath)
        {
            string temporaryPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (Stream resourceStream = OpenResource(assembly, resourceName))
                using (var outputStream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    resourceStream.CopyTo(outputStream);
                    outputStream.Flush(true);
                }

                if (!IsValidExecutable(temporaryPath))
                {
                    throw new InvalidOperationException("Embedded ffmpeg resource is invalid or incomplete.");
                }

                try
                {
                    File.Move(temporaryPath, destinationPath, true);
                }
                catch (IOException) when (IsValidExecutable(destinationPath))
                {
                    // Another application instance completed the same extraction.
                }
            }
            finally
            {
                TryDelete(temporaryPath);
            }
        }

        private static Stream OpenResource(Assembly assembly, string resourceName)
        {
            return assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded ffmpeg resource '{resourceName}' was not found.");
        }

        private static void CleanupOldBinaries(string directory, string currentPath)
        {
            foreach (string path in Directory.GetFiles(directory, "ffmpeg_*.exe"))
            {
                if (!string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(path);
                }
            }

            foreach (string path in Directory.GetFiles(directory, "ffmpeg_*.tmp"))
            {
                TryDelete(path);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to clean up cached ffmpeg file: " + ex.Message);
            }
        }
    }
}
