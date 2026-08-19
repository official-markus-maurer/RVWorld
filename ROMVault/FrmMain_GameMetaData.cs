using System;
using System.Drawing;
using System.Windows.Forms;
using RomVaultCore;
using RomVaultCore.RvDB;
using RVIO;

namespace ROMVault
{
    public partial class FrmMain
    {
        private Label _labelGameName;
        private TextBox _textGameName;
        private Label _buttonGameName;

        private Label _labelGameDescription;
        private TextBox _textGameDescription;


        private Label _labelGameManufacturer;
        private TextBox _textGameManufacturer;

        private Label _labelGameCloneOf;
        private TextBox _textGameCloneOf;
        private Label _buttonGameCloneOf;

        private Label _labelGameRomOf;
        private TextBox _textGameRomOf;

        private Label _labelGameCategory;
        private TextBox _textGameCategory;

        private Label _labelGameSerial;
        private TextBox _textGameSerial;

        private Label _labelGameYear;
        private TextBox _textGameYear;

        private Label _labelGameVersion;
        private TextBox _textGameVersion;

        //Trurip Extra Data
        private Label _labelTruripPublisher;
        private TextBox _textTruripPublisher;

        private Label _labelTruripDeveloper;
        private TextBox _textTruripDeveloper;

        private Label _labelTruripTitleId;
        private TextBox _textTruripTitleId;

        private Label _labelTruripSource;
        private TextBox _textTruripSource;

        private Label _labelTruripCloneOf;
        private TextBox _textTruripCloneOf;

        private Label _labelTruripRelatedTo;
        private TextBox _textTruripRelatedTo;


        private Label _labelTruripYear;
        private TextBox _textTruripYear;

        private Label _labelTruripPlayers;
        private TextBox _textTruripPlayers;


        private Label _labelTruripGenre;
        private TextBox _textTruripGenre;

        private Label _labelTruripSubGenre;
        private TextBox _textTruripSubGenre;


        private Label _labelTruripRatings;
        private TextBox _textTruripRatings;

        private Label _labelTruripScore;
        private TextBox _textTruripScore;



        private void AddTextBox(int line, string name, int x, int x1, out Label lBox, out TextBox tBox)
        {
            int y = 14 + line * 16;

            lBox = new Label
            {
                Location = SPoint(x, y + 1),
                Size = SSize(x1 - x - 2, 13),
                Text = name + @" :",
                TextAlign = ContentAlignment.TopRight
            };
            tBox = new TextBox
            {
                AutoSize = false,
                Location = SPoint(x1, y),
                Size = SSize(20, 17),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                TabStop = false
            };
            gbSetInfo.Controls.Add(lBox);
            gbSetInfo.Controls.Add(tBox);
        }


        private Point SPoint(int x, int y)
        {
            return new Point((int)(x * _scaleFactorX), (int)(y * _scaleFactorY));
        }

        private Size SSize(int x, int y)
        {
            return new Size((int)(x * _scaleFactorX), (int)(y * _scaleFactorY));
        }

        private static Bitmap BitmapLink = null;
        private Bitmap scale()
        {
            if (BitmapLink != null)
                return BitmapLink;

            Bitmap linkImg = rvImages1.link;
            int targetHeight = 14;
            int targetWidth = (int)((float)linkImg.Width / linkImg.Height * targetHeight);
            BitmapLink = new Bitmap(linkImg, new Size(targetWidth, targetHeight));
            return BitmapLink;
        }

        private Label AddLinkLabel()
        {
            Label button = new Label
            {
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                ForeColor = Color.Blue,
                Image = scale(),
                ImageAlign = ContentAlignment.MiddleRight
            };
            button.Font = new Font(button.Font, button.Font.Style | FontStyle.Underline);
            gbSetInfo.Controls.Add(button);
            return button;
        }

        private void AddGameMetaData()
        {
            _buttonGameName = AddLinkLabel();
            AddTextBox(0, "Name", 6, 84, out _labelGameName, out _textGameName);

            AddTextBox(1, "Description", 6, 84, out _labelGameDescription, out _textGameDescription);
            AddTextBox(2, "Manufacturer", 6, 84, out _labelGameManufacturer, out _textGameManufacturer);

            _buttonGameCloneOf = AddLinkLabel();
            AddTextBox(3, "Clone of", 6, 84, out _labelGameCloneOf, out _textGameCloneOf);

            AddTextBox(4, "Rom of", 6, 84, out _labelGameRomOf, out _textGameRomOf);

            AddTextBox(5, "Category", 6, 84, out _labelGameCategory, out _textGameCategory);
            AddTextBox(5, "Serial / Game ID", 160, 264, out _labelGameSerial, out _textGameSerial);

            AddTextBox(6, "Year", 6, 84, out _labelGameYear, out _textGameYear);
            AddTextBox(6, "Version", 160, 264, out _labelGameVersion, out _textGameVersion);

            //Trurip

            AddTextBox(2, "Publisher", 6, 84, out _labelTruripPublisher, out _textTruripPublisher);
            AddTextBox(2, "Title ID", 406, 484, out _labelTruripTitleId, out _textTruripTitleId);

            AddTextBox(3, "Developer", 6, 84, out _labelTruripDeveloper, out _textTruripDeveloper);
            AddTextBox(3, "Source", 406, 484, out _labelTruripSource, out _textTruripSource);

            AddTextBox(4, "Clone of", 6, 84, out _labelTruripCloneOf, out _textTruripCloneOf);
            AddTextBox(5, "Related to", 6, 84, out _labelTruripRelatedTo, out _textTruripRelatedTo);

            AddTextBox(6, "Year", 6, 84, out _labelTruripYear, out _textTruripYear);
            AddTextBox(6, "Genre", 206, 284, out _labelTruripGenre, out _textTruripGenre);
            AddTextBox(6, "Ratings", 406, 484, out _labelTruripRatings, out _textTruripRatings);

            AddTextBox(7, "Players", 6, 84, out _labelTruripPlayers, out _textTruripPlayers);
            AddTextBox(7, "SubGenre", 206, 284, out _labelTruripSubGenre, out _textTruripSubGenre);
            AddTextBox(7, "Score", 406, 484, out _labelTruripScore, out _textTruripScore);


            gbSetInfo_Resize(null, new EventArgs());
            UpdateGameMetaData(new RvFile(FileType.Dir));

            _buttonGameName.Click += buttonLink_Click;
            _buttonGameCloneOf.Click += buttonLink_Click;

        }

        private void buttonLink_Click(object sender, EventArgs e)
        {

            string thisURL = ((Label)sender).Tag.ToString();
            if (!string.IsNullOrWhiteSpace(thisURL))
                RVProcess.StartURL(thisURL);
        }

        private void SetLinkButton(RvFile tGame)
        {
            string gameId = tGame.Game?.GetData(RvGame.GameData.Id);
            string homepage = tGame.Dat?.GetData(RvDat.DatData.HomePage);
            if (!string.IsNullOrWhiteSpace(gameId))
            {
                string datId = tGame.Dat?.GetData(RvDat.DatData.Id);
                if (homepage == "No-Intro" && !string.IsNullOrWhiteSpace(datId))
                {
                    _buttonGameName.Text = $"ID: {gameId}";
                    _buttonGameName.Tag = $"https://datomatic.no-intro.org/index.php?page=show_record&s={datId}&n={gameId}";
                    _buttonGameName.Visible = true;
                    return;
                }
                if (homepage == "redump.info")
                {
                    _buttonGameName.Text = $"ID: {gameId}";
                    _buttonGameName.Tag = $"https://redump.info/disc/{gameId}";
                    _buttonGameName.Visible = true;
                    return;
                }
                _textGameName.Text += $" (ID: {gameId})";
                _buttonGameName.Visible = false;
                return;
            }

            if (homepage == "redump.org")
            {
                //https://forum.redump.info/viewtopic.php?p=117559
                _buttonGameName.Text = $"Obsolete";
                _buttonGameName.Tag = $"https://www.romvault.com/redump.html";
                _buttonGameName.Visible = true;
                return;
            }
            _buttonGameName.Visible = false;
        }

        private void SetCloneLinkButton(RvFile tGame)
        {
            string cloneOfId = tGame.Game?.GetData(RvGame.GameData.CloneOfId);
            if (!string.IsNullOrWhiteSpace(cloneOfId))
            {
                string homepage = tGame.Dat?.GetData(RvDat.DatData.HomePage);
                string datId = tGame.Dat?.GetData(RvDat.DatData.Id);
                if (homepage == "No-Intro" && !string.IsNullOrWhiteSpace(datId))
                {
                    _buttonGameCloneOf.Text = $"ID: {cloneOfId}";
                    _buttonGameCloneOf.Tag = $"https://datomatic.no-intro.org/index.php?page=show_record&s={datId}&n={cloneOfId}";
                    _buttonGameCloneOf.Visible = true;
                    return;
                }
            }
            _buttonGameCloneOf.Visible = false;
        }

        private void UpdateGameMetaData(RvFile tGame)
        {
            _textGameName.Text = tGame.Name;
            SetLinkButton(tGame);
            SetCloneLinkButton(tGame);

            if (tGame.Game == null)
            {
                SetDescriptionVisible(false);
            }

            if (tGame.Game == null || tGame.Game.GetData(RvGame.GameData.EmuArc) != "yes")
            {
                SetTruripVisible(false);
            }

            if (tGame.Game == null || tGame.Game.GetData(RvGame.GameData.EmuArc) == "yes")
            {
                SetMainVisible(false);
            }


            if (tGame.Game != null)
            {
                if (tGame.Game.GetData(RvGame.GameData.EmuArc) == "yes")
                {
                    string desc = tGame.Game.GetData(RvGame.GameData.Description);
                    if (desc == "¤") desc = Path.GetFileNameWithoutExtension(tGame.Name);
                    _textGameDescription.Text = desc;
                    SetDescriptionVisible(true);

                    _textTruripPublisher.Text = tGame.Game.GetData(RvGame.GameData.Publisher);
                    _textTruripDeveloper.Text = tGame.Game.GetData(RvGame.GameData.Developer);
                    _textTruripTitleId.Text = tGame.Game.GetData(RvGame.GameData.Id);
                    _textTruripSource.Text = tGame.Game.GetData(RvGame.GameData.Source);
                    _textTruripCloneOf.Text = tGame.Game.GetData(RvGame.GameData.CloneOf);
                    _textTruripRelatedTo.Text = tGame.Game.GetData(RvGame.GameData.RelatedTo);
                    _textTruripYear.Text = tGame.Game.GetData(RvGame.GameData.Year);
                    _textTruripPlayers.Text = tGame.Game.GetData(RvGame.GameData.Players);
                    _textTruripGenre.Text = tGame.Game.GetData(RvGame.GameData.Genre);
                    _textTruripSubGenre.Text = tGame.Game.GetData(RvGame.GameData.SubGenre);
                    _textTruripRatings.Text = tGame.Game.GetData(RvGame.GameData.Ratings);
                    _textTruripScore.Text = tGame.Game.GetData(RvGame.GameData.Score);
                    SetTruripVisible(true);

                    LoadTruRipPannel(tGame);
                }
                else
                {
                    _labelGameDescription.Visible = true;
                    _textGameDescription.Visible = true;
                    string desc = tGame.Game.GetData(RvGame.GameData.Description);
                    if (desc == "¤") desc = Path.GetFileNameWithoutExtension(tGame.Name);
                    _textGameDescription.Text = desc;

                    _textGameManufacturer.Text = tGame.Game.GetData(RvGame.GameData.Manufacturer);
                    _textGameCloneOf.Text = tGame.Game.GetData(RvGame.GameData.CloneOf);
                    _textGameRomOf.Text = tGame.Game.GetData(RvGame.GameData.RomOf);
                    _textGameCategory.Text = tGame.Game.GetData(RvGame.GameData.Category);
                    _textGameSerial.Text = tGame.Game.GetData(RvGame.GameData.Serial) + tGame.Game.GetData(RvGame.GameData.GameId);
                    _textGameYear.Text = tGame.Game.GetData(RvGame.GameData.Year);
                    _textGameVersion.Text = tGame.Game.GetData(RvGame.GameData.Version);

                    SetMainVisible(true);



                    bool found = false;
                    string path = tGame.Parent.DatTreeFullName;
                    foreach (EmulatorInfo ei in Settings.rvSettings.EInfo)
                    {
                        if (path.Length <= 8)
                            continue;

                        if (!string.Equals(path.Substring(8), ei.TreeDir, StringComparison.CurrentCultureIgnoreCase))
                            continue;

                        if (string.IsNullOrWhiteSpace(ei.ExtraPath))
                            continue;

                        if (ei.ExtraPath != null)
                        {
                            found = true;
                            if (ei.ExtraPath.Substring(0, 1) == "%")
                                LoadMameSLPannels(tGame, ei.ExtraPath.Substring(1));
                            else
                                LoadMamePannels(tGame, ei.ExtraPath);

                            break;
                        }
                    }

                    if (!found)
                        found = LoadNFOPannel(tGame);

                    if (!found)
                        found = LoadC64Pannel(tGame);

                    if (!found)
                        HidePannel();

                }
            }
            else
            {
                HidePannel();
            }

            this.ActiveControl = GameGrid;
        }


        private void SetDescriptionVisible(bool value)
        {
            _labelGameDescription.Visible = value;
            _textGameDescription.Visible = value;
        }

        private void SetMainVisible(bool value)
        {
            _labelGameManufacturer.Visible = value;
            _textGameManufacturer.Visible = value;

            _labelGameCloneOf.Visible = value;
            _textGameCloneOf.Visible = value;

            _labelGameRomOf.Visible = value;
            _textGameRomOf.Visible = value;

            _labelGameCategory.Visible = value;
            _textGameCategory.Visible = value;

            _labelGameSerial.Visible = value;
            _textGameSerial.Visible = value;

            _labelGameYear.Visible = value;
            _textGameYear.Visible = value;

            _labelGameVersion.Visible = value;
            _textGameVersion.Visible = value;
        }

        private void SetTruripVisible(bool value)
        {
            _labelTruripPublisher.Visible = value;
            _textTruripPublisher.Visible = value;

            _labelTruripDeveloper.Visible = value;
            _textTruripDeveloper.Visible = value;

            _labelTruripTitleId.Visible = value;
            _textTruripTitleId.Visible = value;

            _labelTruripSource.Visible = value;
            _textTruripSource.Visible = value;

            _labelTruripCloneOf.Visible = value;
            _textTruripCloneOf.Visible = value;

            _labelTruripRelatedTo.Visible = value;
            _textTruripRelatedTo.Visible = value;

            _labelTruripYear.Visible = value;
            _textTruripYear.Visible = value;

            _labelTruripPlayers.Visible = value;
            _textTruripPlayers.Visible = value;

            _labelTruripGenre.Visible = value;
            _textTruripGenre.Visible = value;

            _labelTruripSubGenre.Visible = value;
            _textTruripSubGenre.Visible = value;

            _labelTruripRatings.Visible = value;
            _textTruripRatings.Visible = value;

            _labelTruripScore.Visible = value;
            _textTruripScore.Visible = value;
        }

        private void gbSetInfo_Resize(object sender, EventArgs e)
        {
            const int leftPos = 84;
            int rightPos = gbSetInfo.Width - 15;
            if (rightPos > 750)
            {
                rightPos = 750;
            }

            int width = rightPos - leftPos;


            if (_textGameName == null)
            {
                return;
            }

            // main Meta Data
            int textWidth = (int)((double)width * 120 / 340);
            int text2Left = leftPos + width - textWidth;
            int label2Left = text2Left - 110;

            _textGameName.Width = width;

            _buttonGameName.Width = 80;
            _buttonGameName.Height = _textGameName.Height;
            _buttonGameName.Left = _textGameName.Left + _textGameName.Width - 80;
            _buttonGameName.Top = _textGameName.Top;


            _textGameDescription.Width = width;
            _textGameManufacturer.Width = width;


            _textGameCloneOf.Width = width;

            _buttonGameCloneOf.Width = 80;
            _buttonGameCloneOf.Height = _textGameCloneOf.Height;
            _buttonGameCloneOf.Left = _textGameCloneOf.Left + _textGameCloneOf.Width - 80;
            _buttonGameCloneOf.Top = _textGameCloneOf.Top;


            _textGameRomOf.Width = width;

            _textGameCategory.Width = textWidth;

            _labelGameSerial.Left = label2Left;
            _textGameSerial.Left = text2Left;
            _textGameSerial.Width = textWidth;

            _textGameYear.Width = textWidth;

            _labelGameVersion.Left = label2Left;
            _textGameVersion.Left = text2Left;
            _textGameVersion.Width = textWidth;



            // TruRip Meta Data
            textWidth = (int)(width * 0.20);
            text2Left = (int)(width * 0.4 + leftPos);
            label2Left = text2Left - 78;
            int text3Left = leftPos + width - textWidth;
            int label3Left = text3Left - 78;

            _textTruripPublisher.Width = (int)(width * 0.6);
            _textTruripDeveloper.Width = (int)(width * 0.6);
            _textTruripCloneOf.Width = width;
            _textTruripRelatedTo.Width = width;

            _textTruripYear.Width = textWidth;
            _textTruripPlayers.Width = textWidth;

            _labelTruripGenre.Left = label2Left;
            _textTruripGenre.Left = text2Left;
            _textTruripGenre.Width = textWidth;

            _labelTruripSubGenre.Left = label2Left;
            _textTruripSubGenre.Left = text2Left;
            _textTruripSubGenre.Width = textWidth;


            _labelTruripTitleId.Left = label3Left;
            _textTruripTitleId.Left = text3Left;
            _textTruripTitleId.Width = textWidth;

            _labelTruripSource.Left = label3Left;
            _textTruripSource.Left = text3Left;
            _textTruripSource.Width = textWidth;

            _labelTruripRatings.Left = label3Left;
            _textTruripRatings.Left = text3Left;
            _textTruripRatings.Width = textWidth;

            _labelTruripScore.Left = label3Left;
            _textTruripScore.Left = text3Left;
            _textTruripScore.Width = textWidth;
        }
    }
}
