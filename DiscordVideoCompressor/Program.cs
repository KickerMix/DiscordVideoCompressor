using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            appUpdater = AppUpdater.CreateOrNull();
            appUpdater?.Start();

            try
            {
                Application.Run(new Form1());
            }
            finally
            {
                appUpdater?.Dispose();
            }
        }
    }
}
