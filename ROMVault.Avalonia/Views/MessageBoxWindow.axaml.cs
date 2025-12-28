using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ROMVault.Avalonia.Views
{
    public partial class MessageBoxWindow : Window
    {
        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        public void SetMessage(string message)
        {
            var textBlock = this.FindControl<TextBlock>("MessageText");
            if (textBlock != null)
            {
                textBlock.Text = message;
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}