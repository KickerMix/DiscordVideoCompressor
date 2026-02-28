using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.SignatureVerifiers;

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
            // NetSparkle's WinForms UI package ships the UIFactory type; reflection keeps
            // the app resilient across minor API differences while still using the package.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type uiFactoryType = assembly.GetType("NetSparkleUpdater.UI.WinForms.UIFactory");
                if (uiFactoryType == null)
                {
                    continue;
                }

                try
                {
                    string executablePath = Path.Combine(AppContext.BaseDirectory, "DiscordVideoCompressor.exe");
                    using Icon appIcon = File.Exists(executablePath)
                        ? Icon.ExtractAssociatedIcon(executablePath)
                        : null;
                    if (appIcon != null)
                    {
                        object instance = Activator.CreateInstance(uiFactoryType, appIcon);
                        if (instance != null)
                        {
                            return instance as IUIFactory;
                        }
                    }
                }
                catch
                {
                }

                object fallback = Activator.CreateInstance(uiFactoryType);
                if (fallback != null)
                {
                    return fallback as IUIFactory;
                }
            }

            return null;
        }
    }
}
