using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using TrrntZipUICore;

namespace TrrntZipUI
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if NET10_0
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.SetDefaultFont(new Font(new FontFamily("Microsoft Sans Serif"), 8.25f));
#endif

            Version Version = Assembly.GetEntryAssembly().GetName().Version;
            string strVersion = $"{Version.Major}.{Version.Minor}.{Version.Build}";

            FrmTrrntzip frmTrrntzip = new FrmTrrntzip();
            frmTrrntzip.Text = $"SAM-UI ({strVersion})";
            Application.Run(frmTrrntzip);
        }
    }
}