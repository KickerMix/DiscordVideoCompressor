using System;
using System.Windows.Forms;

namespace DiscordVideoCompressor
{
    internal static class Program
    {
        private static AppUpdater appUpdater;

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            appUpdater = AppUpdater.CreateOrNull();
            appUpdater?.Start();

            try
            {
                StartupLaunchOptions launchOptions = ParseStartupLaunchOptions(args);
                Application.Run(new Form1(launchOptions.FilePath, launchOptions.CopyOutputToClipboardOnSuccess));
            }
            finally
            {
                appUpdater?.Dispose();
            }
        }

        private static StartupLaunchOptions ParseStartupLaunchOptions(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return default;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                if (TryParseConvertArgument(arg, out string inlinePath))
                {
                    if (!string.IsNullOrWhiteSpace(inlinePath))
                    {
                        return new StartupLaunchOptions(NormalizePathArgument(inlinePath), true);
                    }

                    if (i + 1 < args.Length)
                    {
                        return new StartupLaunchOptions(NormalizePathArgument(args[i + 1]), true);
                    }

                    return default;
                }

                if (arg.StartsWith("-", StringComparison.Ordinal) || arg.StartsWith("/", StringComparison.Ordinal))
                {
                    continue;
                }

                return new StartupLaunchOptions(NormalizePathArgument(arg), false);
            }

            return default;
        }

        private static bool TryParseConvertArgument(string argument, out string inlinePath)
        {
            inlinePath = null;
            if (string.IsNullOrWhiteSpace(argument))
            {
                return false;
            }

            const string longOption = "--convert";
            const string shortOptionDash = "-convert";
            const string shortOptionSlash = "/convert";

            if (argument.Equals(longOption, StringComparison.OrdinalIgnoreCase) ||
                argument.Equals(shortOptionDash, StringComparison.OrdinalIgnoreCase) ||
                argument.Equals(shortOptionSlash, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string inlinePrefix = longOption + "=";
            if (argument.StartsWith(inlinePrefix, StringComparison.OrdinalIgnoreCase))
            {
                inlinePath = argument.Substring(inlinePrefix.Length);
                return true;
            }

            return false;
        }

        private static string NormalizePathArgument(string pathArgument)
        {
            if (string.IsNullOrWhiteSpace(pathArgument))
            {
                return null;
            }

            string trimmed = pathArgument.Trim();
            if (trimmed.Length >= 2 && trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed;
        }

        private readonly struct StartupLaunchOptions
        {
            public StartupLaunchOptions(string filePath, bool copyOutputToClipboardOnSuccess)
            {
                FilePath = filePath;
                CopyOutputToClipboardOnSuccess = copyOutputToClipboardOnSuccess;
            }

            public string FilePath { get; }

            public bool CopyOutputToClipboardOnSuccess { get; }
        }
    }
}
