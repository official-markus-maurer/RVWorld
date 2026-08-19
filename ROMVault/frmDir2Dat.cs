using DATReader.DatStore;
using DATReader.DatWriter;
using RomVaultCore.ReadDat;
using RomVaultCore.RvDB;
using System;
using System.Windows.Forms;

namespace ROMVault
{
    public partial class frmDir2Dat : Form
    {
        public frmDir2Dat()
        {
            InitializeComponent();
        }

        RvFile _RvFile;
        public void PopulateFrom(RvFile rvFile)
        {
            _RvFile = rvFile;
            RvDat tDat = rvFile.Dat;
            if (tDat == null)
            {
                if (rvFile.DirDatCount == 1)
                    tDat = rvFile.DirDat(0);
            }
            txtDir.Text = rvFile.FullNameCase;
            if (tDat != null)
            {
                txtName.Text = tDat.GetData(RvDat.DatData.DatName);
                txtDescription.Text = tDat.GetData(RvDat.DatData.Description);
                txtVersion.Text = tDat.GetData(RvDat.DatData.Version);
                txtDate.Text = tDat.GetData(RvDat.DatData.Date);
                txtAuthor.Text = tDat.GetData(RvDat.DatData.Author);
                txtHomePage.Text = tDat.GetData(RvDat.DatData.HomePage);
                txtURL.Text = tDat.GetData(RvDat.DatData.URL);
            }
            else
            {
                txtName.Text = "";
                txtDescription.Text = "";
                txtVersion.Text = "";
                txtDate.Text = "";
                txtAuthor.Text = "";
                txtHomePage.Text = "";
                txtURL.Text = "";
            }
        }

        private void btnSaveDat_Click(object sender, EventArgs e)
        {
            ExternalDatConverterTo edct = new ExternalDatConverterTo();
            edct.filterGot = chkFilterGot.Checked;
            edct.filterMissing = chkFilterMissing.Checked;
            edct.filterFixable = chkFilterFixable.Checked;
            edct.filterMIA = chkFilterMIA.Checked;
            edct.filterMerged = chkFilterMerged.Checked;

            edct.filterZIPs = rDatZips.Checked;
            edct.filterFiles = rDatFiles.Checked;

            DatHeader dh = edct.ConvertToExternalDat(_RvFile);
            dh.Name = txtName.Text;
            dh.Description = txtDescription.Text;
            dh.Version = txtVersion.Text;
            dh.Date = txtDate.Text;
            dh.Author = txtAuthor.Text;
            dh.Homepage = txtHomePage.Text;
            dh.URL = txtURL.Text;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Output Dat Name";
            sfd.DefaultExt = "dat";
            sfd.Filter = "dat file (*.dat)|*.dat";
            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;


            string datFilename = sfd.FileName;

            DatXMLWriter.WriteDat(datFilename, dh);
        }

    }
}
