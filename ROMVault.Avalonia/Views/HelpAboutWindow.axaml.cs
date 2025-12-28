using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ROMVault.Avalonia.Views
{
    public partial class HelpAboutWindow : Window
    {
        public HelpAboutWindow()
        {
            InitializeComponent();
            
            var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);
            string strVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            if (version.Revision > 0)
                strVersion += $" WIP{version.Revision}";

            Title = "Version " + strVersion + " : " + AppDomain.CurrentDomain.BaseDirectory;
            
            var lblVersion = this.FindControl<TextBlock>("lblVersion");
            if (lblVersion != null)
            {
                lblVersion.Text = "Version " + strVersion;
            }
        }

        private void Website_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try 
            { 
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://www.romvault.com/",
                    UseShellExecute = true
                });
            } 
            catch { }
        }

        private void PayPal_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try 
            { 
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://paypal.me/romvault",
                    UseShellExecute = true
                });
            } 
            catch { }
        }
    }
}
