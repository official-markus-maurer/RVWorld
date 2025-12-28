using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RomVaultCore;
using System;

namespace ROMVault.Avalonia.Views
{
    public partial class ShowErrorWindow : Window
    {
        public ShowErrorWindow()
        {
            InitializeComponent();
            var label1 = this.FindControl<TextBlock>("label1");
            if (Settings.rvSettings.DoNotReportFeedback && label1 != null)
                label1.Text = "You have opted out of sending this Crash Report";
        }

        public void settype(string s)
        {
            var textBox = this.FindControl<TextBox>("textBox1");
            if (textBox != null)
            {
                textBox.Text = s;
            }
        }

        private void Button1_Click(object? sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
