namespace ROMVault
{
    partial class FrmSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            label1 = new System.Windows.Forms.Label();
            lblDATRoot = new System.Windows.Forms.Label();
            btnDAT = new System.Windows.Forms.Button();
            btnOK = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            textBox1 = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            cboFixLevel = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            chkDebugLogs = new System.Windows.Forms.CheckBox();
            chkCacheSaveTimer = new System.Windows.Forms.CheckBox();
            upTime = new System.Windows.Forms.NumericUpDown();
            label5 = new System.Windows.Forms.Label();
            chkDoubleCheckDelete = new System.Windows.Forms.CheckBox();
            chkDetailedReporting = new System.Windows.Forms.CheckBox();
            chkSendFoundMIA = new System.Windows.Forms.CheckBox();
            chkSendFoundMIAAnon = new System.Windows.Forms.CheckBox();
            chkDeleteOldCueFiles = new System.Windows.Forms.CheckBox();
            label2 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            cbo7zStruct = new System.Windows.Forms.ComboBox();
            cboCores = new System.Windows.Forms.ComboBox();
            chkDarkMode = new System.Windows.Forms.CheckBox();
            chkDoNotReportFeedback = new System.Windows.Forms.CheckBox();
            lblMIADays = new System.Windows.Forms.Label();
            upMIADays = new System.Windows.Forms.NumericUpDown();
            chkShowNewMIA = new System.Windows.Forms.CheckBox();
            lblDays = new System.Windows.Forms.Label();
            grpCoreSettings = new System.Windows.Forms.GroupBox();
            grpCompression = new System.Windows.Forms.GroupBox();
            grpMIA = new System.Windows.Forms.GroupBox();
            grpDatVault = new System.Windows.Forms.GroupBox();
            btnValidate = new System.Windows.Forms.Button();
            picEye = new System.Windows.Forms.PictureBox();
            txtDATVaultKey = new System.Windows.Forms.TextBox();
            lblDatVaultKey = new System.Windows.Forms.Label();
            grpLogging = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)upTime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)upMIADays).BeginInit();
            grpCoreSettings.SuspendLayout();
            grpCompression.SuspendLayout();
            grpMIA.SuspendLayout();
            grpDatVault.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picEye).BeginInit();
            grpLogging.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(77, 20);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(55, 13);
            label1.TabIndex = 0;
            label1.Text = "DATRoot:";
            // 
            // lblDATRoot
            // 
            lblDATRoot.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblDATRoot.BackColor = System.Drawing.Color.White;
            lblDATRoot.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblDATRoot.Location = new System.Drawing.Point(156, 15);
            lblDATRoot.Name = "lblDATRoot";
            lblDATRoot.Size = new System.Drawing.Size(319, 22);
            lblDATRoot.TabIndex = 3;
            lblDATRoot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnDAT
            // 
            btnDAT.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnDAT.Location = new System.Drawing.Point(488, 13);
            btnDAT.Name = "btnDAT";
            btnDAT.Size = new System.Drawing.Size(44, 24);
            btnDAT.TabIndex = 6;
            btnDAT.Text = "Set";
            btnDAT.UseVisualStyleBackColor = true;
            btnDAT.Click += BtnDatClick;
            // 
            // btnOK
            // 
            btnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnOK.Location = new System.Drawing.Point(372, 688);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(89, 23);
            btnOK.TabIndex = 9;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOkClick;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.Location = new System.Drawing.Point(467, 688);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(89, 23);
            btnCancel.TabIndex = 10;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancelClick;
            // 
            // textBox1
            // 
            textBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox1.Location = new System.Drawing.Point(78, 143);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(455, 121);
            textBox1.TabIndex = 12;
            // 
            // label4
            // 
            label4.Location = new System.Drawing.Point(77, 68);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(348, 67);
            label4.TabIndex = 13;
            label4.Text = "Filenames not to remove:\r\n- One rule per line\r\n- Basic rules support * and ? wildcards\r\n- Regex rules must start with regex:'\r\n- Scanning Ignore rules must start with 'ignore:'";
            // 
            // cboFixLevel
            // 
            cboFixLevel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            cboFixLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboFixLevel.FormattingEnabled = true;
            cboFixLevel.Location = new System.Drawing.Point(157, 43);
            cboFixLevel.Name = "cboFixLevel";
            cboFixLevel.Size = new System.Drawing.Size(375, 21);
            cboFixLevel.TabIndex = 14;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(77, 47);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(66, 13);
            label3.TabIndex = 17;
            label3.Text = "Fixing Level:";
            // 
            // chkDebugLogs
            // 
            chkDebugLogs.AutoSize = true;
            chkDebugLogs.Location = new System.Drawing.Point(84, 39);
            chkDebugLogs.Name = "chkDebugLogs";
            chkDebugLogs.Size = new System.Drawing.Size(131, 17);
            chkDebugLogs.TabIndex = 18;
            chkDebugLogs.Text = "Enable Debug logging";
            chkDebugLogs.UseVisualStyleBackColor = true;
            // 
            // chkCacheSaveTimer
            // 
            chkCacheSaveTimer.AutoSize = true;
            chkCacheSaveTimer.Location = new System.Drawing.Point(84, 291);
            chkCacheSaveTimer.Name = "chkCacheSaveTimer";
            chkCacheSaveTimer.Size = new System.Drawing.Size(154, 17);
            chkCacheSaveTimer.TabIndex = 19;
            chkCacheSaveTimer.Text = "Save Cache on timer every";
            chkCacheSaveTimer.UseVisualStyleBackColor = true;
            // 
            // upTime
            // 
            upTime.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            upTime.Location = new System.Drawing.Point(244, 289);
            upTime.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            upTime.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            upTime.Name = "upTime";
            upTime.Size = new System.Drawing.Size(47, 20);
            upTime.TabIndex = 20;
            upTime.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(300, 294);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(44, 13);
            label5.TabIndex = 21;
            label5.Text = "Minutes";
            // 
            // chkDoubleCheckDelete
            // 
            chkDoubleCheckDelete.AutoSize = true;
            chkDoubleCheckDelete.Location = new System.Drawing.Point(84, 272);
            chkDoubleCheckDelete.Name = "chkDoubleCheckDelete";
            chkDoubleCheckDelete.Size = new System.Drawing.Size(262, 17);
            chkDoubleCheckDelete.TabIndex = 22;
            chkDoubleCheckDelete.Text = "Double check file exists elsewhere before deleting";
            chkDoubleCheckDelete.UseVisualStyleBackColor = true;
            // 
            // chkDetailedReporting
            // 
            chkDetailedReporting.AutoSize = true;
            chkDetailedReporting.Location = new System.Drawing.Point(84, 19);
            chkDetailedReporting.Name = "chkDetailedReporting";
            chkDetailedReporting.Size = new System.Drawing.Size(243, 17);
            chkDetailedReporting.TabIndex = 25;
            chkDetailedReporting.Text = "Show detailed actions in Fixing Status window";
            chkDetailedReporting.UseVisualStyleBackColor = true;
            // 
            // chkSendFoundMIA
            // 
            chkSendFoundMIA.AutoSize = true;
            chkSendFoundMIA.Location = new System.Drawing.Point(84, 60);
            chkSendFoundMIA.Name = "chkSendFoundMIA";
            chkSendFoundMIA.Size = new System.Drawing.Size(165, 17);
            chkSendFoundMIA.TabIndex = 27;
            chkSendFoundMIA.Text = "Send Found MIA notifications";
            chkSendFoundMIA.UseVisualStyleBackColor = true;
            chkSendFoundMIA.CheckedChanged += chkSendFoundMIA_CheckedChanged;
            // 
            // chkSendFoundMIAAnon
            // 
            chkSendFoundMIAAnon.AutoSize = true;
            chkSendFoundMIAAnon.Location = new System.Drawing.Point(100, 78);
            chkSendFoundMIAAnon.Name = "chkSendFoundMIAAnon";
            chkSendFoundMIAAnon.Size = new System.Drawing.Size(115, 17);
            chkSendFoundMIAAnon.TabIndex = 28;
            chkSendFoundMIAAnon.Text = "Send anonymously";
            chkSendFoundMIAAnon.UseVisualStyleBackColor = true;
            // 
            // chkDeleteOldCueFiles
            // 
            chkDeleteOldCueFiles.AutoSize = true;
            chkDeleteOldCueFiles.Location = new System.Drawing.Point(84, 39);
            chkDeleteOldCueFiles.Name = "chkDeleteOldCueFiles";
            chkDeleteOldCueFiles.Size = new System.Drawing.Size(208, 17);
            chkDeleteOldCueFiles.TabIndex = 30;
            chkDeleteOldCueFiles.Text = "Delete previous Cue file zips in ToSort ";
            chkDeleteOldCueFiles.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(77, 20);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(102, 13);
            label2.TabIndex = 37;
            label2.Text = "Max ZSTD workers:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(77, 42);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(83, 13);
            label7.TabIndex = 39;
            label7.Text = "Default 7Z type:";
            // 
            // cbo7zStruct
            // 
            cbo7zStruct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbo7zStruct.FormattingEnabled = true;
            cbo7zStruct.Location = new System.Drawing.Point(208, 41);
            cbo7zStruct.Name = "cbo7zStruct";
            cbo7zStruct.Size = new System.Drawing.Size(121, 21);
            cbo7zStruct.TabIndex = 40;
            // 
            // cboCores
            // 
            cboCores.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboCores.FormattingEnabled = true;
            cboCores.Location = new System.Drawing.Point(208, 17);
            cboCores.Name = "cboCores";
            cboCores.Size = new System.Drawing.Size(78, 21);
            cboCores.TabIndex = 41;
            // 
            // chkDarkMode
            // 
            chkDarkMode.AutoSize = true;
            chkDarkMode.Location = new System.Drawing.Point(84, 312);
            chkDarkMode.Name = "chkDarkMode";
            chkDarkMode.Size = new System.Drawing.Size(166, 17);
            chkDarkMode.TabIndex = 42;
            chkDarkMode.Text = "Dark Mode (Restart required.)";
            chkDarkMode.UseVisualStyleBackColor = true;
            // 
            // chkDoNotReportFeedback
            // 
            chkDoNotReportFeedback.AutoSize = true;
            chkDoNotReportFeedback.Location = new System.Drawing.Point(84, 59);
            chkDoNotReportFeedback.Name = "chkDoNotReportFeedback";
            chkDoNotReportFeedback.Size = new System.Drawing.Size(136, 17);
            chkDoNotReportFeedback.TabIndex = 43;
            chkDoNotReportFeedback.Text = "Do not report feedback";
            chkDoNotReportFeedback.UseVisualStyleBackColor = true;
            // 
            // lblMIADays
            // 
            lblMIADays.AutoSize = true;
            lblMIADays.Location = new System.Drawing.Point(101, 20);
            lblMIADays.Name = "lblMIADays";
            lblMIADays.Size = new System.Drawing.Size(165, 13);
            lblMIADays.TabIndex = 45;
            lblMIADays.Text = "Consider files MIA if unfound after";
            // 
            // upMIADays
            // 
            upMIADays.Location = new System.Drawing.Point(270, 19);
            upMIADays.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            upMIADays.Name = "upMIADays";
            upMIADays.Size = new System.Drawing.Size(65, 20);
            upMIADays.TabIndex = 46;
            upMIADays.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // chkShowNewMIA
            // 
            chkShowNewMIA.AutoSize = true;
            chkShowNewMIA.Location = new System.Drawing.Point(84, 41);
            chkShowNewMIA.Name = "chkShowNewMIA";
            chkShowNewMIA.Size = new System.Drawing.Size(180, 17);
            chkShowNewMIA.TabIndex = 47;
            chkShowNewMIA.Text = "Enable Newly Missing rom status";
            chkShowNewMIA.UseVisualStyleBackColor = true;
            // 
            // lblDays
            // 
            lblDays.AutoSize = true;
            lblDays.Location = new System.Drawing.Point(340, 20);
            lblDays.Name = "lblDays";
            lblDays.Size = new System.Drawing.Size(29, 13);
            lblDays.TabIndex = 48;
            lblDays.Text = "days";
            // 
            // grpCoreSettings
            // 
            grpCoreSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpCoreSettings.Controls.Add(label1);
            grpCoreSettings.Controls.Add(label4);
            grpCoreSettings.Controls.Add(textBox1);
            grpCoreSettings.Controls.Add(lblDATRoot);
            grpCoreSettings.Controls.Add(cboFixLevel);
            grpCoreSettings.Controls.Add(label3);
            grpCoreSettings.Controls.Add(chkCacheSaveTimer);
            grpCoreSettings.Controls.Add(chkDarkMode);
            grpCoreSettings.Controls.Add(upTime);
            grpCoreSettings.Controls.Add(label5);
            grpCoreSettings.Controls.Add(chkDoubleCheckDelete);
            grpCoreSettings.Controls.Add(btnDAT);
            grpCoreSettings.Location = new System.Drawing.Point(12, 10);
            grpCoreSettings.Name = "grpCoreSettings";
            grpCoreSettings.Size = new System.Drawing.Size(547, 333);
            grpCoreSettings.TabIndex = 49;
            grpCoreSettings.TabStop = false;
            grpCoreSettings.Text = "Core Settings:";
            // 
            // grpCompression
            // 
            grpCompression.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpCompression.Controls.Add(label2);
            grpCompression.Controls.Add(label7);
            grpCompression.Controls.Add(cbo7zStruct);
            grpCompression.Controls.Add(cboCores);
            grpCompression.Location = new System.Drawing.Point(12, 520);
            grpCompression.Name = "grpCompression";
            grpCompression.Size = new System.Drawing.Size(547, 73);
            grpCompression.TabIndex = 50;
            grpCompression.TabStop = false;
            grpCompression.Text = "Compression:";
            // 
            // grpMIA
            // 
            grpMIA.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpMIA.Controls.Add(lblMIADays);
            grpMIA.Controls.Add(chkSendFoundMIA);
            grpMIA.Controls.Add(chkSendFoundMIAAnon);
            grpMIA.Controls.Add(lblDays);
            grpMIA.Controls.Add(upMIADays);
            grpMIA.Controls.Add(chkShowNewMIA);
            grpMIA.Location = new System.Drawing.Point(12, 415);
            grpMIA.Name = "grpMIA";
            grpMIA.Size = new System.Drawing.Size(547, 99);
            grpMIA.TabIndex = 51;
            grpMIA.TabStop = false;
            grpMIA.Text = "MIA:";
            // 
            // grpDatVault
            // 
            grpDatVault.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpDatVault.Controls.Add(btnValidate);
            grpDatVault.Controls.Add(picEye);
            grpDatVault.Controls.Add(txtDATVaultKey);
            grpDatVault.Controls.Add(lblDatVaultKey);
            grpDatVault.Controls.Add(chkDeleteOldCueFiles);
            grpDatVault.Location = new System.Drawing.Point(12, 347);
            grpDatVault.Name = "grpDatVault";
            grpDatVault.Size = new System.Drawing.Size(547, 64);
            grpDatVault.TabIndex = 52;
            grpDatVault.TabStop = false;
            grpDatVault.Text = "DatVault:";
            // 
            // btnValidate
            // 
            btnValidate.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnValidate.Location = new System.Drawing.Point(384, 13);
            btnValidate.Name = "btnValidate";
            btnValidate.Size = new System.Drawing.Size(89, 23);
            btnValidate.TabIndex = 41;
            btnValidate.Text = "Validate Online";
            btnValidate.UseVisualStyleBackColor = true;
            btnValidate.Click += btnValidate_Click;
            // 
            // picEye
            // 
            picEye.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            picEye.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picEye.Image = rvImages1.eye;
            picEye.Location = new System.Drawing.Point(346, 12);
            picEye.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            picEye.Name = "picEye";
            picEye.Size = new System.Drawing.Size(24, 22);
            picEye.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            picEye.TabIndex = 40;
            picEye.TabStop = false;
            picEye.Click += picEye_Click;
            // 
            // txtDATVaultKey
            // 
            txtDATVaultKey.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            txtDATVaultKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtDATVaultKey.Location = new System.Drawing.Point(156, 13);
            txtDATVaultKey.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtDATVaultKey.Name = "txtDATVaultKey";
            txtDATVaultKey.PasswordChar = '*';
            txtDATVaultKey.Size = new System.Drawing.Size(191, 20);
            txtDATVaultKey.TabIndex = 39;
            // 
            // lblDatVaultKey
            // 
            lblDatVaultKey.AutoSize = true;
            lblDatVaultKey.Location = new System.Drawing.Point(78, 16);
            lblDatVaultKey.Name = "lblDatVaultKey";
            lblDatVaultKey.Size = new System.Drawing.Size(72, 13);
            lblDatVaultKey.TabIndex = 38;
            lblDatVaultKey.Text = "DatVault Key:";
            // 
            // grpLogging
            // 
            grpLogging.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpLogging.Controls.Add(chkDetailedReporting);
            grpLogging.Controls.Add(chkDebugLogs);
            grpLogging.Controls.Add(chkDoNotReportFeedback);
            grpLogging.Location = new System.Drawing.Point(12, 595);
            grpLogging.Name = "grpLogging";
            grpLogging.Size = new System.Drawing.Size(547, 83);
            grpLogging.TabIndex = 53;
            grpLogging.TabStop = false;
            grpLogging.Text = "Logging:";
            // 
            // FrmSettings
            // 
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(568, 720);
            Controls.Add(grpLogging);
            Controls.Add(grpDatVault);
            Controls.Add(grpMIA);
            Controls.Add(grpCompression);
            Controls.Add(grpCoreSettings);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "FrmSettings";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RomVault Settings";
            Load += FrmConfigLoad;
            ((System.ComponentModel.ISupportInitialize)upTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)upMIADays).EndInit();
            grpCoreSettings.ResumeLayout(false);
            grpCoreSettings.PerformLayout();
            grpCompression.ResumeLayout(false);
            grpCompression.PerformLayout();
            grpMIA.ResumeLayout(false);
            grpMIA.PerformLayout();
            grpDatVault.ResumeLayout(false);
            grpDatVault.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picEye).EndInit();
            grpLogging.ResumeLayout(false);
            grpLogging.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblDATRoot;
        private System.Windows.Forms.Button btnDAT;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox cboFixLevel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkDebugLogs;
        private System.Windows.Forms.CheckBox chkCacheSaveTimer;
        private System.Windows.Forms.NumericUpDown upTime;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkDoubleCheckDelete;
        private System.Windows.Forms.CheckBox chkDetailedReporting;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox chkSendFoundMIA;
        private System.Windows.Forms.CheckBox chkSendFoundMIAAnon;
        private System.Windows.Forms.CheckBox chkDeleteOldCueFiles;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbo7zStruct;
        private System.Windows.Forms.ComboBox cboCores;
        private System.Windows.Forms.CheckBox chkDarkMode;
        private System.Windows.Forms.CheckBox chkDoNotReportFeedback;
        private System.Windows.Forms.Label lblMIADays;
        private System.Windows.Forms.NumericUpDown upMIADays;
        private System.Windows.Forms.CheckBox chkShowNewMIA;
        private System.Windows.Forms.Label lblDays;
        private System.Windows.Forms.GroupBox grpCoreSettings;
        private System.Windows.Forms.GroupBox grpCompression;
        private System.Windows.Forms.GroupBox grpMIA;
        private System.Windows.Forms.GroupBox grpDatVault;
        private System.Windows.Forms.GroupBox grpLogging;
        private System.Windows.Forms.Label lblDatVaultKey;
        private System.Windows.Forms.PictureBox picEye;
        private System.Windows.Forms.TextBox txtDATVaultKey;
        private System.Windows.Forms.Button btnValidate;
    }
}