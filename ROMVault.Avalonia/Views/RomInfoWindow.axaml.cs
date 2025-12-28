using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RomVaultCore.RvDB;
using System.Text;

namespace ROMVault.Avalonia.Views
{
    public partial class RomInfoWindow : Window
    {
        public RomInfoWindow()
        {
            InitializeComponent();
        }

        public bool SetRom(RvFile tFile)
        {
            if (tFile.FileGroup == null)
                return false;

            StringBuilder sb = new StringBuilder();

            foreach(var v in tFile.FileGroup.Files)
            {
                sb.AppendLine(v.GotStatus+" | "+   v.FullName);
            }
            var textBox = this.FindControl<TextBox>("textBox1");
            if (textBox != null)
            {
                textBox.Text = sb.ToString();
            }
            return true;
        }
    }
}
