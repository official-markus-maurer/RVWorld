using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using RomVaultCore;
using RomVaultCore.RvDB;
using RomVaultCore.ReadDat;
using RomVaultCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ROMVault.Avalonia.Views
{
    public partial class DirectoryMappingsWindow : Window
    {
        private Color _cMagenta = Color.FromRgb(255, 214, 255);
        private Color _cGreen = Color.FromRgb(214, 255, 214);
        private Color _cYellow = Color.FromRgb(255, 255, 214);
        private Color _cRed = Color.FromRgb(255, 214, 214);

        private DirMapping _rule = null!;
        private bool _displayType;

        public DirectoryMappingsWindow()
        {
            InitializeComponent();
            
            if (Settings.rvSettings.DirMappings.Count > 0)
                _rule = Settings.rvSettings.DirMappings[0];
            else
                _rule = new DirMapping { DirKey = "RomVault" };

            // Fix colors for dark mode if needed (Avalonia handles themes differently, but logic preserved)
            if (Settings.rvSettings.Darkness)
            {
                _cMagenta = Color.FromRgb((byte)(255 * 0.8), (byte)(214 * 0.8), (byte)(255 * 0.8));
                _cGreen = Color.FromRgb((byte)(214 * 0.8), (byte)(255 * 0.8), (byte)(214 * 0.8));
                _cYellow = Color.FromRgb((byte)(255 * 0.8), (byte)(255 * 0.8), (byte)(214 * 0.8));
            }

            // Setup events
            var btnSetROMLocation = this.FindControl<Button>("btnSetROMLocation");
            var btnClearROMLocation = this.FindControl<Button>("btnClearROMLocation");
            var btnApply = this.FindControl<Button>("btnSet");
            var btnDelete = this.FindControl<Button>("btnDelete");
            var btnDeleteSelected = this.FindControl<Button>("btnDeleteSelected");
            var btnResetAll = this.FindControl<Button>("btnResetAll");
            var btnClose = this.FindControl<Button>("btnClose");
            var dgRules = this.FindControl<DataGrid>("DGDirectoryMappingRules");

            if (btnSetROMLocation != null) btnSetROMLocation.Click += BtnSetROMLocationClick;
            if (btnClearROMLocation != null) btnClearROMLocation.Click += BtnClearROMLocation_Click;
            if (btnApply != null) btnApply.Click += BtnApplyClick;
            if (btnDelete != null) btnDelete.Click += BtnDeleteClick;
            if (btnDeleteSelected != null) btnDeleteSelected.Click += BtnDeleteSelectedClick;
            if (btnResetAll != null) btnResetAll.Click += BtnResetAllClick;
            if (btnClose != null) btnClose.Click += BtnCloseClick;
            if (dgRules != null) dgRules.DoubleTapped += DataGridGamesDoubleClick;

            UpdateGrid();
            SetDisplay();
        }

        public void SetLocation(string dLocation)
        {
            _rule = FindRule(dLocation);
            SetDisplay();
            UpdateGrid();
        }

        public void SetDisplayType(bool type)
        {
            _displayType = type;
            var btnDelete = this.FindControl<Button>("btnDelete");
            if (btnDelete != null) btnDelete.IsVisible = type;

            // Hide/Show controls based on type (simplified logic compared to WinForms loop)
            var lblDelete = this.FindControl<TextBlock>("lblDelete"); // "Existing Mapping"
            var dgRules = this.FindControl<DataGrid>("DGDirectoryMappingRules");
            var btnDeleteSelected = this.FindControl<Button>("btnDeleteSelected");
            var btnResetAll = this.FindControl<Button>("btnResetAll");
            var btnClose = this.FindControl<Button>("btnClose");

            bool showGrid = !type;
            if (lblDelete != null) lblDelete.IsVisible = showGrid;
            if (dgRules != null) dgRules.IsVisible = showGrid;
            if (btnDeleteSelected != null) btnDeleteSelected.IsVisible = showGrid;
            if (btnResetAll != null) btnResetAll.IsVisible = showGrid;
            if (btnClose != null) btnClose.IsVisible = showGrid;

            Height = type ? 155 : 428;
            CanResize = !type;
        }

        private static DirMapping FindRule(string dLocation)
        {
            foreach (DirMapping t in Settings.rvSettings.DirMappings)
            {
                if (string.Compare(t.DirKey, dLocation, StringComparison.Ordinal) == 0)
                    return t;
            }

            return new DirMapping { DirKey = dLocation };
        }

        private void SetDisplay()
        {
            var txtDATLocation = this.FindControl<TextBlock>("txtDATLocation");
            var txtROMLocation = this.FindControl<TextBlock>("txtROMLocation");
            
            if (txtDATLocation != null) txtDATLocation.Text = _rule.DirKey;
            if (txtROMLocation != null) txtROMLocation.Text = _rule.DirPath;
        }

        private void UpdateGrid()
        {
            var dgRules = this.FindControl<DataGrid>("DGDirectoryMappingRules");
            if (dgRules == null) return;

            var items = new List<DirMappingViewModel>();
            foreach (DirMapping t in Settings.rvSettings.DirMappings)
            {
                var vm = new DirMappingViewModel(t);
                
                if (t.DirPath == "ToSort")
                {
                    vm.BgColor = new SolidColorBrush(_cMagenta);
                }
                else if (t == _rule)
                {
                    vm.BgColor = new SolidColorBrush(_cGreen);
                }
                else if (t.DirKey.Length > _rule.DirKey.Length)
                {
                    if (t.DirKey.Substring(0, _rule.DirKey.Length + 1) == _rule.DirKey + "\\")
                    {
                        vm.BgColor = new SolidColorBrush(_cYellow);
                    }
                }

                if (!Directory.Exists(t.DirPath))
                {
                    vm.BgColor = new SolidColorBrush(_cRed);
                }
                items.Add(vm);
            }
            dgRules.ItemsSource = items;
        }

        private void BtnClearROMLocation_Click(object? sender, RoutedEventArgs e)
        {
            var txtROMLocation = this.FindControl<TextBlock>("txtROMLocation");
            if (txtROMLocation == null) return;

            if (_rule.DirKey == "RomVault")
            {
                txtROMLocation.Text = "RomRoot";
                return;
            }

            if (_rule.DirKey == "ToSort")
            {
                txtROMLocation.Text = "ToSort";
                return;
            }

            txtROMLocation.Text = null;
        }

        private async void BtnSetROMLocationClick(object? sender, RoutedEventArgs e)
        {
            var txtROMLocation = this.FindControl<TextBlock>("txtROMLocation");
            
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Please select a folder for This Rom Set",
                AllowMultiple = false
            });

            if (folders.Count == 1 && txtROMLocation != null)
            {
                string selectedPath = folders[0].Path.LocalPath;
                string relPath = RelativePath.MakeRelative(AppDomain.CurrentDomain.BaseDirectory, selectedPath);
                txtROMLocation.Text = relPath;
            }
        }

        private void BtnApplyClick(object? sender, RoutedEventArgs e)
        {
            var txtROMLocation = this.FindControl<TextBlock>("txtROMLocation");
            string? newDir = txtROMLocation?.Text;

            if (string.IsNullOrWhiteSpace(newDir))
            {
                // Show message box (simplified)
                return;
            }
            
            // Avalonia doesn't have a direct equivalent to Directory.Exists for relative paths without resolving, 
            // but RomVault logic seems to assume it works.
            // Check existence logic from WinForms
            // For now, assuming standard IO check.
            
            _rule.DirPath = newDir;

            bool updatingRule = false;
            int i;
            for (i = 0; i < Settings.rvSettings.DirMappings.Count; i++)
            {
                if (Settings.rvSettings.DirMappings[i] == _rule)
                {
                    updatingRule = true;
                    break;
                }

                if (string.Compare(Settings.rvSettings.DirMappings[i].DirKey, _rule.DirKey, StringComparison.Ordinal) > 0)
                {
                    break;
                }
            }

            if (!updatingRule)
                Settings.rvSettings.DirMappings.Insert(i, _rule);

            UpdateGrid();
            Settings.WriteConfig(Settings.rvSettings);

            if (_displayType)
                Close();
        }

        private void BtnDeleteClick(object? sender, RoutedEventArgs e)
        {
            string datLocation = _rule.DirKey;

            if (datLocation == "RomVault")
            {
                // Show error
                return;
            }
            else
            {
                DatUpdate.CheckAllDats(DB.DirRoot.Child(0), datLocation);
                for (int i = 0; i < Settings.rvSettings.DirMappings.Count; i++)
                {
                    if (Settings.rvSettings.DirMappings[i].DirKey == datLocation)
                    {
                        Settings.rvSettings.DirMappings.RemoveAt(i);
                        i--;
                    }
                }
            }
            Settings.WriteConfig(Settings.rvSettings);

            UpdateGrid();
            Close();
        }

        private void BtnDeleteSelectedClick(object? sender, RoutedEventArgs e)
        {
            var dgRules = this.FindControl<DataGrid>("DGDirectoryMappingRules");
            if (dgRules?.SelectedItems == null) return;

            foreach (var item in dgRules.SelectedItems)
            {
                if (item is DirMappingViewModel vm)
                {
                    string datLocation = vm.DirKey;
                    if (datLocation == "RomVault")
                    {
                        continue;
                    }
                    else
                    {
                        for (int i = 0; i < Settings.rvSettings.DirMappings.Count; i++)
                        {
                            if (Settings.rvSettings.DirMappings[i].DirKey == datLocation)
                            {
                                Settings.rvSettings.DirMappings.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            }
            Settings.WriteConfig(Settings.rvSettings);
            UpdateGrid();
        }

        private void BtnResetAllClick(object? sender, RoutedEventArgs e)
        {
            Settings.rvSettings.ResetDirMappings();
            Settings.WriteConfig(Settings.rvSettings);
            _rule = Settings.rvSettings.DirMappings[0];
            UpdateGrid();
            SetDisplay();
        }

        private void BtnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DataGridGamesDoubleClick(object? sender, global::Avalonia.Input.TappedEventArgs e)
        {
            var dgRules = this.FindControl<DataGrid>("DGDirectoryMappingRules");
            if (dgRules?.SelectedItem == null) return;

            if (dgRules.SelectedItem is DirMappingViewModel vm)
            {
                Title = "Edit Existing Directory / DATs Mapping";
                _rule = FindRule(vm.DirKey);
                UpdateGrid();
                SetDisplay();
            }
        }
    }

    public class DirMappingViewModel
    {
        public string DirKey { get; set; }
        public string DirPath { get; set; }
        public IBrush BgColor { get; set; }

        public DirMappingViewModel(DirMapping mapping)
        {
            DirKey = mapping.DirKey;
            DirPath = mapping.DirPath;
            BgColor = Brushes.Transparent;
        }
    }
}
