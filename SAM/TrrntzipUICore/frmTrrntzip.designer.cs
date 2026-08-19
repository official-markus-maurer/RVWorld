namespace TrrntZipUICore
{
    partial class FrmTrrntzip
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmTrrntzip));
            splitContainer = new System.Windows.Forms.SplitContainer();
            StatusPanel = new System.Windows.Forms.Panel();
            picRomVault = new System.Windows.Forms.PictureBox();
            btnCancel = new System.Windows.Forms.Button();
            btnPause = new System.Windows.Forms.Button();
            tbProccessors = new System.Windows.Forms.TrackBar();
            picDonate = new System.Windows.Forms.PictureBox();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            chkDryRun = new System.Windows.Forms.CheckBox();
            cboOutType = new System.Windows.Forms.ComboBox();
            cboInType = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            lblTotalStatus = new System.Windows.Forms.Label();
            picTitle = new System.Windows.Forms.PictureBox();
            DropBox = new System.Windows.Forms.PictureBox();
            dataGrid = new System.Windows.Forms.DataGridView();
            FileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            timer1 = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            StatusPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picRomVault).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tbProccessors).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picDonate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picTitle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)DropBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGrid).BeginInit();
            SuspendLayout();
            // 
            // splitContainer
            // 
            splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            splitContainer.IsSplitterFixed = true;
            splitContainer.Location = new System.Drawing.Point(0, 0);
            splitContainer.Margin = new System.Windows.Forms.Padding(4);
            splitContainer.MinimumSize = new System.Drawing.Size(0, 346);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(StatusPanel);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(dataGrid);
            splitContainer.Size = new System.Drawing.Size(915, 416);
            splitContainer.SplitterDistance = 292;
            splitContainer.SplitterWidth = 5;
            splitContainer.TabIndex = 0;
            // 
            // StatusPanel
            // 
            StatusPanel.Controls.Add(picRomVault);
            StatusPanel.Controls.Add(btnCancel);
            StatusPanel.Controls.Add(btnPause);
            StatusPanel.Controls.Add(tbProccessors);
            StatusPanel.Controls.Add(picDonate);
            StatusPanel.Controls.Add(label3);
            StatusPanel.Controls.Add(label2);
            StatusPanel.Controls.Add(chkDryRun);
            StatusPanel.Controls.Add(cboOutType);
            StatusPanel.Controls.Add(cboInType);
            StatusPanel.Controls.Add(label1);
            StatusPanel.Controls.Add(lblTotalStatus);
            StatusPanel.Controls.Add(picTitle);
            StatusPanel.Controls.Add(DropBox);
            StatusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            StatusPanel.Location = new System.Drawing.Point(0, 0);
            StatusPanel.Margin = new System.Windows.Forms.Padding(4);
            StatusPanel.Name = "StatusPanel";
            StatusPanel.Size = new System.Drawing.Size(292, 416);
            StatusPanel.TabIndex = 0;
            // 
            // picRomVault
            // 
            picRomVault.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            picRomVault.Image = (System.Drawing.Image)resources.GetObject("picRomVault.Image");
            picRomVault.Location = new System.Drawing.Point(176, 369);
            picRomVault.Margin = new System.Windows.Forms.Padding(4);
            picRomVault.Name = "picRomVault";
            picRomVault.Size = new System.Drawing.Size(105, 34);
            picRomVault.TabIndex = 18;
            picRomVault.TabStop = false;
            picRomVault.Click += picRomVault_Click;
            // 
            // btnCancel
            // 
            btnCancel.Enabled = false;
            btnCancel.Image = (System.Drawing.Image)resources.GetObject("btnCancel.Image");
            btnCancel.Location = new System.Drawing.Point(247, 112);
            btnCancel.Margin = new System.Windows.Forms.Padding(4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(24, 23);
            btnCancel.TabIndex = 17;
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnPause
            // 
            btnPause.Enabled = false;
            btnPause.Image = (System.Drawing.Image)resources.GetObject("btnPause.Image");
            btnPause.Location = new System.Drawing.Point(219, 112);
            btnPause.Margin = new System.Windows.Forms.Padding(4);
            btnPause.Name = "btnPause";
            btnPause.Size = new System.Drawing.Size(24, 23);
            btnPause.TabIndex = 16;
            btnPause.UseVisualStyleBackColor = true;
            btnPause.Click += btnPause_Click;
            // 
            // tbProccessors
            // 
            tbProccessors.Location = new System.Drawing.Point(14, 218);
            tbProccessors.Margin = new System.Windows.Forms.Padding(4);
            tbProccessors.Name = "tbProccessors";
            tbProccessors.Size = new System.Drawing.Size(258, 45);
            tbProccessors.TabIndex = 15;
            tbProccessors.ValueChanged += tbProccessors_ValueChanged;
            // 
            // picDonate
            // 
            picDonate.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            picDonate.Image = (System.Drawing.Image)resources.GetObject("picDonate.Image");
            picDonate.Location = new System.Drawing.Point(15, 369);
            picDonate.Margin = new System.Windows.Forms.Padding(4);
            picDonate.Name = "picDonate";
            picDonate.Size = new System.Drawing.Size(156, 34);
            picDonate.TabIndex = 13;
            picDonate.TabStop = false;
            picDonate.Click += picDonate_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(93, 169);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(48, 15);
            label3.TabIndex = 12;
            label3.Text = "Output:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(93, 144);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(38, 15);
            label2.TabIndex = 11;
            label2.Text = "Input:";
            // 
            // chkDryRun
            // 
            chkDryRun.AutoSize = true;
            chkDryRun.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            chkDryRun.Location = new System.Drawing.Point(209, 195);
            chkDryRun.Margin = new System.Windows.Forms.Padding(4);
            chkDryRun.Name = "chkDryRun";
            chkDryRun.Size = new System.Drawing.Size(68, 19);
            chkDryRun.TabIndex = 10;
            chkDryRun.Text = "Dry Run";
            chkDryRun.UseVisualStyleBackColor = true;
            chkDryRun.CheckedChanged += chkDryRun_CheckedChanged;
            // 
            // cboOutType
            // 
            cboOutType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboOutType.FormattingEnabled = true;
            cboOutType.Items.AddRange(new object[] { "Zip-Torrent", "Zip-ZSTD", "7Z-ZSTD", "7Z-ZSTD-Solid", "7Z-LZMA", "7Z-LZMA-Solid", "Repair keep original" });
            cboOutType.Location = new System.Drawing.Point(143, 165);
            cboOutType.Margin = new System.Windows.Forms.Padding(4);
            cboOutType.Name = "cboOutType";
            cboOutType.Size = new System.Drawing.Size(140, 23);
            cboOutType.TabIndex = 9;
            cboOutType.TextChanged += cboOutType_TextChanged;
            // 
            // cboInType
            // 
            cboInType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboInType.FormattingEnabled = true;
            cboInType.Items.AddRange(new object[] { "ZIP", "7Z", "ZIP & 7Z", "Files", "Directories", "All" });
            cboInType.Location = new System.Drawing.Point(143, 140);
            cboInType.Margin = new System.Windows.Forms.Padding(4);
            cboInType.Name = "cboInType";
            cboInType.Size = new System.Drawing.Size(140, 23);
            cboInType.TabIndex = 8;
            cboInType.TextChanged += cboInType_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(85, 115);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(130, 15);
            label1.TabIndex = 4;
            label1.Text = "<-- drop Files/Dirs here";
            // 
            // lblTotalStatus
            // 
            lblTotalStatus.Location = new System.Drawing.Point(10, 195);
            lblTotalStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblTotalStatus.Name = "lblTotalStatus";
            lblTotalStatus.Size = new System.Drawing.Size(136, 20);
            lblTotalStatus.TabIndex = 3;
            lblTotalStatus.Text = "(0/0)";
            lblTotalStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // picTitle
            // 
            picTitle.Image = (System.Drawing.Image)resources.GetObject("picTitle.Image");
            picTitle.Location = new System.Drawing.Point(14, 7);
            picTitle.Margin = new System.Windows.Forms.Padding(4);
            picTitle.Name = "picTitle";
            picTitle.Size = new System.Drawing.Size(261, 97);
            picTitle.TabIndex = 2;
            picTitle.TabStop = false;
            picTitle.Click += picTitle_Click;
            // 
            // DropBox
            // 
            DropBox.BackColor = System.Drawing.SystemColors.Control;
            DropBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            DropBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            DropBox.Image = rvImages1.giphy;
            DropBox.InitialImage = null;
            DropBox.Location = new System.Drawing.Point(12, 114);
            DropBox.Margin = new System.Windows.Forms.Padding(4);
            DropBox.Name = "DropBox";
            DropBox.Size = new System.Drawing.Size(73, 70);
            DropBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            DropBox.TabIndex = 0;
            DropBox.TabStop = false;
            // 
            // dataGrid
            // 
            dataGrid.AllowUserToAddRows = false;
            dataGrid.AllowUserToDeleteRows = false;
            dataGrid.AllowUserToResizeRows = false;
            dataGrid.BackgroundColor = System.Drawing.Color.White;
            dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { FileName, Status });
            dataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            dataGrid.Location = new System.Drawing.Point(0, 0);
            dataGrid.Margin = new System.Windows.Forms.Padding(4);
            dataGrid.MultiSelect = false;
            dataGrid.Name = "dataGrid";
            dataGrid.ReadOnly = true;
            dataGrid.RowHeadersVisible = false;
            dataGrid.RowHeadersWidth = 62;
            dataGrid.ShowCellErrors = false;
            dataGrid.ShowEditingIcon = false;
            dataGrid.ShowRowErrors = false;
            dataGrid.Size = new System.Drawing.Size(618, 416);
            dataGrid.TabIndex = 0;
            // 
            // FileName
            // 
            FileName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            FileName.HeaderText = "FileName";
            FileName.MinimumWidth = 200;
            FileName.Name = "FileName";
            FileName.ReadOnly = true;
            // 
            // Status
            // 
            Status.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            Status.HeaderText = "Status";
            Status.MinimumWidth = 8;
            Status.Name = "Status";
            Status.ReadOnly = true;
            Status.Width = 160;
            // 
            // timer1
            // 
            timer1.Tick += timer1_Tick;
            // 
            // FrmTrrntzip
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(915, 416);
            Controls.Add(splitContainer);
            DoubleBuffered = true;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4);
            MinimumSize = new System.Drawing.Size(494, 362);
            Name = "FrmTrrntzip";
            Text = "SAM-UI";
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            StatusPanel.ResumeLayout(false);
            StatusPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picRomVault).EndInit();
            ((System.ComponentModel.ISupportInitialize)tbProccessors).EndInit();
            ((System.ComponentModel.ISupportInitialize)picDonate).EndInit();
            ((System.ComponentModel.ISupportInitialize)picTitle).EndInit();
            ((System.ComponentModel.ISupportInitialize)DropBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGrid).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel StatusPanel;
        private System.Windows.Forms.PictureBox DropBox;
        private System.Windows.Forms.PictureBox picTitle;
        private System.Windows.Forms.Label lblTotalStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dataGrid;
        private System.Windows.Forms.ComboBox cboOutType;
        private System.Windows.Forms.ComboBox cboInType;
        private System.Windows.Forms.CheckBox chkDryRun;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox picDonate;
        private System.Windows.Forms.TrackBar tbProccessors;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.PictureBox picRomVault;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn Status;
        private System.Windows.Forms.Timer timer1;
    }
}

