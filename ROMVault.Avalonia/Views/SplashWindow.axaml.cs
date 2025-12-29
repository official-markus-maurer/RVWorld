using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RomVaultCore;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;
using System;
using System.Reflection;

namespace ROMVault.Avalonia.Views
{
    public partial class SplashWindow : Window
    {
        private double _opacityIncrement = 0.05;
        private readonly ThreadWorker _thWrk;
        private readonly DispatcherTimer _timer;

        public SplashWindow()
        {
            InitializeComponent();
            
            var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);
            string strVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            if (version.Revision > 0)
                strVersion += $" WIP{version.Revision}";
            
            var lblVersion = this.FindControl<TextBlock>("lblVersion");
            if (lblVersion != null)
                lblVersion.Text = $"Version {strVersion} : {AppDomain.CurrentDomain.BaseDirectory}";

            Opacity = 0;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += Timer1Tick;

            _thWrk = new ThreadWorker(StartUpCode) { wReport = BgwProgressChanged, wFinal = BgwRunWorkerCompleted };
            
            Opened += SplashWindow_Opened;
        }

        private void SplashWindow_Opened(object? sender, EventArgs e)
        {
            _thWrk.StartAsync();
            _timer.Start();
        }

        private static void StartUpCode(ThreadWorker thWrk)
        {
            RepairStatus.InitStatusCheck();
            DB.Read(thWrk);
            if (DB.DirRoot != null)
            {
                RepairStatus.ReportStatusReset(DB.DirRoot);
            }
        }

        private void BgwProgressChanged(object e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var progressBar = this.FindControl<ProgressBar>("progressBar");
                var lblStatus = this.FindControl<TextBlock>("lblStatus");

                if (e is int percent)
                {
                    if (progressBar != null && percent >= progressBar.Minimum && percent <= progressBar.Maximum)
                    {
                        progressBar.Value = percent;
                    }
                    return;
                }
                
                if (e is bgwSetRange bgwSr)
                {
                    if (progressBar != null)
                    {
                        progressBar.Minimum = 0;
                        progressBar.Maximum = bgwSr.MaxVal;
                        progressBar.Value = 0;
                    }
                    return;
                }

                if (e is bgwText bgwT)
                {
                    if (lblStatus != null)
                    {
                        lblStatus.Text = bgwT.Text;
                    }
                }
            });
        }

        private void BgwRunWorkerCompleted()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _opacityIncrement = -0.1;
                _timer.Start();
            });
        }

        private void Timer1Tick(object? sender, EventArgs e)
        {
            if (_opacityIncrement > 0)
            {
                if (Opacity < 1)
                {
                    Opacity += _opacityIncrement;
                }
                else
                {
                    _timer.Stop();
                }
            }
            else
            {
                if (Opacity > 0)
                {
                    Opacity += _opacityIncrement;
                }
                else
                {
                    _timer.Stop();
                    Close();
                }
            }
        }
    }
}
