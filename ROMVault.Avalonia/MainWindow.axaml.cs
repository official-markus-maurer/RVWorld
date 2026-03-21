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

/// <summary>
/// The main window of the ROMVault Avalonia application.
/// Handles the primary UI logic, including the directory tree, game grid, and main menu actions.
/// </summary>
public partial class MainWindow : Window
{
    private RvFile? _gameGridSource;
    private bool _updatingGameGrid;
    private bool _working = false;
    private GridLength _lastArtworkWidth = new GridLength(300);

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// Sets up the directory tree, event handlers, and initial status aggregation.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        if (lblStatusLeft != null) lblStatusLeft.Text = "";
        if (lblStatusRight != null) lblStatusRight.Text = "";
        
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

        // Ensure status is calculated
        if (DB.DirRoot != null)
        {
             // Force initialization of RepairStatus
             RepairStatus.InitStatusCheck();
             // Force aggregation of DirStatus since it's not persisted and RepStatusReset might skip it
             AggregateDirStatus(DB.DirRoot);
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

    /// <summary>
    /// Recursively aggregates directory status counts from children to parents.
    /// This is necessary because DirStatus is not persisted and needs to be recalculated on load.
    /// </summary>
    /// <param name="dir">The directory to process.</param>
    private void AggregateDirStatus(RvFile dir)
    {
        if (dir.ChildCount == 0) return;

        // Reset the DirStatus for the current directory
        // Since we don't have a Clear method, we assume it's fresh (all zeros) on load.
        // If this is called multiple times, counts would be wrong, but we only call it once on startup.

        for (int i = 0; i < dir.ChildCount; i++)
        {
            RvFile child = dir.Child(i);
            
            if (child.IsDirectory)
            {
                AggregateDirStatus(child);
            }

            // Manually add child status to parent (dir)
            dir.DirStatus.RepStatusAddRemove(child.RepStatus, 1);
            
            if (child.IsDirectory)
            {
                dir.DirStatus.RepStatusArrayAddRemove(child.DirStatus, 1);
            }
        }
    }

    /// <summary>
    /// Handles the selection event from the RvTree control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The selected RvFile.</param>
    private void OnRvTreeSelected(object? sender, RvFile e)
    {
        DatSetSelected(e);
    }

    /// <summary>
    /// Updates the UI when a DAT or Directory is selected in the tree.
    /// Populates the DAT Info panel and updates the game grid.
    /// </summary>
    /// <param name="cf">The selected RvFile (Directory or DAT).</param>
    private void DatSetSelected(RvFile? cf)
    {
        if (cf == null) return;

        if (lblStatusLeft != null)
        {
            lblStatusLeft.Text = cf.FullName;
        }

        // Populate Dat Info
        if (cf.Dat != null)
        {
            lblDITName.Text = cf.Dat.GetData(RvDat.DatData.DatName);
            lblDITDescription.Text = cf.Dat.GetData(RvDat.DatData.Description);
            lblDITCategory.Text = cf.Dat.GetData(RvDat.DatData.Category);
            lblDITVersion.Text = cf.Dat.GetData(RvDat.DatData.Version);
            lblDITAuthor.Text = cf.Dat.GetData(RvDat.DatData.Author);
            lblDITDate.Text = cf.Dat.GetData(RvDat.DatData.Date);
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

        // Populate Stats
        lblDITRomsGot.Text = cf.DirStatus.CountCorrect().ToString();
        lblDITRomsMissing.Text = cf.DirStatus.CountMissing().ToString();
        lblDITRomsFixable.Text = cf.DirStatus.CountCanBeFixed().ToString();
        lblDITRomsUnknown.Text = cf.DirStatus.CountUnknown().ToString();

        UpdateGameGrid(cf);
    }

    /// <summary>
    /// Updates the Game Grid (main list of games/files) based on the selected directory and filters.
    /// </summary>
    /// <param name="tDir">The directory to display. If null, uses the previously selected directory.</param>
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
        bool showDescriptionColumn = false;

        for (int j = 0; j < _gameGridSource.ChildCount; j++)
        {
            RvFile tChildDir = _gameGridSource.Child(j);
            if (!tChildDir.IsDirectory) continue;

            if (searchLowerCase.Length > 0 && !tChildDir.Name.ToLower().Contains(searchLowerCase))
                continue;

            if (!showDescriptionColumn && tChildDir.Game != null)
            {
                string desc = tChildDir.Game.GetData(RvGame.GameData.Description);
                if (!string.IsNullOrWhiteSpace(desc) && desc != "¤")
                {
                    showDescriptionColumn = true;
                }
            }

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

        var gameDescColumn = GameGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Description", StringComparison.Ordinal));
        if (gameDescColumn != null) gameDescColumn.IsVisible = showDescriptionColumn;

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

    /// <summary>
    /// Handles the selection change event in the Game Grid.
    /// Updates the metadata panel, ROM grid, and artwork based on the selected game.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
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

    /// <summary>
    /// Updates the Game Metadata panel (description, manufacturer, year, etc.) for the selected game.
    /// </summary>
    /// <param name="tGame">The selected game file.</param>
    private void UpdateGameMetaData(RvFile? tGame)
    {
        var lblGameName = this.FindControl<TextBox>("lblGameName");
        
        var lblGameDescriptionLabel = this.FindControl<TextBlock>("lblGameDescriptionLabel");
        var lblGameDescription = this.FindControl<TextBlock>("lblGameDescription");
        
        var lblGameManufacturerLabel = this.FindControl<TextBlock>("lblGameManufacturerLabel");
        var lblGameManufacturer = this.FindControl<TextBlock>("lblGameManufacturer");
        
        var lblGameCloneOfLabel = this.FindControl<TextBlock>("lblGameCloneOfLabel");
        var lblGameCloneOf = this.FindControl<TextBlock>("lblGameCloneOf");
        
        var lblGameRomOfLabel = this.FindControl<TextBlock>("lblGameRomOfLabel");
        var lblGameRomOf = this.FindControl<TextBlock>("lblGameRomOf");
        
        var lblGameYearLabel = this.FindControl<TextBlock>("lblGameYearLabel");
        var lblGameYear = this.FindControl<TextBlock>("lblGameYear");
        
        var lblGameCategoryLabel = this.FindControl<TextBlock>("lblGameCategoryLabel");
        var lblGameCategory = this.FindControl<TextBlock>("lblGameCategory");

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
            // Note: Treating EmuArc same as Standard for basic fields to match WinForms behavior
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

    /// <summary>
    /// Hides all artwork tabs and collapses the artwork column.
    /// </summary>
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

    /// <summary>
    /// Shows the artwork section and restores its width.
    /// </summary>
    private void ShowArtworkSection()
    {
        if (ArtworkSplitter != null) ArtworkSplitter.IsVisible = true;
        if (ArtworkTabs != null) ArtworkTabs.IsVisible = true;
        if (GameListGrid != null) GameListGrid.ColumnDefinitions[2].Width = _lastArtworkWidth.Value > 0 ? _lastArtworkWidth : new GridLength(300);
    }

    /// <summary>
    /// Updates the visibility of artwork tabs based on available assets for the selected game.
    /// Checks for emulator specific artwork first, then NFOs, then C64 specifics.
    /// </summary>
    /// <param name="tGame">The selected game file.</param>
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

    /// <summary>
    /// Loads MAME-style artwork panels (artwork, logo, screenshots, cabinets).
    /// </summary>
    /// <param name="tGame">The game file.</param>
    /// <param name="extraPath">The path to the artwork assets.</param>
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

    /// <summary>
    /// Loads MAME Software List style artwork panels.
    /// </summary>
    /// <param name="tGame">The game file.</param>
    /// <param name="extraPath">The path to the artwork assets.</param>
    /// <summary>
    /// Loads MAME Software List style artwork panels.
    /// </summary>
    /// <param name="tGame">The game file.</param>
    /// <param name="extraPath">The path to the artwork assets.</param>
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

    /// <summary>
    /// Loads TruRip specific artwork panels.
    /// </summary>
    /// <param name="tGame">The game file.</param>
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

    /// <summary>
    /// Loads C64 specific artwork panels (Front, Cassette, Inlay).
    /// </summary>
    /// <param name="tGame">The game file.</param>
    /// <returns>True if any artwork was loaded.</returns>
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

    /// <summary>
    /// Loads NFO and DIZ files for display.
    /// </summary>
    /// <param name="tGame">The game file.</param>
    /// <returns>True if any text file was loaded.</returns>
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

    /// <summary>
    /// Tries to load an image with .png or .jpg extension.
    /// </summary>
    private bool TryLoadImage(global::Avalonia.Controls.Image pic, RvFile tGame, string filename)
    {
        return LoadImage(pic, tGame, filename + ".png") || LoadImage(pic, tGame, filename + ".jpg");
    }

    /// <summary>
    /// Loads an image from a file or zip entry into an Image control.
    /// </summary>
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

    /// <summary>
    /// Loads text from a file or zip entry into a TextBox.
    /// </summary>
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

    /// <summary>
    /// Loads NFO text, attempting to handle CodePage 437 or ASCII.
    /// </summary>
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

    /// <summary>
    /// Converts a wildcard pattern to a regex.
    /// </summary>
    private static Regex WildcardToRegex(string pattern)
    {
        if (pattern.ToLower().StartsWith("regex:"))
            return new Regex(pattern.Substring(6), RegexOptions.IgnoreCase);

        return new Regex("^" + Regex.Escape(pattern).
        Replace("\\*", ".*").
        Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Loads bytes from a file or a zip entry matching the filename pattern.
    /// </summary>
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
            Debug.WriteLine(e);
            return false;
        }
    }

    /// <summary>
    /// Updates the ROM Grid (list of individual ROMs inside a game) for the selected game.
    /// </summary>
    /// <param name="tGame">The selected game file.</param>
    private void UpdateRomGrid(RvFile tGame)
    {
        var fileList = new List<RvFile>();
        AddDir(tGame, "", ref fileList);

        bool showMergeColumn = false;
        bool altFound = false;
        bool showStatus = false;
        bool showFileModDate = false;

        for (int i = 0; i < fileList.Count; i++)
        {
            var tFile = fileList[i];

            if (!showMergeColumn && !string.IsNullOrWhiteSpace(tFile.Merge))
            {
                showMergeColumn = true;
            }

            if (!altFound)
            {
                altFound = (tFile.AltSize != null) || (tFile.AltCRC != null) || (tFile.AltSHA1 != null) || (tFile.AltMD5 != null);
            }

            if (!showStatus && !string.IsNullOrWhiteSpace(tFile.Status))
            {
                showStatus = true;
            }

            if (!showFileModDate)
            {
                showFileModDate =
                    (tFile.FileModTimeStamp != 0) &&
                    (tFile.FileModTimeStamp != long.MinValue) &&
                    (tFile.FileModTimeStamp != Compress.StructuredZip.StructuredZip.TrrntzipDateTime) &&
                    (tFile.FileModTimeStamp != Compress.StructuredZip.StructuredZip.TrrntzipDosDateTime);
            }
        }

        var romMergeColumn = RomGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Merge", StringComparison.Ordinal));
        if (romMergeColumn != null) romMergeColumn.IsVisible = showMergeColumn;

        var romAltSizeColumn = RomGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Alt Size", StringComparison.Ordinal));
        if (romAltSizeColumn != null) romAltSizeColumn.IsVisible = altFound;

        var romAltCRC32Column = RomGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Alt CRC32", StringComparison.Ordinal));
        if (romAltCRC32Column != null) romAltCRC32Column.IsVisible = altFound;

        var romAltSHA1Column = RomGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Alt SHA1", StringComparison.Ordinal));
        if (romAltSHA1Column != null) romAltSHA1Column.IsVisible = altFound;

        var romAltMD5Column = RomGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Alt MD5", StringComparison.Ordinal));
        if (romAltMD5Column != null) romAltMD5Column.IsVisible = altFound;

        var romStatusColumn = RomGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Status", StringComparison.Ordinal));
        if (romStatusColumn != null) romStatusColumn.IsVisible = showStatus;

        var romFileModDateColumn = RomGrid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), "Modified Date/Time", StringComparison.Ordinal));
        if (romFileModDateColumn != null) romFileModDateColumn.IsVisible = showFileModDate;

        RomGrid.ItemsSource = fileList;
    }

    /// <summary>
    /// Recursively adds files from a directory to the file list for the ROM Grid.
    /// </summary>
    /// <param name="tGame">The current directory to process.</param>
    /// <param name="pathAdd">The path prefix to add to file names.</param>
    /// <param name="fileList">The list to populate.</param>
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

    /// <summary>
    /// Adds a single ROM file to the file list if it meets the display criteria.
    /// </summary>
    /// <param name="tFile">The file to add.</param>
    /// <param name="pathAdd">The path prefix.</param>
    /// <param name="fileList">The list to populate.</param>
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
    
    /// <summary>
    /// Handles the "Scan" context menu click on the tree.
    /// </summary>
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

    /// <summary>
    /// Handles the "Set Dir Dat Settings" context menu click.
    /// Opens the directory settings window.
    /// </summary>
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

    /// <summary>
    /// Handles the "Set Dir Mappings" context menu click.
    /// Opens the directory mappings window for a specific directory.
    /// </summary>
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

    /// <summary>
    /// Handles the "Global Dir Mappings" menu click.
    /// Opens the global directory mappings window.
    /// </summary>
    private async void OnGlobalDirMappingsClick(object? sender, RoutedEventArgs e)
    {
         if (_working) return;
         var win = new Views.DirectoryMappingsWindow();
         win.SetDisplayType(false);
         await win.ShowDialog(this);
    }

    /// <summary>
    /// Handles the "Open Directory" context menu click.
    /// Opens the selected directory in the OS file explorer.
    /// </summary>
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

    /// <summary>
    /// Handles the "Save Fix DATs" context menu click.
    /// Generates fix DATs for the selected directory.
    /// </summary>
    private async void OnSaveFixDatsClick(object? sender, RoutedEventArgs e) 
    {
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        var selected = rvTree?.Selected;
        if (selected != null)
        {
             await Code.Report.CreateFixDat(this, selected, true);
        }
    }

    /// <summary>
    /// Handles the "Save Full DAT" context menu click.
    /// Saves the DAT file to disk.
    /// </summary>
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

    /// <summary>
    /// Handles the pointer press event on the instance count text block.
    /// Shows the ROM Info window for the selected file.
    /// </summary>
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


    /// <summary>
    /// Handles the "Scan" context menu click on the Game Grid.
    /// </summary>
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

    /// <summary>
    /// Handles the "Open Directory" context menu click on the Game Grid.
    /// Opens the directory or zip file location in the OS file explorer.
    /// </summary>
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

    /// <summary>
    /// Handles the "Open Parent Directory" context menu click on the Game Grid.
    /// </summary>
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

    /// <summary>
    /// Handles the "Launch Emulator" context menu click.
    /// </summary>
    private void OnLaunchEmulatorClick(object? sender, RoutedEventArgs e) 
    { 
        if (GameGrid.SelectedItem is RvFile tGame)
        {
            LaunchEmulator(tGame);
        }
    }

    /// <summary>
    /// Handles the "Open Web Page" context menu click.
    /// Opens the No-Intro or Redump page for the game if available.
    /// </summary>
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
    
    /// <summary>
    /// Handles the "Update New DATs" menu click.
    /// </summary>
    private void OnUpdateNewDatsClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        UpdateDats();
    }

    /// <summary>
    /// Handles the "Update All DATs" menu click.
    /// Checks for changes in all DATs and updates the database.
    /// </summary>
    private void OnUpdateAllDatsClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        DatUpdate.CheckAllDats(DB.DirRoot.Child(0), @"DatRoot\");
        UpdateDats();
    }

    /// <summary>
    /// Handles the "Scan ROMs" menu click.
    /// </summary>
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

    private void OnFixRomsClick(object? sender, RoutedEventArgs e)
    {
        if (_working) return;
        FixFiles();
    }
    
    /// <summary>
    /// Handles the "Fix DAT Report" menu click.
    /// </summary>
    private async void OnFixDatReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.CreateFixDat(this, DB.DirRoot.Child(0), true);
    }
    
    /// <summary>
    /// Handles the "Generate Full Report" menu click.
    /// </summary>
    private async void OnFullReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.GenerateReport(this);
    }
    
    /// <summary>
    /// Handles the "Generate Fix Report" menu click.
    /// </summary>
    private async void OnFixReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.GenerateFixReport(this);
    }
    
    /// <summary>
    /// Handles the "Global Dir Dat Settings" menu click.
    /// </summary>
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

    /// <summary>
    /// Handles the "Settings" menu click.
    /// </summary>
    private async void OnRomVaultSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (_working) return;
        var win = new Views.SettingsWindow();
        await win.ShowDialog(this);
    }

    /// <summary>
    /// Handles the "Add To Sort" menu click.
    /// Adds a new directory to be sorted into the database.
    /// </summary>
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

    /// <summary>
    /// Shows the TorrentZip help window.
    /// </summary>
    private void OnHelpTorrentZipClick(object? sender, RoutedEventArgs e) 
    {
        var win = new Views.TrrntZipWindow();
        win.Show();
    }

    /// <summary>
    /// Opens the online Wiki.
    /// </summary>
    private void OnHelpWikiClick(object? sender, RoutedEventArgs e) 
    {
        try { Process.Start(new ProcessStartInfo { FileName = "https://wiki.romvault.com/doku.php?id=help", UseShellExecute = true }); } catch { }
    }

    /// <summary>
    /// Shows the Color Key window.
    /// </summary>
    private void OnHelpColorKeyClick(object? sender, RoutedEventArgs e) 
    {
        var win = new Views.KeyWindow();
        win.Show(this);
    }

    /// <summary>
    /// Opens the What's New online page.
    /// </summary>
    private void OnHelpWhatsNewClick(object? sender, RoutedEventArgs e) 
    {
        try { Process.Start(new ProcessStartInfo { FileName = "https://wiki.romvault.com/doku.php?id=whats_new", UseShellExecute = true }); } catch { }
    }

    /// <summary>
    /// Shows the About window.
    /// </summary>
    private void OnHelpAboutClick(object? sender, RoutedEventArgs e) 
    {
        var win = new Views.HelpAboutWindow();
        win.ShowDialog(this);
    }

    /// <summary>
    /// Handles the "Update DATs" toolbar button click.
    /// </summary>
    private void OnUpdateDatsClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        UpdateDats();
    }

    /// <summary>
    /// Handles the "Find Fixes" toolbar button click.
    /// </summary>
    private void OnFindFixesClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        FindFixes();
    }

    private void OnTreePresetPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.Tag is not string tag) return;
        if (!int.TryParse(tag, out int index)) return;
        bool set = e.GetCurrentPoint(control).Properties.IsRightButtonPressed;
        TreeDefault(set, index);
    }

    private void TreeDefault(bool set, int index)
    {
        var dtss = new DatTreeStatusStore();
        if (set)
        {
            dtss.write(index);
            return;
        }
        dtss.read(index);
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        rvTree?.Setup(DB.DirRoot);
    }
    
    /// <summary>
    /// Handles the "Fix Files" toolbar button click.
    /// </summary>
    private void OnFixFilesClick(object? sender, RoutedEventArgs e) 
    {
         if (_working) return;
         FixFiles();
    }

    /// <summary>
    /// Handles the "Report" toolbar button click.
    /// </summary>
    private async void OnReportClick(object? sender, RoutedEventArgs e) 
    {
        if (_working) return;
        await Code.Report.CreateFixDat(this, DB.DirRoot.Child(0), true);
    }

    /// <summary>
    /// Launches the configured emulator for the selected game.
    /// </summary>
    /// <param name="tGame">The game file to launch.</param>
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

    /// <summary>
    /// Finds the configured emulator information for a given game path.
    /// </summary>
    /// <param name="tGame">The game file.</param>
    /// <returns>The <see cref="EmulatorInfo"/> if found, otherwise null.</returns>
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

    /// <summary>
    /// Sets the UI to a "Working" state (busy cursor, disabled controls).
    /// </summary>
    private void Start()
    {
        _working = true;
        this.Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Wait);
        if (lblStatusRight != null) lblStatusRight.Text = "Working...";
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        if (rvTree != null) rvTree.Working = true;
    }

    /// <summary>
    /// Resets the UI from a "Working" state.
    /// </summary>
    private void Finish()
    {
        _working = false;
        this.Cursor = global::Avalonia.Input.Cursor.Default;
        if (lblStatusRight != null) lblStatusRight.Text = "";
        var rvTree = this.FindControl<ROMVault.Avalonia.Views.RvTree>("RvTreeControl");
        if (rvTree != null)
        {
            rvTree.Working = false;
            DatSetSelected(rvTree.Selected);
        }
    }

    /// <summary>
    /// Starts the ROM scanning process in a background thread.
    /// </summary>
    /// <param name="sd">The scan level (depth).</param>
    /// <param name="StartAt">The file/directory to start scanning from. If null, scans everything.</param>
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

    /// <summary>
    /// Starts the DAT update process in a background thread.
    /// Updates the internal database from DAT files.
    /// </summary>
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

    /// <summary>
    /// Starts the process to find fixes for missing/broken ROMs.
    /// </summary>
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

    /// <summary>
    /// Starts the process to apply fixes (move/rename/copy files).
    /// </summary>
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
