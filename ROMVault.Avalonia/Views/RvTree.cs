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
using Avalonia.Threading;

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
        private RvFile? _hovered;
        private string _typeSearch = "";
        private DispatcherTimer? _typeSearchTimer;
        private readonly List<RvFile> _visibleNodes = new List<RvFile>();

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
            this.ClipToBounds = true;
            Focusable = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_lTree == null) return;

            var point = e.GetPosition(this);
            var hit = HitTestNode(point);
            if (!ReferenceEquals(hit, _hovered))
            {
                _hovered = hit;
                InvalidateVisual();
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (_hovered != null)
            {
                _hovered = null;
                InvalidateVisual();
            }
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_lTree == null)
                return;

            if (Selected == null)
            {
                var first = GetFirstVisible();
                if (first != null) SetSelected(first);
                return;
            }

            if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
                return;

            if (e.Key == Key.Down)
            {
                var next = GetRelativeVisible(Selected, 1);
                if (next != null) SetSelected(next);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up)
            {
                var prev = GetRelativeVisible(Selected, -1);
                if (prev != null) SetSelected(prev);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Right)
            {
                if (Selected.Tree != null)
                {
                    if (!Selected.Tree.TreeExpanded && CanExpand(Selected))
                    {
                        Selected.Tree.SetTreeExpanded(true, false);
                        SetupInt();
                    }
                    else
                    {
                        var child = GetFirstChild(Selected);
                        if (child != null) SetSelected(child);
                    }
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left)
            {
                if (Selected.Tree != null && Selected.Tree.TreeExpanded)
                {
                    Selected.Tree.SetTreeExpanded(false, false);
                    SetupInt();
                }
                else if (Selected.Parent != null && Selected.Parent != _lTree)
                {
                    SetSelected(Selected.Parent);
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Space)
            {
                if (Selected.Tree != null)
                {
                    if (Selected.Tree.Checked != RvTreeRow.TreeSelect.Locked)
                    {
                        var nextState = Selected.Tree.Checked == RvTreeRow.TreeSelect.Selected
                            ? RvTreeRow.TreeSelect.UnSelected
                            : RvTreeRow.TreeSelect.Selected;
                        SetChecked(Selected, nextState, false, false);
                        RvChecked?.Invoke(this, Selected);
                        SetupInt();
                    }
                }
                e.Handled = true;
                return;
            }

            if (TryAppendTypeSearch(e.Key, out var search))
            {
                var match = FindByPrefix(search, Selected);
                if (match != null)
                {
                    SetSelected(match);
                }
                e.Handled = true;
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

            // Row (Selection)
            double rowWidth = Bounds.Width > 0 ? Bounds.Width : 2000;
            var rowRect = new Rect(0, uTree.RTree.Top, rowWidth, uTree.RTree.Height);
            if (rowRect.Contains(new Point(x, y)))
            {
                Selected = pTree;
                RvSelected?.Invoke(this, pTree);

                if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                {
                     RvRightClicked?.Invoke(this, pTree);
                }

                SetupInt();
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

        private RvFile? HitTestNode(Point point)
        {
            if (_lTree == null)
                return null;

            for (int i = 0; i < _lTree.ChildCount; i++)
            {
                var dir = _lTree.Child(i);
                if (!dir.IsDirectory || dir.Tree?.UiObject == null)
                    continue;

                var hit = HitTestNodeRecursive(dir, point);
                if (hit != null)
                    return hit;
            }
            return null;
        }

        private RvFile? HitTestNodeRecursive(RvFile node, Point point)
        {
            if (node.Tree?.UiObject is UiTree uTree)
            {
                var rowRect = new Rect(0, uTree.RTree.Top, Bounds.Width, uTree.RTree.Height);
                if (rowRect.Contains(point))
                    return node;
            }

            if (node.Tree?.TreeExpanded == true)
            {
                for (int i = 0; i < node.ChildCount; i++)
                {
                    var child = node.Child(i);
                    if (!child.IsDirectory || child.Tree?.UiObject == null)
                        continue;

                    var hit = HitTestNodeRecursive(child, point);
                    if (hit != null)
                        return hit;
                }
            }
            return null;
        }

        private static bool CanExpand(RvFile node)
        {
            if (node.Tree == null) return false;
            if (node.DirDatCount > 1) return true;

            for (int i = 0; i < node.ChildCount; i++)
            {
                var c = node.Child(i);
                if (c.IsDirectory && c.Tree != null)
                    return true;
            }
            return false;
        }

        private RvFile? GetFirstVisible()
        {
            if (_lTree == null) return null;
            for (int i = 0; i < _lTree.ChildCount; i++)
            {
                var dir = _lTree.Child(i);
                if (dir.IsDirectory && dir.Tree != null)
                    return dir;
            }
            return null;
        }

        private RvFile? GetFirstChild(RvFile node)
        {
            for (int i = 0; i < node.ChildCount; i++)
            {
                var child = node.Child(i);
                if (child.IsDirectory && child.Tree != null)
                    return child;
            }
            return null;
        }

        private List<RvFile> BuildVisibleList()
        {
            return _visibleNodes;
        }

        public double? GetRowTop(RvFile node)
        {
            if (node.Tree?.UiObject is UiTree ui)
                return ui.RTree.Top;
            return null;
        }

        private static void AddVisibleRecursive(RvFile node, List<RvFile> list)
        {
            list.Add(node);
            if (node.Tree?.TreeExpanded != true) return;

            for (int i = 0; i < node.ChildCount; i++)
            {
                var child = node.Child(i);
                if (child.IsDirectory && child.Tree != null)
                    AddVisibleRecursive(child, list);
            }
        }

        private RvFile? GetRelativeVisible(RvFile current, int delta)
        {
            var visible = BuildVisibleList();
            int idx = visible.IndexOf(current);
            if (idx < 0) return null;
            int nextIdx = idx + delta;
            if (nextIdx < 0 || nextIdx >= visible.Count) return null;
            return visible[nextIdx];
        }

        private bool TryAppendTypeSearch(Key key, out string search)
        {
            search = _typeSearch;

            if (key == Key.Back)
            {
                if (_typeSearch.Length > 0)
                {
                    _typeSearch = _typeSearch.Substring(0, _typeSearch.Length - 1);
                    ResetTypeSearchTimer();
                }
                search = _typeSearch;
                return _typeSearch.Length > 0;
            }

            char c = '\0';
            if (key >= Key.A && key <= Key.Z)
                c = (char)('a' + (key - Key.A));
            else if (key >= Key.D0 && key <= Key.D9)
                c = (char)('0' + (key - Key.D0));
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
                c = (char)('0' + (key - Key.NumPad0));
            else if (key == Key.Space)
                c = ' ';

            if (c == '\0')
                return false;

            _typeSearch += c;
            ResetTypeSearchTimer();
            search = _typeSearch;
            return true;
        }

        private void ResetTypeSearchTimer()
        {
            _typeSearchTimer?.Stop();
            _typeSearchTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(900), DispatcherPriority.Background, (_, _) =>
            {
                _typeSearch = "";
                _typeSearchTimer?.Stop();
                _typeSearchTimer = null;
            });
            _typeSearchTimer.Start();
        }

        private RvFile? FindByPrefix(string search, RvFile startAfter)
        {
            if (string.IsNullOrWhiteSpace(search))
                return null;

            var visible = BuildVisibleList();
            int startIdx = visible.IndexOf(startAfter);
            if (startIdx < 0) startIdx = -1;

            for (int i = startIdx + 1; i < visible.Count; i++)
            {
                var name = visible[i].Name ?? "";
                if (name.StartsWith(search, StringComparison.CurrentCultureIgnoreCase))
                    return visible[i];
            }

            for (int i = 0; i <= startIdx && i < visible.Count; i++)
            {
                var name = visible[i].Name ?? "";
                if (name.StartsWith(search, StringComparison.CurrentCultureIgnoreCase))
                    return visible[i];
            }
            return null;
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
            _maxWidth = 0;

            if (_lTree != null && _lTree.ChildCount >= 1)
            {
                for (int i = 0; i < _lTree.ChildCount - 1; i++)
                {
                    SetupTree(_lTree.Child(i), "├");
                }

                SetupTree(_lTree.Child(_lTree.ChildCount - 1), "└");
            }

            _visibleNodes.Clear();
            if (_lTree != null)
            {
                for (int i = 0; i < _lTree.ChildCount; i++)
                {
                    var dir = _lTree.Child(i);
                    if (!dir.IsDirectory || dir.Tree == null)
                        continue;
                    AddVisibleRecursive(dir, _visibleNodes);
                }
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

            var rowRect = new Rect(0, uTree.RTree.Top, Bounds.Width, uTree.RTree.Height);
            var viewport = new Rect(0, 0, Bounds.Width, Bounds.Height);
            if (rowRect.Top > viewport.Bottom)
                return;

            bool drawRow = rowRect.Bottom >= viewport.Top;

            if (drawRow)
            {
                if (ReferenceEquals(pTree, Selected))
                {
                    IBrush selBrush;
                    if (TryGetResource("TreeSelectedBrush", null, out var selRes) && selRes is IBrush sb)
                        selBrush = sb;
                    else if (TryGetResource("AccentWeakBrush", null, out var accentWeak) && accentWeak is IBrush aw)
                        selBrush = aw;
                    else
                        selBrush = new SolidColorBrush(Color.FromArgb(0x55, 0x2B, 0x7F, 0xFF));

                    context.FillRectangle(selBrush, rowRect);

                    IBrush outlineBrush;
                    if (TryGetResource("AccentBrush", null, out var accentRes) && accentRes is IBrush accentBrush)
                        outlineBrush = accentBrush;
                    else
                        outlineBrush = Brushes.DodgerBlue;

                    var outlineRect = rowRect.Deflate(1);
                    context.DrawRectangle(null, new Pen(outlineBrush, 1), outlineRect, 6, 6);
                }
                else if (ReferenceEquals(pTree, _hovered))
                {
                    IBrush hovBrush;
                    if (TryGetResource("TreeHoverBrush", null, out var hovRes) && hovRes is IBrush hb)
                        hovBrush = hb;
                    else if (TryGetResource("SurfaceBackgroundAltBrush", null, out var altRes) && altRes is IBrush ab)
                        hovBrush = ab;
                    else
                        hovBrush = new SolidColorBrush(Color.FromArgb(0x22, 0x2B, 0x7F, 0xFF));
                    context.FillRectangle(hovBrush, rowRect);
                }

                IBrush lineBrush = Brushes.Gray;
                if (TryGetResource("SurfaceBorderBrush", null, out var lineRes) && lineRes is IBrush lb)
                    lineBrush = lb;
                Pen pen = new Pen(lineBrush, 1, new DashStyle(new double[] { 1, 1 }, 0));
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

                if (uTree.RExpand.Width > 0)
                {
                    string iconName = pTree.Tree.TreeExpanded ? "ExpandBoxMinus" : "ExpandBoxPlus";
                    Bitmap? bmp = GetBitmap(iconName);
                    if (bmp != null)
                        context.DrawImage(bmp, uTree.RExpand);
                }

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
                if (pTree.Dat == null && pTree.DirDatCount == 0)
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

                FormattedText text = new FormattedText(
                    pTree.Name ?? "",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    12,
                    textColor
                );
                context.DrawText(text, uTree.RText.Position);
            }


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
