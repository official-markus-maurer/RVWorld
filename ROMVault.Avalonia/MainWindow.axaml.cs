using Avalonia.Controls;
using Avalonia.Interactivity;
using RomVaultCore.RvDB;
using RomVaultCore;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using System;
using RomVaultCore.Utils;
using RomVaultCore.Scanner;
using RomVaultCore.ReadDat;
using RomVaultCore.FindFix;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Diagnostics;
using System.IO;
using DATReader.DatStore;
using DATReader.DatWriter;
using Avalonia.Media.Imaging;
using Compress;
using Compress.ZipFile;
using System.Text.RegularExpressions;
using Path = System.IO.Path;
using File = System.IO.File;

namespace ROMVault.Avalonia;

public partial class MainWindow : Window
{
    private RvFile? _gameGridSource;
    private bool _updatingGameGrid;
    private bool _working = false;
    private GridLength _lastArtworkWidth = new GridLength(300);

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize Tree
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        if (rvTree != null)
        {
            rvTree.Setup(DB.DirRoot);
            rvTree.RvSelected += OnRvTreeSelected;
            rvTree.RvChecked += (s, e) =>
            {
                RepairStatus.ReportStatusReset(DB.DirRoot);
                DatSetSelected(rvTree.Selected);
            };
            rvTree.RvRightClicked += (s, e) =>
            {
                var contextMenu = this.FindControl<ContextMenu>("TreeContextMenu");
                contextMenu?.Open(rvTree);
            };
        }

        // Initialize Events
        chkBoxShowComplete.Click += (s, e) => UpdateGameGrid();
        chkBoxShowPartial.Click += (s, e) => UpdateGameGrid();
        chkBoxShowFixes.Click += (s, e) => UpdateGameGrid();
        chkBoxShowMIA.Click += (s, e) => UpdateGameGrid();
        chkBoxShowMerged.Click += (s, e) => UpdateGameGrid();
        chkBoxShowEmpty.Click += (s, e) => UpdateGameGrid();
        
        txtFilter.TextChanged += (s, e) => UpdateGameGrid();
        btnClear.Click += (s, e) => { txtFilter.Text = ""; };

        GameGrid.SelectionChanged += GameGrid_SelectionChanged;
        GameGrid.DoubleTapped += GameGrid_DoubleTapped;
    }

    private void GameGrid_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (GameGrid.SelectedItem is RvFile tGame && tGame.FileType == FileType.Dir)
        {
            var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
            if (rvTree != null)
            {
                rvTree.SetSelected(tGame);
            }
        }
    }

    private void OnRvTreeSelected(object? sender, RvFile e)
    {
        DatSetSelected(e);
    }

    private void DatSetSelected(RvFile? cf)
    {
        if (cf == null) return;

        // Populate Dat Info
        if (cf.Dat != null)
        {
            lblDITName.Text = cf.Dat.GetData(RvDat.DatData.DatName);
            lblDITDescription.Text = cf.Dat.GetData(RvDat.DatData.Description);
            lblDITCategory.Text = cf.Dat.GetData(RvDat.DatData.Category);
            lblDITVersion.Text = cf.Dat.GetData(RvDat.DatData.Version);
            lblDITAuthor.Text = cf.Dat.GetData(RvDat.DatData.Author);
            lblDITDate.Text = cf.Dat.GetData(RvDat.DatData.Date);
            // lblDITPath.Text = cf.Dat.GetData(RvDat.DatData.RootDir); // Assuming logic
        }
        else
        {
            lblDITName.Text = "";
            lblDITDescription.Text = "";
            lblDITCategory.Text = "";
            lblDITVersion.Text = "";
            lblDITAuthor.Text = "";
            lblDITDate.Text = "";
        }
        lblDITPath.Text = cf.FullName;

        // Populate Stats
        lblDITRomsGot.Text = cf.DirStatus.CountCorrect().ToString();
        lblDITRomsMissing.Text = cf.DirStatus.CountMissing().ToString();
        lblDITRomsFixable.Text = cf.DirStatus.CountCanBeFixed().ToString();
        lblDITRomsUnknown.Text = cf.DirStatus.CountUnknown().ToString();

        UpdateGameGrid(cf);
    }

    private void UpdateGameGrid(RvFile? tDir = null)
    {
        if (tDir != null)
        {
            _gameGridSource = tDir;
        }

        if (_gameGridSource == null) return;

        _updatingGameGrid = true;
        
        var gameList = new List<RvFile>();
        string searchLowerCase = txtFilter.Text?.ToLower() ?? "";

        for (int j = 0; j < _gameGridSource.ChildCount; j++)
        {
            RvFile tChildDir = _gameGridSource.Child(j);
            if (!tChildDir.IsDirectory) continue;

            if (searchLowerCase.Length > 0 && !tChildDir.Name.ToLower().Contains(searchLowerCase))
                continue;

            ReportStatus tDirStat = tChildDir.DirStatus;

            bool gCorrect = tDirStat.HasCorrect();
            bool gMissing = tDirStat.HasMissing(false);
            bool gUnknown = tDirStat.HasUnknown();
            bool gInToSort = tDirStat.HasInToSort();
            bool gFixes = tDirStat.HasFixesNeeded();
            bool gMIA = tDirStat.HasMIA();
            bool gAllMerged = tDirStat.HasAllMerged();

            bool show = (chkBoxShowComplete.IsChecked == true && gCorrect && !gMissing && !gFixes);
            show = show || (chkBoxShowPartial.IsChecked == true && gMissing && gCorrect);
            show = show || (chkBoxShowEmpty.IsChecked == true && gMissing && !gCorrect);
            show = show || (chkBoxShowFixes.IsChecked == true && gFixes);
            show = show || (chkBoxShowMIA.IsChecked == true && gMIA);
            show = show || (chkBoxShowMerged.IsChecked == true && gAllMerged);
            show = show || gUnknown;
            show = show || gInToSort;
            show = show || tChildDir.GotStatus == GotStatus.Corrupt;
            show = show || !(gCorrect || gMissing || gUnknown || gInToSort || gFixes || gMIA || gAllMerged);

            if (show)
            {
                gameList.Add(tChildDir);
            }
        }

        GameGrid.ItemsSource = gameList;
        _updatingGameGrid = false;
        
        if (gameList.Count > 0)
        {
            // GameGrid.SelectedIndex = 0; // Optional: Select first item
        }
        else
        {
            RomGrid.ItemsSource = null;
        }
    }

    private void GameGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingGameGrid) return;
        if (GameGrid.SelectedItem is RvFile tGame)
        {
             UpdateGameMetaData(tGame);
             UpdateRomGrid(tGame);
             UpdateArtworkVisibility(tGame);
        }
        else
        {
             UpdateGameMetaData(null);
             RomGrid.ItemsSource = null;
             HideAllArtworkTabs();
        }
    }

    private void UpdateGameMetaData(RvFile? tGame)
    {
        var lblGameName = this.FindControl<TextBox>("lblGameName");
        
        var lblGameDescriptionLabel = this.FindControl<TextBlock>("lblGameDescriptionLabel");
        var lblGameDescription = this.FindControl<TextBox>("lblGameDescription");
        
        var lblGameManufacturerLabel = this.FindControl<TextBlock>("lblGameManufacturerLabel");
        var lblGameManufacturer = this.FindControl<TextBox>("lblGameManufacturer");
        
        var lblGameCloneOfLabel = this.FindControl<TextBlock>("lblGameCloneOfLabel");
        var lblGameCloneOf = this.FindControl<TextBox>("lblGameCloneOf");
        
        var lblGameRomOfLabel = this.FindControl<TextBlock>("lblGameRomOfLabel");
        var lblGameRomOf = this.FindControl<TextBox>("lblGameRomOf");
        
        var lblGameYearLabel = this.FindControl<TextBlock>("lblGameYearLabel");
        var lblGameYear = this.FindControl<TextBox>("lblGameYear");
        
        var lblGameCategoryLabel = this.FindControl<TextBlock>("lblGameCategoryLabel");
        var lblGameCategory = this.FindControl<TextBox>("lblGameCategory");

        void SetVisible(bool visible, params Control?[] controls)
        {
            foreach (var c in controls)
            {
                if (c != null) c.IsVisible = visible;
            }
        }

        if (tGame == null)
        {
            if (lblGameName != null) lblGameName.Text = "";
            SetVisible(false, lblGameDescriptionLabel, lblGameDescription, 
                              lblGameManufacturerLabel, lblGameManufacturer,
                              lblGameCloneOfLabel, lblGameCloneOf,
                              lblGameRomOfLabel, lblGameRomOf,
                              lblGameYearLabel, lblGameYear,
                              lblGameCategoryLabel, lblGameCategory);
            return;
        }

        if (lblGameName != null)
        {
            string gameId = tGame.Game?.GetData(RvGame.GameData.Id) ?? "";
            lblGameName.Text = tGame.Name + (!string.IsNullOrWhiteSpace(gameId) ? $" (ID:{gameId})" : "");
        }

        if (tGame.Game != null)
        {
            // For now, treating EmuArc same as Standard for basic fields, 
            // as EmuArc in WinForms also shows Description, CloneOf, RomOf, Year, Category (mostly).
            // WinForms hides Manufacturer for EmuArc, but shows others.
            // We will show what we have available if it's not empty, simplifying logic.
            
            bool isEmuArc = tGame.Game.GetData(RvGame.GameData.EmuArc) == "yes";

            // Description
            string desc = tGame.Game.GetData(RvGame.GameData.Description);
            if (desc == "¤") desc = Path.GetFileNameWithoutExtension(tGame.Name);
            if (lblGameDescription != null) lblGameDescription.Text = desc;
            SetVisible(true, lblGameDescriptionLabel, lblGameDescription);

            // Manufacturer
            string manu = tGame.Game.GetData(RvGame.GameData.Manufacturer);
            if (lblGameManufacturer != null) lblGameManufacturer.Text = manu;
            SetVisible(!isEmuArc && !string.IsNullOrEmpty(manu), lblGameManufacturerLabel, lblGameManufacturer);

            // CloneOf
            string clone = tGame.Game.GetData(RvGame.GameData.CloneOf);
            if (lblGameCloneOf != null) lblGameCloneOf.Text = clone;
            SetVisible(!string.IsNullOrEmpty(clone), lblGameCloneOfLabel, lblGameCloneOf);

            // RomOf
            string romOf = tGame.Game.GetData(RvGame.GameData.RomOf);
            if (lblGameRomOf != null) lblGameRomOf.Text = romOf;
            SetVisible(!string.IsNullOrEmpty(romOf), lblGameRomOfLabel, lblGameRomOf);

            // Year
            string year = tGame.Game.GetData(RvGame.GameData.Year);
            if (lblGameYear != null) lblGameYear.Text = year;
            SetVisible(!string.IsNullOrEmpty(year), lblGameYearLabel, lblGameYear);

            // Category
            string cat = tGame.Game.GetData(RvGame.GameData.Category);
            if (lblGameCategory != null) lblGameCategory.Text = cat;
            SetVisible(!string.IsNullOrEmpty(cat), lblGameCategoryLabel, lblGameCategory);
        }
        else
        {
            SetVisible(false, lblGameDescriptionLabel, lblGameDescription, 
                              lblGameManufacturerLabel, lblGameManufacturer,
                              lblGameCloneOfLabel, lblGameCloneOf,
                              lblGameRomOfLabel, lblGameRomOf,
                              lblGameYearLabel, lblGameYear,
                              lblGameCategoryLabel, lblGameCategory);
        }
    }

    private void HideAllArtworkTabs()
    {
        if (GameListGrid != null && GameListGrid.ColumnDefinitions[2].Width.Value > 0)
        {
             _lastArtworkWidth = GameListGrid.ColumnDefinitions[2].Width;
        }

        TabArtwork.IsVisible = false;
        TabMedium.IsVisible = false;
        TabScreens.IsVisible = false;
        TabInfo.IsVisible = false;
        TabInfo2.IsVisible = false;

        if (ArtworkSplitter != null) ArtworkSplitter.IsVisible = false;
        if (ArtworkTabs != null) ArtworkTabs.IsVisible = false;
        if (GameListGrid != null) GameListGrid.ColumnDefinitions[2].Width = new GridLength(0);
    }

    private void ShowArtworkSection()
    {
        if (ArtworkSplitter != null) ArtworkSplitter.IsVisible = true;
        if (ArtworkTabs != null) ArtworkTabs.IsVisible = true;
        if (GameListGrid != null) GameListGrid.ColumnDefinitions[2].Width = _lastArtworkWidth.Value > 0 ? _lastArtworkWidth : new GridLength(300);
    }

    private void UpdateArtworkVisibility(RvFile tGame)
    {
        HideAllArtworkTabs();

        if (tGame == null) return;
        if (tGame.Parent == null) return;

        bool found = false;

        if (tGame.Game != null && tGame.Game.GetData(RvGame.GameData.EmuArc) == "yes")
        {
             LoadTruRipPannel(tGame);
             return;
        }

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
    }

    private void LoadMamePannels(RvFile tGame, string extraPath)
    {
        string[] path = extraPath.Split('\\');
        RvFile fExtra = DB.DirRoot.Child(0);

        foreach (string p in path)
        {
            if (fExtra.ChildNameSearch(FileType.Dir, p, out int pIndex) != 0)
                return;
            fExtra = fExtra.Child(pIndex);
        }

        bool artLoaded = false;
        bool logoLoaded = false;
        bool titleLoaded = false;
        bool screenLoaded = false;
        int index;

        if (fExtra.ChildNameSearch(FileType.Zip, "artpreview.zip", out index) == 0)
            artLoaded = TryLoadImage(picArtwork, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
        else if (fExtra.ChildNameSearch(FileType.Dir, "artpreviewsnap", out index) == 0)
            artLoaded = TryLoadImage(picArtwork, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));

        if (fExtra.ChildNameSearch(FileType.Zip, "marquees.zip", out index) == 0)
            logoLoaded = TryLoadImage(picLogo, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
        else if (fExtra.ChildNameSearch(FileType.Dir, "marquees", out index) == 0)
            logoLoaded = TryLoadImage(picLogo, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));

        if (fExtra.ChildNameSearch(FileType.Zip, "snap.zip", out index) == 0)
            screenLoaded = TryLoadImage(picScreenShot, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
        else if (fExtra.ChildNameSearch(FileType.Dir, "snap", out index) == 0)
            screenLoaded = TryLoadImage(picScreenShot, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));

        if (fExtra.ChildNameSearch(FileType.Zip, "cabinets.zip", out index) == 0)
            titleLoaded = TryLoadImage(picScreenTitle, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));
        else if (fExtra.ChildNameSearch(FileType.Dir, "cabinets", out index) == 0)
            titleLoaded = TryLoadImage(picScreenTitle, fExtra.Child(index), Path.GetFileNameWithoutExtension(tGame.Name));

        if (artLoaded || logoLoaded) TabArtwork.IsVisible = true;
        if (titleLoaded || screenLoaded) TabScreens.IsVisible = true;

        if (artLoaded || logoLoaded || titleLoaded || screenLoaded)
        {
            ShowArtworkSection();
        }
    }

    private void LoadMameSLPannels(RvFile tGame, string extraPath)
    {
        string[] path = extraPath.Split('\\');
        RvFile fExtra = DB.DirRoot.Child(0);

        foreach (string p in path)
        {
            if (fExtra.ChildNameSearch(FileType.Dir, p, out int pIndex) != 0)
                return;
            fExtra = fExtra.Child(pIndex);
        }

        bool artLoaded = false;
        bool logoLoaded = false;
        bool screenLoaded = false;
        int index;

        string fname = tGame.Parent.Name + "/" + Path.GetFileNameWithoutExtension(tGame.Name);

        if (fExtra.ChildNameSearch(FileType.Zip, "covers_SL.zip", out index) == 0)
            artLoaded = TryLoadImage(picArtwork, fExtra.Child(index), fname);

        if (fExtra.ChildNameSearch(FileType.Zip, "snap_SL.zip", out index) == 0)
            logoLoaded = TryLoadImage(picLogo, fExtra.Child(index), fname);

        if (fExtra.ChildNameSearch(FileType.Zip, "titles_SL.zip", out index) == 0)
            screenLoaded = TryLoadImage(picScreenShot, fExtra.Child(index), fname);

        if (artLoaded || logoLoaded) TabArtwork.IsVisible = true;
        if (screenLoaded) TabScreens.IsVisible = true;

        if (artLoaded || logoLoaded || screenLoaded)
        {
            ShowArtworkSection();
        }
    }

    private void LoadTruRipPannel(RvFile tGame)
    {
        bool artLoaded = TryLoadImage(picArtwork, tGame, "Artwork/artwork_front");
        bool logoLoaded = TryLoadImage(picLogo, tGame, "Artwork/logo");
        if (!logoLoaded)
            logoLoaded = TryLoadImage(picArtwork, tGame, "Artwork/artwork_back");

        bool medium1Loaded = TryLoadImage(picMedium1, tGame, "Artwork/medium_front*");
        bool medium2Loaded = TryLoadImage(picMedium2, tGame, "Artwork/medium_back*");
        bool titleLoaded = TryLoadImage(picScreenTitle, tGame, "Artwork/screentitle");
        bool screenLoaded = TryLoadImage(picScreenShot, tGame, "Artwork/screenshot");
        bool storyLoaded = LoadText(txtInfo, tGame, "Artwork/story.txt");

        if (artLoaded || logoLoaded) TabArtwork.IsVisible = true;
        if (medium1Loaded || medium2Loaded) TabMedium.IsVisible = true;
        if (titleLoaded || screenLoaded) TabScreens.IsVisible = true;
        if (storyLoaded) { TabInfo.Header = "Info"; TabInfo.IsVisible = true; }

        if (artLoaded || logoLoaded || medium1Loaded || medium2Loaded || titleLoaded || screenLoaded || storyLoaded)
        {
            ShowArtworkSection();
        }
    }

    private bool LoadC64Pannel(RvFile tGame)
    {
        bool artLoaded = TryLoadImage(picArtwork, tGame, "Front");
        bool logoLoaded = TryLoadImage(picLogo, tGame, "Extras/Cassette");
        bool titleLoaded = TryLoadImage(picScreenTitle, tGame, "Extras/Inlay");
        bool screenLoaded = TryLoadImage(picScreenShot, tGame, "Extras/Inlay_back");

        if (artLoaded || logoLoaded) TabArtwork.IsVisible = true;
        if (titleLoaded || screenLoaded) TabScreens.IsVisible = true;

        if (artLoaded || logoLoaded || titleLoaded || screenLoaded)
        {
            ShowArtworkSection();
            return true;
        }
        return false;
    }

    private bool LoadNFOPannel(RvFile tGame)
    {
        bool storyLoaded = LoadNFO(txtInfo, tGame, "*.nfo");
        if (storyLoaded)
        {
            TabInfo.Header = "NFO";
            TabInfo.IsVisible = true;
        }

        bool storyLoaded2 = LoadNFO(txtInfo2, tGame, "*.diz");
        if (storyLoaded2)
        {
            TabInfo2.Header = "DIZ";
            TabInfo2.IsVisible = true;
        }
        
        if (storyLoaded || storyLoaded2)
        {
            ShowArtworkSection();
            return true;
        }
        return false;
    }

    // --- Helpers ---

    private bool TryLoadImage(global::Avalonia.Controls.Image pic, RvFile tGame, string filename)
    {
        return LoadImage(pic, tGame, filename + ".png") || LoadImage(pic, tGame, filename + ".jpg");
    }

    private bool LoadImage(global::Avalonia.Controls.Image picBox, RvFile tGame, string filename)
    {
        picBox.Source = null;
        if (!LoadBytes(tGame, filename, out byte[] memBuffer))
            return false;
        
        try
        {
            using (MemoryStream ms = new MemoryStream(memBuffer))
            {
                picBox.Source = new Bitmap(ms);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool LoadText(TextBox txtBox, RvFile tGame, string filename)
    {
        txtBox.Text = "";
        if (!LoadBytes(tGame, filename, out byte[] memBuffer))
            return false;

        try
        {
            string txt = System.Text.Encoding.ASCII.GetString(memBuffer);
            txt = txt.Replace("\r\n", "\r\n\r\n");
            txtBox.Text = txt;
            return true;
        }
        catch { return false; }
    }

    private bool LoadNFO(TextBox txtBox, RvFile tGame, string search)
    {
        if (!LoadBytes(tGame, search, out byte[] memBuffer))
            return false;

        try
        {
            // Try to use CodePage 437 if available, else ASCII
            string txt;
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                txt = System.Text.Encoding.GetEncoding(437).GetString(memBuffer);
            }
            catch
            {
                txt = System.Text.Encoding.ASCII.GetString(memBuffer);
            }
            
            txt = txt.Replace("\r\n", "\n");
            txt = txt.Replace("\r", "\n");
            txt = txt.Replace("\n", "\r\n");
            txtBox.Text = txt;
            return true;
        }
        catch { return false; }
    }

    private static Regex WildcardToRegex(string pattern)
    {
        if (pattern.ToLower().StartsWith("regex:"))
            return new Regex(pattern.Substring(6), RegexOptions.IgnoreCase);

        return new Regex("^" + Regex.Escape(pattern).
        Replace("\\*", ".*").
        Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
    }

    private static bool LoadBytes(RvFile tGame, string filename, out byte[] memBuffer)
    {
        memBuffer = Array.Empty<byte>();

        Regex rSearch = WildcardToRegex(filename);

        int cCount = tGame.ChildCount;
        if (cCount == 0)
            return false;

        int found = -1;
        for (int i = 0; i < cCount; i++)
        {
            RvFile rvf = tGame.Child(i);
            if (rvf.GotStatus != GotStatus.Got)
                continue;
            if (!rSearch.IsMatch(rvf.Name)) 
                continue;
            found = i;
            break;
        }

        if (found == -1)
            return false;

        try
        {
            switch (tGame.FileType)
            {
                case FileType.Zip:
                    {
                        RvFile imagefile = tGame.Child(found);
                        if (imagefile.ZipFileHeaderPosition == null)
                            return false;

                        Zip zf = new Zip();
                        if (zf.ZipFileOpen(tGame.FullNameCase, tGame.FileModTimeStamp, false) != ZipReturn.ZipGood)
                            return false;

                        if (zf.ZipFileOpenReadStreamFromLocalHeaderPointer((ulong)imagefile.ZipFileHeaderPosition, false,
                                out Stream stream, out ulong streamSize, out ushort _) != ZipReturn.ZipGood)
                        {
                            zf.ZipFileClose();
                            return false;
                        }

                        memBuffer = new byte[streamSize];
                        int bytesRead = 0;
                        while (bytesRead < (int)streamSize)
                        {
                            int read = stream.Read(memBuffer, bytesRead, (int)streamSize - bytesRead);
                            if (read == 0) break;
                            bytesRead += read;
                        }
                        zf.ZipFileClose();
                        return true;
                    }
                case FileType.Dir:
                    {
                        RvFile imagefile = tGame.Child(found);
                        string artwork = imagefile.FullNameCase;
                        if (!File.Exists(artwork))
                            return false;

                        using (FileStream stream = new FileStream(artwork, FileMode.Open, FileAccess.Read))
                        {
                            memBuffer = new byte[stream.Length];
                            int bytesRead = 0;
                            while (bytesRead < memBuffer.Length)
                            {
                                int read = stream.Read(memBuffer, bytesRead, memBuffer.Length - bytesRead);
                                if (read == 0) break;
                                bytesRead += read;
                            }
                        }
                        return true;
                    }
                default:
                    return false;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    private void UpdateRomGrid(RvFile tGame)
    {
        var fileList = new List<RvFile>();
        AddDir(tGame, "", ref fileList);
        RomGrid.ItemsSource = fileList;
    }

    private void AddDir(RvFile tGame, string pathAdd, ref List<RvFile> fileList)
    {
        if (tGame == null) return;

        try
        {
            for (int l = 0; l < tGame.ChildCount; l++)
            {
                RvFile tBase = tGame.Child(l);
                RvFile tFile = tBase;

                if (tFile.IsFile)
                {
                    AddRom(tFile, pathAdd, ref fileList);
                }

                if (tGame.Dat == null) continue;

                RvFile tDir = tBase;
                if (!tDir.IsDirectory) continue;

                if (tDir.Game == null)
                {
                    AddDir(tDir, pathAdd + tDir.Name + "/", ref fileList);
                }
            }
        }
        catch { }
    }

    private void AddRom(RvFile tFile, string pathAdd, ref List<RvFile> fileList)
    {
        try
        {
            if (tFile.DatStatus != DatStatus.InDatMerged || tFile.RepStatus != RepStatus.NotCollected ||
                chkBoxShowMerged.IsChecked == true)
            {
                tFile.UiDisplayName = pathAdd + tFile.Name;
                fileList.Add(tFile);
            }
        }
        catch { }
    }

    // Context Menu Handlers
    private void OnTreeScanClick(object? sender, RoutedEventArgs e)
    {
        if (_working) return;
        EScanLevel scanLevel = EScanLevel.Level2;
        if (sender is MenuItem menuItem && menuItem.Tag is string level)
        {
             Enum.TryParse(level, out scanLevel);
        }
        
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        ScanRoms(scanLevel, rvTree?.Selected);
    }

    private async void OnSetDirDatSettingsClick(object? sender, RoutedEventArgs e) 
    {
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        var selected = rvTree?.Selected;
        if (selected != null)
        {
             var win = new Views.DirectorySettingsWindow();
             win.SetLocation(selected.TreeFullName);
             win.SetDisplayType(true);
             await win.ShowDialog(this);
             
             if (win.ChangesMade)
             {
                 UpdateDats();
             }
        }
    }
    private async void OnSetDirMappingsClick(object? sender, RoutedEventArgs e) 
    {
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        var selected = rvTree?.Selected;
        if (selected != null)
        {
             var win = new Views.DirectoryMappingsWindow();
             win.SetLocation(selected.TreeFullName);
             win.SetDisplayType(true);
             await win.ShowDialog(this);
        }
    }

    private async void OnGlobalDirMappingsClick(object? sender, RoutedEventArgs e)
    {
         if (_working) return;
         var win = new Views.DirectoryMappingsWindow();
         win.SetDisplayType(false);
         await win.ShowDialog(this);
    }

    private void OnOpenDirectoryClick(object? sender, RoutedEventArgs e) 
    {
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        var selected = rvTree?.Selected;
        if (selected != null)
        {
             string tDir = selected.FullName;
             if (Directory.Exists(tDir))
             {
                 try 
                 { 
                     Process.Start(new ProcessStartInfo
                     {
                         FileName = tDir,
                         UseShellExecute = true,
                         Verb = "open"
                     }); 
                 } catch { }
             }
        }
    }
    private async void OnSaveFixDatsClick(object? sender, RoutedEventArgs e) 
    {
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        var selected = rvTree?.Selected;
        if (selected != null)
        {
             await Code.Report.CreateFixDat(this, selected, true);
        }
    }
    private async void OnSaveFullDatClick(object? sender, RoutedEventArgs e) 
    {
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        var selected = rvTree?.Selected;
        if (selected == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save an Dat File",
            SuggestedFileName = selected.Name,
            DefaultExtension = "dat",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("DAT file") { Patterns = new[] { "*.dat" } }
            }
        });

        if (file != null)
        {
             DatHeader dh = (new ExternalDatConverterTo()).ConvertToExternalDat(selected);
             using (var stream = await file.OpenWriteAsync())
             {
                 DatXMLWriter.WriteDat(stream, dh);
             }
        }
    }

    private void OnInstanceCountPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var textBlock = sender as TextBlock;
        var rvFile = textBlock?.DataContext as RvFile;
        if (rvFile != null)
        {
            var win = new Views.RomInfoWindow();
            win.SetRom(rvFile);
            win.ShowDialog(this);
        }
    }


    private void OnGameGridScanClick(object? sender, RoutedEventArgs e)
    {
        if (_working) return;
        EScanLevel scanLevel = EScanLevel.Level2;
        if (sender is MenuItem menuItem && menuItem.Tag is string level)
        {
             Enum.TryParse(level, out scanLevel);
        }
        
        if (GameGrid.SelectedItem is RvFile selected)
        {
            ScanRoms(scanLevel, selected);
        }
        else
        {
            ScanRoms(scanLevel);
        }
    }

    private void OnGameGridOpenDirClick(object? sender, RoutedEventArgs e) 
    { 
        if (GameGrid.SelectedItem is RvFile thisFile)
        {
            if (thisFile.FileType == FileType.Dir)
            {
                string folderPath = thisFile.FullNameCase;
                if (Directory.Exists(folderPath))
                {
                     try 
                     { 
                         Process.Start(new ProcessStartInfo
                         {
                             FileName = folderPath,
                             UseShellExecute = true,
                             Verb = "open"
                         }); 
                     } catch { }
                }
            }
            else if (thisFile.FileType == FileType.Zip || thisFile.FileType == FileType.SevenZip)
            {
                string zipPath = thisFile.FullNameCase;
                if (File.Exists(zipPath))
                {
                     try 
                     { 
                         Process.Start(new ProcessStartInfo
                         {
                             FileName = zipPath,
                             UseShellExecute = true,
                             Verb = "open"
                         }); 
                     } catch { }
                }
            }
        }
    }
    private void OnGameGridOpenParentClick(object? sender, RoutedEventArgs e) 
    { 
        if (GameGrid.SelectedItem is RvFile thisFile)
        {
            var parent = thisFile.Parent;
            if (parent != null && parent.FileType == FileType.Dir)
            {
                string folderPath = parent.FullNameCase;
                if (Directory.Exists(folderPath))
                {
                     try 
                     { 
                         Process.Start(new ProcessStartInfo
                         {
                             FileName = folderPath,
                             UseShellExecute = true,
                             Verb = "open"
                         }); 
                     } catch { }
                }
            }
        }
    }
    private void OnLaunchEmulatorClick(object? sender, RoutedEventArgs e) 
    { 
        if (GameGrid.SelectedItem is RvFile tGame)
        {
            LaunchEmulator(tGame);
        }
    }
    private void OnOpenWebPageClick(object? sender, RoutedEventArgs e) 
    { 
        if (GameGrid.SelectedItem is RvFile thisGame)
        {
            if (thisGame.Game != null && thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "No-Intro")
            {
                string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
                string datId = thisGame.Dat.GetData(RvDat.DatData.Id);
                if (!string.IsNullOrWhiteSpace(gameId) && !string.IsNullOrWhiteSpace(datId))
                    try { Process.Start(new ProcessStartInfo { FileName = $"https://datomatic.no-intro.org/index.php?page=show_record&s={datId}&n={gameId}", UseShellExecute = true }); } catch { }
            }
            if (thisGame.Game != null && thisGame.Dat?.GetData(RvDat.DatData.HomePage) == "redump.org")
            {
                string gameId = thisGame.Game.GetData(RvGame.GameData.Id);
                if (!string.IsNullOrWhiteSpace(gameId))
                    try { Process.Start(new ProcessStartInfo { FileName = $"http://redump.org/disc/{gameId}/", UseShellExecute = true }); } catch { }
            }
        }
    }

    // Main Menu / Toolbar Handlers
    private void OnUpdateNewDatsClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        UpdateDats();
    }

    private void OnUpdateAllDatsClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        DatUpdate.CheckAllDats(DB.DirRoot.Child(0), @"DatRoot\");
        UpdateDats();
    }

    private void OnScanRomsClick(object? sender, RoutedEventArgs e) 
    {
         if (_working) return;
         EScanLevel scanLevel = EScanLevel.Level2;
         if (sender is MenuItem menuItem && menuItem.Tag is string level)
        {
            Enum.TryParse(level, out scanLevel);
        }
        ScanRoms(scanLevel);
    }
    private void OnFixRomsClick(object? sender, RoutedEventArgs e) { }
    private async void OnFixDatReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.CreateFixDat(this, DB.DirRoot.Child(0), true);
    }
    
    private async void OnFullReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.GenerateReport(this);
    }
    
    private async void OnFixReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.GenerateFixReport(this);
    }
    private async void OnGlobalDirDatSettingsClick(object? sender, RoutedEventArgs e)
    {
         if (_working) return;
         var win = new Views.DirectorySettingsWindow();
         win.SetLocation("RomVault");
         win.SetDisplayType(false);
         await win.ShowDialog(this);
         
         if (win.ChangesMade)
         {
             UpdateDats();
         }
    }

    private async void OnRomVaultSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (_working) return;
        var win = new Views.SettingsWindow();
        await win.ShowDialog(this);
    }
    private async void OnAddToSortClick(object? sender, RoutedEventArgs e)
    {
        if (_working) return;
        
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select new ToSort Folder",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        string selectedPath = folders[0].Path.LocalPath;

        string relPath = RelativePath.MakeRelative(AppDomain.CurrentDomain.BaseDirectory, selectedPath);

        RvFile ts = new RvFile(FileType.Dir)
        {
            Name = relPath,
            DatStatus = DatStatus.InToSort,
            Tree = new RvTreeRow()
        };
        ts.Tree.SetChecked(RvTreeRow.TreeSelect.Locked, false);

        DB.DirRoot.ChildAdd(ts, DB.DirRoot.ChildCount);

        RepairStatus.ReportStatusReset(DB.DirRoot);
        
        // Refresh Tree
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        if (rvTree != null)
        {
             rvTree.Setup(DB.DirRoot);
             rvTree.SetSelected(ts);
        }
        
        DatSetSelected(ts);
        DB.Write();
    }
    private void OnHelpTorrentZipClick(object? sender, RoutedEventArgs e) 
    {
        var win = new Views.TrrntZipWindow();
        win.Show();
    }
    private void OnHelpWikiClick(object? sender, RoutedEventArgs e) 
    {
        try { Process.Start(new ProcessStartInfo { FileName = "https://wiki.romvault.com/doku.php?id=help", UseShellExecute = true }); } catch { }
    }
    private void OnHelpColorKeyClick(object? sender, RoutedEventArgs e) 
    {
        var win = new Views.KeyWindow();
        win.Show(this);
    }
    private void OnHelpWhatsNewClick(object? sender, RoutedEventArgs e) 
    {
        try { Process.Start(new ProcessStartInfo { FileName = "https://wiki.romvault.com/doku.php?id=whats_new", UseShellExecute = true }); } catch { }
    }
    private void OnHelpAboutClick(object? sender, RoutedEventArgs e) 
    {
        var win = new Views.HelpAboutWindow();
        win.ShowDialog(this);
    }

    private void OnUpdateDatsClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        UpdateDats();
    }

    private void OnFindFixesClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        FindFixes();
    }
    
    private void OnFixFilesClick(object? sender, RoutedEventArgs e) 
    {
         if (_working) return;
         FixFiles();
    }
    private async void OnReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.CreateFixDat(this, DB.DirRoot.Child(0), true);
    }

    private void LaunchEmulator(RvFile tGame)
    {
        EmulatorInfo? ei = FindEmulatorInfo(tGame);
        if (ei == null)
            return;

        string commandLineOptions = ei.CommandLine;
        string dirname = tGame.Parent.FullName;
        if (dirname.StartsWith("RomRoot\\"))
             dirname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dirname);

        commandLineOptions = commandLineOptions.Replace("{gamename}", Path.GetFileNameWithoutExtension(tGame.Name));
        commandLineOptions = commandLineOptions.Replace("{gamefilename}", tGame.Name);
        commandLineOptions = commandLineOptions.Replace("{gamedirectory}", dirname);

        string? workingDir = ei.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(workingDir))
            workingDir = Path.GetDirectoryName(ei.ExeName);

        if (workingDir == null) return;

        using (Process exeProcess = new Process())
        {
            exeProcess.StartInfo.WorkingDirectory = workingDir;
            exeProcess.StartInfo.FileName = ei.ExeName;
            exeProcess.StartInfo.Arguments = commandLineOptions;
            exeProcess.StartInfo.UseShellExecute = false;
            exeProcess.StartInfo.CreateNoWindow = true;
            exeProcess.Start();
        }
    }

    private EmulatorInfo? FindEmulatorInfo(RvFile tGame)
    {
        string path = tGame.Parent.DatTreeFullName;
        if (Settings.rvSettings?.EInfo == null)
            return null;
        if (path == "Error")
            return null;
        if (path.Length <= 8)
            return null;

        foreach (EmulatorInfo ei in Settings.rvSettings.EInfo)
        {
            if (!string.Equals(path.Substring(8), ei.TreeDir, StringComparison.CurrentCultureIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(ei.CommandLine))
                continue;

            if (!File.Exists(ei.ExeName))
                continue;
            return ei;
        }
        return null;
    }

    // Worker Functions

    private void Start()
    {
        _working = true;
        this.Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Wait);
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        if (rvTree != null) rvTree.Working = true;
        // TODO: Disable other UI controls
    }

    private void Finish()
    {
        _working = false;
        this.Cursor = global::Avalonia.Input.Cursor.Default;
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        if (rvTree != null)
        {
            rvTree.Working = false;
            DatSetSelected(rvTree.Selected);
        }
    }

    public void ScanRoms(EScanLevel sd, RvFile? StartAt = null)
    {
        FileScanning.StartAt = StartAt;
        FileScanning.EScanLevel = sd;
        
        Start();
        
        var thWrk = new ThreadWorker(FileScanning.ScanFiles);
        
        var progressWindow = new Views.ProgressWindow(thWrk);
        progressWindow.Title = "Scanning Roms";
        progressWindow.ShowDialog(this);
        
        thWrk.wFinal += OnScanFinal;
        thWrk.StartAsync();
    }

    private void OnScanReport(object obj)
    {
        // Handled by ProgressWindow
    }

    private void OnScanFinal()
    {
        Dispatcher.UIThread.Post(() => {
            Finish();
        });
    }

    public void UpdateDats()
    {
        // Preserve selection
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        RvFile? selected = rvTree?.Selected;
        List<RvFile> parents = new List<RvFile>();
        while (selected != null)
        {
            parents.Add(selected);
            selected = selected.Parent;
        }

        Start();

        var thWrk = new ThreadWorker(DatUpdate.UpdateDat);
        
        var progressWindow = new Views.ProgressWindow(thWrk);
        progressWindow.Title = "Updating Dats";
        progressWindow.ShowDialog(this);

        thWrk.wFinal += () => {
             Dispatcher.UIThread.Post(() => {
                // Rebuild Tree
                if (rvTree != null) rvTree.Setup(DB.DirRoot);

                // Restore selection
                while (parents.Count > 1 && parents[0].Parent == null)
                    parents.RemoveAt(0);

                if (parents.Count > 0)
                    selected = parents[0];
                else
                    selected = null;

                if (rvTree != null)
                {
                    rvTree.SetSelected(selected);
                }
                
                Finish();
                
                // Extra Finish steps for UpdateDats
                 DatSetSelected(selected);
            });
        };
        thWrk.StartAsync();
    }

    public void FindFixes()
    {
        Start();
        var thWrk = new ThreadWorker(RomVaultCore.FindFix.FindFixes.ScanFiles);
        
        var progressWindow = new Views.ProgressWindow(thWrk);
        progressWindow.Title = "Finding Fixes";
        progressWindow.ShowDialog(this);

        thWrk.wFinal += OnScanFinal;
        thWrk.StartAsync();
    }

    public void FixFiles()
    {
        Start();
        var thWrk = new ThreadWorker(RomVaultCore.FixFile.Fix.PerformFixes);
        
        var progressWindow = new Views.ProgressWindow(thWrk);
        progressWindow.Title = "Fixing Files";
        progressWindow.ShowDialog(this);
        
        thWrk.wFinal += OnScanFinal;
        thWrk.StartAsync();
    }

}