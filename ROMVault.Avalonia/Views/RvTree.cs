using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Documents;
using RomVaultCore;
using RomVaultCore.RvDB;
using System;
using System.Collections.Generic;
using System.IO;

namespace ROMVault.Avalonia.Views
{
    /// <summary>
    /// A custom tree control for displaying the ROMVault directory structure.
    /// Handles custom rendering of tree nodes, icons, checkboxes, and expansion logic.
    /// </summary>
    public class RvTree : Control
    {
        private class UiTree
        {
            public string TreeBranches = "";
            public Rect RTree;
            public Rect RExpand;
            public Rect RChecked;
            public Rect RIcon;
            public Rect RText;
        }

        private RvFile? _lTree;
        private double _yPos;
        private double _maxWidth;
        private Dictionary<string, Bitmap> _bitmapCache = new Dictionary<string, Bitmap>();

        /// <summary>
        /// Gets the currently selected file/directory in the tree.
        /// </summary>
        public RvFile? Selected { get; private set; }
        
        /// <summary>
        /// Indicates if the tree is currently performing a background operation (e.g., scanning).
        /// </summary>
        public bool Working { get; set; }

        /// <summary>
        /// Event raised when a node is selected.
        /// </summary>
        public event EventHandler<RvFile>? RvSelected;
        
        /// <summary>
        /// Event raised when a node's checkbox is toggled.
        /// </summary>
        public event EventHandler<RvFile>? RvChecked;

        /// <summary>
        /// Sets the selected node programmatically.
        /// </summary>
        /// <param name="selected">The file to select.</param>
        public void SetSelected(RvFile? selected)
        {
            Selected = selected;
            this.InvalidateVisual();
            if (selected != null)
                RvSelected?.Invoke(this, selected);
        }
        
        /// <summary>
        /// Event raised when a node is right-clicked.
        /// </summary>
        public event EventHandler<RvFile>? RvRightClicked;

        public RvTree()
        {
            // Enable hit testing
            this.ClipToBounds = true;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            var point = e.GetPosition(this);
            var x = point.X;
            var y = point.Y;

            if (_lTree != null)
            {
                for (int i = 0; i < _lTree.ChildCount; i++)
                {
                    RvFile tDir = _lTree.Child(i);
                    if (tDir.Tree == null)
                        continue;
                    if (CheckMouseDown(tDir, x, y, e))
                        break;
                }
            }
        }

        private bool CheckMouseDown(RvFile pTree, double x, double y, PointerPressedEventArgs e)
        {
            if (pTree.Tree?.UiObject == null) return false;
            var uTree = (UiTree)pTree.Tree.UiObject;

            // Expand box
            if (uTree.RExpand.Contains(new Point(x, y)))
            {
                SetExpanded(pTree, e.GetCurrentPoint(this).Properties.IsRightButtonPressed);
                SetupInt();
                return true;
            }

            // Checkbox
            if (uTree.RChecked.Contains(new Point(x, y)))
            {
                RvChecked?.Invoke(this, pTree);

                bool shiftPressed = (e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift;

                if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                {
                    if (pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortPrimary) || pTree.ToSortStatusIs(RvFile.ToSortDirType.ToSortCache))
                        return true;

                    SetChecked(pTree, RvTreeRow.TreeSelect.Locked, false, shiftPressed);
                    SetupInt();
                    return true;
                }

                SetChecked(pTree, pTree.Tree.Checked == RvTreeRow.TreeSelect.Selected ? RvTreeRow.TreeSelect.UnSelected : RvTreeRow.TreeSelect.Selected, false, shiftPressed);
                SetupInt();
                return true;
            }

            // Text (Selection)
            if (uTree.RText.Contains(new Point(x, y)))
            {
                Selected = pTree;
                RvSelected?.Invoke(this, pTree);

                if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                {
                     RvRightClicked?.Invoke(this, pTree);
                }

                SetupInt(); // Redraw selection highlight if we implement it
                return true;
            }

            // Recurse if expanded
            if (pTree.Tree.TreeExpanded)
            {
                for (int i = 0; i < pTree.ChildCount; i++)
                {
                    RvFile rDir = pTree.Child(i);
                    if (!rDir.IsDirectory || rDir.Tree == null)
                        continue;

                    if (CheckMouseDown(rDir, x, y, e))
                        return true;
                }
            }

            return false;
        }

        private void SetExpanded(RvFile pTree, bool rightClick)
        {
             if (!rightClick)
            {
                pTree.Tree.SetTreeExpanded(!pTree.Tree.TreeExpanded, false);
                return;
            }
            RvTreeRow.OpenStream();
            // Find the value of the first child node.
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile d = pTree.Child(i);
                if (!d.IsDirectory || d.Tree == null)
                    continue;

                //Recusivly Set All Child Nodes to this value
                SetExpandedRecurse(pTree, !d.Tree.TreeExpanded, false);
                break;
            }
            RvTreeRow.CloseStream();
        }

        private static void SetExpandedRecurse(RvFile pTree, bool expanded, bool isWorking)
        {
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile d = pTree.Child(i);
                if (!d.IsDirectory || d.Tree == null)
                    continue;

                d.Tree.SetTreeExpanded(expanded, isWorking);
                SetExpandedRecurse(d, expanded, isWorking);
            }
        }

        private static void SetChecked(RvFile pTree, RvTreeRow.TreeSelect nSelection, bool isWorking, bool shiftPressed)
        {
            if (!isWorking) RvTreeRow.OpenStream();
            SetCheckedRecurse(pTree, nSelection, isWorking, shiftPressed);
            if (!isWorking) RvTreeRow.CloseStream();
        }

        private static void SetCheckedRecurse(RvFile pTree, RvTreeRow.TreeSelect nSelection, bool isworking, bool shiftPressed)
        {
            pTree.Tree.SetChecked(nSelection, isworking);
            if (shiftPressed)
                return;
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile d = pTree.Child(i);
                if (d.IsDirectory && d.Tree != null)
                {
                    SetCheckedRecurse(d, nSelection, isworking, false);
                }
            }
        }

        /// <summary>
        /// Sets up the tree data structure for rendering.
        /// Call this when the tree structure changes.
        /// </summary>
        /// <param name="dirTree">The root directory.</param>
        public void Setup(RvFile dirTree)
        {
            _lTree = dirTree;
            SetupInt();
        }

        /// <summary>
        /// Internal method to recalculate the layout of the tree.
        /// </summary>
        private void SetupInt()
        {
            _yPos = 0;

            if (_lTree != null && _lTree.ChildCount >= 1)
            {
                for (int i = 0; i < _lTree.ChildCount - 1; i++)
                {
                    SetupTree(_lTree.Child(i), "├");
                }

                SetupTree(_lTree.Child(_lTree.ChildCount - 1), "└");
            }
            
            // In Avalonia custom control, we usually request resize or invalidate measure.
            // For scrolling, we might need to be inside a ScrollViewer and set our Height.
            this.Height = _yPos;
            this.MinWidth = _maxWidth;
            this.Width = double.NaN;
            this.InvalidateVisual();
        }

        /// <summary>
        /// Recursively calculates the layout for a tree node and its children.
        /// </summary>
        /// <param name="pTree">The current tree node.</param>
        /// <param name="pTreeBranches">The string representing the branch structure for this node.</param>
        private void SetupTree(RvFile pTree, string pTreeBranches)
        {
            int nodeDepth = pTreeBranches.Length - 1;

            double nodeHeight = 16;
            if (pTree.Tree.TreeExpanded && pTree.DirDatCount > 1)
            {
                for (int i = 0; i < pTree.DirDatCount; i++)
                {
                    if (!pTree.DirDat(i).Flag(DatFlags.AutoAddedDirectory))
                        nodeHeight += 12;
                }
            }

            UiTree uTree = new UiTree();
            pTree.Tree.UiObject = uTree;

            uTree.TreeBranches = pTreeBranches;

            uTree.RTree = new Rect(0, _yPos, 1 + nodeDepth * 18, nodeHeight);
            uTree.RExpand = new Rect(5 + nodeDepth * 18, _yPos + 4, 9, 9);
            uTree.RChecked = new Rect(20 + nodeDepth * 18, _yPos + 2, 13, 13);
            uTree.RIcon = new Rect(35 + nodeDepth * 18, _yPos, 16, 16);
            uTree.RText = new Rect(51 + nodeDepth * 18, _yPos, 1000, nodeHeight); // Arbitrary width for text

            double textRight = 51 + nodeDepth * 18 + (pTree.Name?.Length ?? 0) * 10 + 50;
            if (textRight > _maxWidth) _maxWidth = textRight;

            pTreeBranches = pTreeBranches.Replace("├", "│");
            pTreeBranches = pTreeBranches.Replace("└", " ");

            _yPos = _yPos + nodeHeight;

            bool found = false;
            int last = 0;
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile dir = pTree.Child(i);
                if (!dir.IsDirectory)
                    continue;

                if (dir.Tree == null)
                    continue;

                found = true;
                if (pTree.Tree.TreeExpanded)
                    last = i;

            }


            if (!found && pTree.DirDatCount <= 1)
            {
                uTree.RExpand = new Rect(0, 0, 0, 0);
            }

            if (pTree.Tree.TreeExpanded && found)
            {
                uTree.TreeBranches += "┐";
            }

            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvFile dir = pTree.Child(i);
                if (!dir.IsDirectory)
                    continue;

                if (dir.Tree == null)
                    continue;

                if (!pTree.Tree.TreeExpanded)
                    continue;

                if (i != last)
                    SetupTree(pTree.Child(i), pTreeBranches + "├");
                else
                    SetupTree(pTree.Child(i), pTreeBranches + "└");
            }
        }

        /// <summary>
        /// Renders the tree control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            IBrush backBrush = Brushes.Transparent;
            if (this.TryGetResource("SystemControlBackgroundAltHighBrush", null, out var resBg) && resBg is IBrush brushBg)
            {
                backBrush = brushBg;
            }
            context.FillRectangle(backBrush, new Rect(Bounds.Size));

            if (_lTree == null)
                return;

            for (int i = 0; i < _lTree.ChildCount; i++)
            {
                RvFile tDir = _lTree.Child(i);
                if (!tDir.IsDirectory)
                    continue;

                if (tDir.Tree?.UiObject != null)
                {
                    PaintTree(tDir, context);
                }
            }
        }

        /// <summary>
        /// Paints a single tree node and its visible children.
        /// </summary>
        /// <param name="pTree">The tree node to paint.</param>
        /// <param name="context">The drawing context.</param>
        private void PaintTree(RvFile pTree, DrawingContext context)
        {
            UiTree uTree = (UiTree)pTree.Tree.UiObject;

            // Simple culling check (optional optimization)
            // if (!uTree.RTree.Intersects(new Rect(0, 0, Bounds.Width, Bounds.Height))) { ... }

            // Draw Lines
            Pen pen = new Pen(Brushes.Gray, 1, new DashStyle(new double[] { 1, 1 }, 0));
            string lTree = uTree.TreeBranches;
            for (int j = 0; j < lTree.Length; j++)
            {
                char c = lTree[j];
                double x = 9 + j * 18;
                double y = uTree.RTree.Top;
                double h = uTree.RTree.Height;

                switch (c)
                {
                    case '│':
                        context.DrawLine(pen, new Point(x, y), new Point(x, y + h));
                        break;
                    case '├':
                        context.DrawLine(pen, new Point(x, y), new Point(x, y + h));
                        context.DrawLine(pen, new Point(x, y + 8), new Point(x + 8, y + 8));
                        break;
                    case '└':
                        context.DrawLine(pen, new Point(x, y), new Point(x, y + 8));
                        context.DrawLine(pen, new Point(x, y + 8), new Point(x + 8, y + 8));
                        break;
                    case ' ':
                        break;
                    case '┐':
                        context.DrawLine(pen, new Point(x + 10, y + 16), new Point(x + 10, y + h));
                        context.DrawLine(pen, new Point(x, y + 8), new Point(x + 10, y + 8));
                        context.DrawLine(pen, new Point(x + 10, y + 8), new Point(x + 10, y + 16));
                        break;
                }
            }

            // Draw Expand Box
            if (uTree.RExpand.Width > 0)
            {
                string iconName = pTree.Tree.TreeExpanded ? "ExpandBoxMinus" : "ExpandBoxPlus";
                Bitmap? bmp = GetBitmap(iconName);
                if (bmp != null)
                    context.DrawImage(bmp, uTree.RExpand);
            }

            // Draw Checkbox
            string checkName = "TickBoxUnTicked";
            switch (pTree.Tree.Checked)
            {
                case RvTreeRow.TreeSelect.Locked:
                    checkName = "TickBoxLocked";
                    break;
                case RvTreeRow.TreeSelect.UnSelected:
                    checkName = "TickBoxUnTicked";
                    break;
                case RvTreeRow.TreeSelect.Selected:
                    checkName = "TickBoxTicked";
                    break;
            }

            Bitmap? checkBmp = GetBitmap(checkName);
            if (checkBmp != null)
               context.DrawImage(checkBmp, uTree.RChecked);

            // Draw Icon
            int icon = 2;
            if (pTree.DirStatus.HasInToSort())
            {
                icon = 4;
            }
            else if (!pTree.DirStatus.HasCorrect() && pTree.DirStatus.HasMissing())
            {
                icon = 1;
            }
            else if (!pTree.DirStatus.HasMissing() && pTree.DirStatus.HasMIA())
            {
                icon = 5;
            }
            else if (!pTree.DirStatus.HasMissing())
            {
                icon = 3;
            }

            string dirIcon;
            if (pTree.Dat == null && pTree.DirDatCount == 0) // Directory above DAT's in Tree
            {
                dirIcon = "DirectoryTree" + icon;
            }
            else
            {
                dirIcon = "Tree" + icon;
            }

            Bitmap? dirBmp = GetBitmap(dirIcon);
            if (dirBmp != null)
                context.DrawImage(dirBmp, uTree.RIcon);

            // Draw Text
            IBrush textColor = Brushes.Black;
            var fg = this.GetValue(TextElement.ForegroundProperty);
            if (fg != null)
            {
                textColor = fg;
            }
            else if (this.TryGetResource("SystemControlForegroundBaseHighBrush", null, out var res) && res is IBrush brush)
            {
                textColor = brush;
            }
            
            if (pTree == Selected)
            {
                // Highlight background
                context.FillRectangle(Brushes.LightBlue, uTree.RText);
            }

            FormattedText text = new FormattedText(
                pTree.Name ?? "",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                12,
                textColor
            );
            context.DrawText(text, uTree.RText.Position);


            // Recurse
            if (pTree.Tree.TreeExpanded)
            {
                for (int i = 0; i < pTree.ChildCount; i++)
                {
                    RvFile child = pTree.Child(i);
                    if (child.IsDirectory && child.Tree?.UiObject != null)
                    {
                        PaintTree(child, context);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a bitmap from the cache or loads it from resources.
        /// </summary>
        /// <param name="name">The name of the asset (without extension).</param>
        /// <returns>The loaded bitmap, or null if not found.</returns>
        private Bitmap? GetBitmap(string name)
        {
            if (_bitmapCache.TryGetValue(name, out var bmp))
                return bmp;

            try 
            {
                var uri = new Uri($"avares://ROMVault.Avalonia/Assets/{name}.png");
                if (AssetLoader.Exists(uri))
                {
                    bmp = new Bitmap(AssetLoader.Open(uri));
                    _bitmapCache[name] = bmp;
                    return bmp;
                }
            }
            catch { }
            
            return null;
        }
    }
}
