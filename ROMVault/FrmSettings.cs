/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2025                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RomVaultCore;
using RomVaultCore.Utils;
using RomVaultCore.RvDB;

namespace ROMVault
{
    public partial class FrmSettings : Form
    {
        private CheckBox chkChdCache;
        private CheckBox chkChdDebug;
        private CheckBox chkChdStrictCueGdi;
        private CheckBox chkChdExportOnFix;
        private CheckBox chkChdStreaming;
        private CheckBox chkChdPreferSynthetic;
        private CheckBox chkChdTrustContainer;
        private NumericUpDown upChdDvdHunk;
        private Button btnPurgeChdCache;
        public FrmSettings()
        {
            InitializeComponent();

            cboFixLevel.Items.Clear();
            cboFixLevel.Items.Add("Level 1 - Fast copy Match on CRC");
            cboFixLevel.Items.Add("Level 2 - Fast copy if SHA1 scanned");
            cboFixLevel.Items.Add("Level 3 - Uncompress/Hash/Compress");

            cboCores.Items.Add("Auto");
            for (int i = 1; i <= 64; i++)
                cboCores.Items.Add(i.ToString());

            cbo7zStruct.Items.Add("LZMA Solid - rv7z");
            cbo7zStruct.Items.Add("LZMA Non-Solid");
            cbo7zStruct.Items.Add("ZSTD Solid");
            cbo7zStruct.Items.Add("ZSTD Non-Solid");

            if (Settings.rvSettings.Darkness)
                Dark.dark.SetColors(this);

            int chdInsertTop = 372;
            int chdInsertHeight = 174;
            int shiftStartTop = 392;
            int shift = chdInsertTop + chdInsertHeight - shiftStartTop + 8;
            if (shift > 0)
            {
                ClientSize = new Size(ClientSize.Width, ClientSize.Height + shift);
                btnOK.Top += shift;
                btnCancel.Top += shift;
                chkSendFoundMIA.Top += shift;
                chkSendFoundMIAAnon.Top += shift;
                chkDeleteOldCueFiles.Top += shift;
                chkDetailedReporting.Top += shift;
                chkDebugLogs.Top += shift;
                chkDoNotReportFeedback.Top += shift;
                lblSide3.Top += shift;
                lblSide4.Top += shift;
            }

            GroupBox grpChd = new GroupBox();
            grpChd.Text = "CHD";
            grpChd.Left = 125;
            grpChd.Top = chdInsertTop;
            grpChd.Width = ClientSize.Width - grpChd.Left - 20;
            grpChd.Height = chdInsertHeight;
            grpChd.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(grpChd);

            chkChdCache = new CheckBox();
            chkChdCache.AutoSize = true;
            chkChdCache.Text = "Enable CHD scan cache";
            chkChdCache.Left = 12;
            chkChdCache.Top = 22;
            grpChd.Controls.Add(chkChdCache);

            chkChdDebug = new CheckBox();
            chkChdDebug.AutoSize = true;
            chkChdDebug.Text = "Write CHD scan debug logs";
            chkChdDebug.Left = 12;
            chkChdDebug.Top = 44;
            grpChd.Controls.Add(chkChdDebug);

            chkChdStreaming = new CheckBox();
            chkChdStreaming.AutoSize = true;
            chkChdStreaming.Text = "Enable CHD streaming (experimental)";
            chkChdStreaming.Left = 12;
            chkChdStreaming.Top = 66;
            chkChdStreaming.CheckedChanged += (s, e) =>
            {
                if (chkChdStreaming.Checked)
                    MessageBox.Show("Enables CHD streaming hashing without extracting (DVD/ISO and some CD/GDI when metadata is available).", "RomVault", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            grpChd.Controls.Add(chkChdStreaming);

            int col2 = grpChd.Width / 2;

            chkChdStrictCueGdi = new CheckBox();
            chkChdStrictCueGdi.AutoSize = true;
            chkChdStrictCueGdi.Text = "Strict: require CUE/GDI in CHD mode";
            chkChdStrictCueGdi.Left = col2;
            chkChdStrictCueGdi.Top = 22;
            chkChdStrictCueGdi.CheckedChanged += (s, e) =>
            {
                if (chkChdStrictCueGdi.Checked)
                    MessageBox.Show("CHD cannot faithfully reproduce original CUE/GDI contents. Strict mode will often keep sets incomplete.", "RomVault", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            grpChd.Controls.Add(chkChdStrictCueGdi);

            chkChdExportOnFix = new CheckBox();
            chkChdExportOnFix.AutoSize = true;
            chkChdExportOnFix.Text = "Allow exporting tracks during Fix";
            chkChdExportOnFix.Left = col2;
            chkChdExportOnFix.Top = 44;
            chkChdExportOnFix.CheckedChanged += (s, e) =>
            {
                if (chkChdExportOnFix.Checked)
                    MessageBox.Show("This will extract track files from CHD into your ROM folders during fixing. It is off by default because it defeats CHD container storage.", "RomVault", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            grpChd.Controls.Add(chkChdExportOnFix);

            chkChdPreferSynthetic = new CheckBox();
            chkChdPreferSynthetic.AutoSize = true;
            chkChdPreferSynthetic.Text = "Prefer synthetic CUE/GDI when possible";
            chkChdPreferSynthetic.Left = col2;
            chkChdPreferSynthetic.Top = 66;
            chkChdPreferSynthetic.CheckedChanged += (s, e) =>
            {
                if (chkChdPreferSynthetic.Checked)
                    MessageBox.Show("When a DAT expects CUE/GDI but metadata-only descriptors are acceptable (no strict hashes), prefer synthetic descriptors generated from CHD metadata.", "RomVault", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            grpChd.Controls.Add(chkChdPreferSynthetic);

            chkChdTrustContainer = new CheckBox();
            chkChdTrustContainer.AutoSize = true;
            chkChdTrustContainer.Text = "Treat CHD as satisfying track files";
            chkChdTrustContainer.Left = 12;
            chkChdTrustContainer.Top = 88;
            grpChd.Controls.Add(chkChdTrustContainer);

            Label lblDvdHunk = new Label();
            lblDvdHunk.AutoSize = true;
            lblDvdHunk.Text = "DVD hunk size (KiB)";
            lblDvdHunk.Left = 12;
            lblDvdHunk.Top = 112;
            grpChd.Controls.Add(lblDvdHunk);

            upChdDvdHunk = new NumericUpDown();
            upChdDvdHunk.Left = 140;
            upChdDvdHunk.Top = 108;
            upChdDvdHunk.Minimum = 0;
            upChdDvdHunk.Maximum = 1024;
            upChdDvdHunk.Increment = 32;
            upChdDvdHunk.Width = 90;
            grpChd.Controls.Add(upChdDvdHunk);

            btnPurgeChdCache = new Button();
            btnPurgeChdCache.Text = "Purge CHD Scan Cache";
            btnPurgeChdCache.Left = col2;
            btnPurgeChdCache.Top = 138;
            btnPurgeChdCache.Width = grpChd.Width - btnPurgeChdCache.Left - 12;
            btnPurgeChdCache.Height = 26;
            btnPurgeChdCache.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            btnPurgeChdCache.Click += (s, e) =>
            {
                try
                {
                    string baseTempDir = DB.GetToSortCache()?.FullName ?? Environment.CurrentDirectory;
                    string dir = System.IO.Path.Combine(baseTempDir, "__RomVault.chdscanCache");
                    if (System.IO.Directory.Exists(dir))
                        System.IO.Directory.Delete(dir, true);
                    MessageBox.Show("CHD scan cache purged.", "RomVault", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to purge cache: " + ex.Message, "RomVault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            grpChd.Controls.Add(btnPurgeChdCache);
        }

        private void FrmConfigLoad(object sender, EventArgs e)
        {
            lblDATRoot.Text = Settings.rvSettings.DatRoot;
            cboFixLevel.SelectedIndex = (int)Settings.rvSettings.FixLevel;

            textBox1.Text = "";
            foreach (string file in Settings.rvSettings.IgnoreFiles)
            {
                textBox1.Text += file + Environment.NewLine;
            }
            chkSendFoundMIA.Checked = Settings.rvSettings.MIACallback;
            chkSendFoundMIAAnon.Checked = Settings.rvSettings.MIAAnon;

            chkDetailedReporting.Checked = Settings.rvSettings.DetailedFixReporting;
            chkDoubleCheckDelete.Checked = Settings.rvSettings.DoubleCheckDelete;
            chkCacheSaveTimer.Checked = Settings.rvSettings.CacheSaveTimerEnabled;
            upTime.Value = Settings.rvSettings.CacheSaveTimePeriod;
            chkDebugLogs.Checked = Settings.rvSettings.DebugLogsEnabled;
            chkDeleteOldCueFiles.Checked = Settings.rvSettings.DeleteOldCueFiles;
            cboCores.SelectedIndex = Settings.rvSettings.zstdCompCount >= cboCores.Items.Count ? 0 : Settings.rvSettings.zstdCompCount;
            cbo7zStruct.SelectedIndex = Settings.rvSettings.sevenZDefaultStruct;
            chkDarkMode.Checked = Settings.rvSettings.Darkness;
            chkDoNotReportFeedback.Checked = Settings.rvSettings.DoNotReportFeedback;

            chkChdCache.Checked = Settings.rvSettings.ChdScanCacheEnabled;
            chkChdDebug.Checked = Settings.rvSettings.ChdScanDebugEnabled;
            chkChdStrictCueGdi.Checked = Settings.rvSettings.ChdStrictCueGdi;
            chkChdExportOnFix.Checked = Settings.rvSettings.ChdExportTracksOnFix;
            chkChdStreaming.Checked = Settings.rvSettings.ChdStreamingEnabled;
            chkChdPreferSynthetic.Checked = Settings.rvSettings.ChdPreferSyntheticDescriptor;
            chkChdTrustContainer.Checked = Settings.rvSettings.ChdTrustContainerForTracks;
            upChdDvdHunk.Value = Settings.rvSettings.ChdDvdHunkSizeKiB;
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnOkClick(object sender, EventArgs e)
        {
            Settings.rvSettings.DatRoot = lblDATRoot.Text;
            Settings.rvSettings.FixLevel = (EFixLevel)cboFixLevel.SelectedIndex;
            string strtxt = textBox1.Text;
            strtxt = strtxt.Replace("\r", "");
            string[] strsplit = strtxt.Split('\n');

            Settings.rvSettings.IgnoreFiles = new List<string>(strsplit);
            for (int i = 0; i < Settings.rvSettings.IgnoreFiles.Count; i++)
            {
                Settings.rvSettings.IgnoreFiles[i] = Settings.rvSettings.IgnoreFiles[i].Trim();
                if (string.IsNullOrEmpty(Settings.rvSettings.IgnoreFiles[i]))
                {
                    Settings.rvSettings.IgnoreFiles.RemoveAt(i);
                    i--;
                }
            }
            Settings.rvSettings.SetRegExRules();

            Settings.rvSettings.DetailedFixReporting = chkDetailedReporting.Checked;
            Settings.rvSettings.DoubleCheckDelete = chkDoubleCheckDelete.Checked;
            Settings.rvSettings.DebugLogsEnabled = chkDebugLogs.Checked;
            Settings.rvSettings.CacheSaveTimerEnabled = chkCacheSaveTimer.Checked;
            Settings.rvSettings.CacheSaveTimePeriod = (int)upTime.Value;

            Settings.rvSettings.MIACallback = chkSendFoundMIA.Checked;
            Settings.rvSettings.MIAAnon = chkSendFoundMIAAnon.Checked;
            Settings.rvSettings.DeleteOldCueFiles = chkDeleteOldCueFiles.Checked;

            Settings.rvSettings.zstdCompCount = cboCores.SelectedIndex;

            Settings.rvSettings.sevenZDefaultStruct = cbo7zStruct.SelectedIndex;
            Settings.rvSettings.Darkness = chkDarkMode.Checked;

            Settings.rvSettings.DoNotReportFeedback = chkDoNotReportFeedback.Checked;

            Settings.rvSettings.ChdScanCacheEnabled = chkChdCache.Checked;
            Settings.rvSettings.ChdScanDebugEnabled = chkChdDebug.Checked;
            Settings.rvSettings.ChdStrictCueGdi = chkChdStrictCueGdi.Checked;
            Settings.rvSettings.ChdExportTracksOnFix = chkChdExportOnFix.Checked;
            Settings.rvSettings.ChdStreamingEnabled = chkChdStreaming.Checked;
            Settings.rvSettings.ChdPreferSyntheticDescriptor = chkChdPreferSynthetic.Checked;
            Settings.rvSettings.ChdTrustContainerForTracks = chkChdTrustContainer.Checked;
            Settings.rvSettings.ChdDvdHunkSizeKiB = (int)upChdDvdHunk.Value;

            Settings.WriteConfig(Settings.rvSettings);
            Close();
        }

        private void BtnDatClick(object sender, EventArgs e)
        {
            FolderBrowserDialog browse = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = "Select a folder for DAT Root",
                RootFolder = Environment.SpecialFolder.MyComputer,
                SelectedPath = Settings.rvSettings.DatRoot
            };

            if (browse.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            lblDATRoot.Text = RelativePath.MakeRelative(AppDomain.CurrentDomain.BaseDirectory, browse.SelectedPath);
        }

        private void chkSendFoundMIA_CheckedChanged(object sender, EventArgs e)
        {
            chkSendFoundMIAAnon.Enabled = chkSendFoundMIA.Checked;
        }

    }
}
