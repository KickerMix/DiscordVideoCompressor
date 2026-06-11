using System;
using System.Drawing;
using System.IO;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.WinForms;

namespace DiscordVideoCompressor
{
    internal sealed class AppUpdater : IDisposable
    {
        private readonly SparkleUpdater sparkleUpdater;

        private AppUpdater(SparkleUpdater sparkleUpdater)
        {
            this.sparkleUpdater = sparkleUpdater;
        }

        public static AppUpdater CreateOrNull()
        {
            if (!AppUpdaterSettings.IsConfigured)
            {
                return null;
            }

            var sparkle = new SparkleUpdater(
                AppUpdaterSettings.AppCastUrl,
                new Ed25519Checker(SecurityMode.Strict, AppUpdaterSettings.PublicEd25519Key))
            {
                UIFactory = CreateUiFactory(),
                RelaunchAfterUpdate = false,
                UserInteractionMode = UserInteractionMode.DownloadAndInstall
            };

            return new AppUpdater(sparkle);
        }

        public void Start()
        {
            sparkleUpdater.StartLoop(true);
        }

        public void CheckForUpdates()
        {
            sparkleUpdater.CheckForUpdatesAtUserRequest();
        }

        public void Dispose()
        {
            sparkleUpdater?.Dispose();
        }

        private static IUIFactory CreateUiFactory()
        {
            string executablePath = Path.Combine(AppContext.BaseDirectory, "DiscordVideoCompressor.exe");
            if (File.Exists(executablePath))
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(executablePath);
                if (appIcon != null)
                {
                    return new UIFactory(appIcon);
                }
            }

            return new UIFactory();
        }
    }
}
