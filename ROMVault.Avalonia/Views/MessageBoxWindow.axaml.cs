using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ROMVault.Avalonia.Views
{
    /// <summary>
    /// A simple message box window for displaying information to the user.
    /// </summary>
    public partial class MessageBoxWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBoxWindow"/> class.
        /// </summary>
        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the message text to be displayed.
        /// </summary>
        /// <param name="message">The message content.</param>
        public void SetMessage(string message)
        {
            var textBlock = this.FindControl<TextBlock>("MessageText");
            if (textBlock != null)
            {
                textBlock.Text = message;
            }
        }

        /// <summary>
        /// Handles the OK button click.
        /// Closes the window.
        /// </summary>
        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}