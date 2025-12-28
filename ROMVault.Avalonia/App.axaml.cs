using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RomVaultCore;
using RomVaultCore.Utils;
using RomVaultCore.FixFile.FixAZipCore;
using System;

namespace ROMVault.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            // Wire up ReportError
            ReportError.ErrorForm += (message) =>
            {
                global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var win = new Views.ShowErrorWindow();
                    win.settype(message);
                    win.Show();
                });
            };

            ReportError.Dialog += (text, caption) =>
            {
                 global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var win = new Views.MessageBoxWindow();
                    win.Title = caption;
                    win.SetMessage(text);
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null && desktop.MainWindow.IsVisible)
                        await win.ShowDialog(desktop.MainWindow);
                    else
                        win.Show();
                });
            };

            // Core Initialization
            Settings.rvSettings = new Settings();
            Settings.rvSettings = Settings.SetDefaults(out string errorReadingSettings);

            if (!string.IsNullOrWhiteSpace(errorReadingSettings))
            {
                ReportError.Show(errorReadingSettings, "Error Reading Settings");
                System.Diagnostics.Debug.WriteLine($"Error Reading Settings: {errorReadingSettings}");
            }

            // Dark mode handling
            if (Settings.rvSettings.Darkness)
            {
                RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
            }
            else
            {
                RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;
            }

            FindSourceFile.SetFixOrderSettings();

            RootDirsCreate.CheckDatRoot();
            RootDirsCreate.CheckRomRoot();
            RootDirsCreate.CheckToSort();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                
                var splash = new Views.SplashWindow();
                desktop.MainWindow = splash;
                
                splash.Closed += (s, e) =>
                {
                    var mainWindow = new MainWindow();
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("crash_log.txt", ex.ToString());
            throw;
        }
    }
}