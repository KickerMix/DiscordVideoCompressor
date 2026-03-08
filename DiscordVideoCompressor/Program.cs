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
                string startupFilePath = TryGetStartupFilePath(args);
                Application.Run(new Form1(startupFilePath));
            }
            finally
            {
                appUpdater?.Dispose();
            }
        }

        private static string TryGetStartupFilePath(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return null;
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
                        return NormalizePathArgument(inlinePath);
                    }

                    if (i + 1 < args.Length)
                    {
                        return NormalizePathArgument(args[i + 1]);
                    }

                    return null;
                }

                if (arg.StartsWith("-", StringComparison.Ordinal) || arg.StartsWith("/", StringComparison.Ordinal))
                {
                    continue;
                }

                return NormalizePathArgument(arg);
            }

            return null;
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
    }
}
