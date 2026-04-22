using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using IcdFyIt.App.Converters;

namespace IcdFyIt.App.Controls;

/// <summary>Event args for <see cref="DraggableGrid.ItemMoved"/>.</summary>
public sealed class ItemMovedEventArgs : EventArgs
{
    public object DraggedItem { get; }
    public object TargetItem { get; }
    /// <summary>True = insert before the target row; false = insert after it.</summary>
    public bool Above { get; }
    /// <summary>Set to true in the handler to prevent the default direct-list manipulation.</summary>
    public bool Handled { get; set; }

    public ItemMovedEventArgs(object draggedItem, object targetItem, bool above)
    {
        DraggedItem = draggedItem;
        TargetItem  = targetItem;
        Above       = above;
    }
}

/// <summary>
/// A spreadsheet-style grid that supports native drag-to-reorder rows, editable cells
/// with live Avalonia data-binding, column visibility toggling and row selection.
/// </summary>
public partial class DraggableGrid : UserControl
{
    // ── Styled properties ─────────────────────────────────────────────────────

    public static readonly StyledProperty<IList?> ItemsSourceProperty =
        AvaloniaProperty.Register<DraggableGrid, IList?>(nameof(ItemsSource));

    public IList? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<DraggableGrid, object?>(nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    // ── Column descriptor collection (populated from XAML before visual-tree attach) ──

    public List<DraggableGridColumn> Columns { get; } = new();

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Raised when an editable cell's TextBox loses focus (i.e. an edit is committed).</summary>
    public event EventHandler? EditEnded;

    /// <summary>Raised when the user completes a drag-to-reorder gesture.
    /// Set <see cref="ItemMovedEventArgs.Handled"/> = true to suppress the default
    /// direct-list mutation and handle reordering yourself (e.g. via a ViewModel).</summary>
    public event EventHandler<ItemMovedEventArgs>? ItemMoved;

    // ── Internal visual references ────────────────────────────────────────────

    private Grid?       _root;
    private Border?     _headerBorder;
    private StackPanel? _rowsPanel;
    private readonly List<Border> _rowBorders = new();

    // ── Drag-and-drop state ───────────────────────────────────────────────────

    private int      _dragIndex     = -1;
    private Point    _dragStartPos;
    private Border?  _draggedBorder;
    private bool     _isDragging;
    private IPointer? _capturedPointer;

    // ── Column-resize state ───────────────────────────────────────────────────

    /// <summary>Current pixel widths indexed by <see cref="Columns"/> list index.</summary>
    private double[] _colWidths = Array.Empty<double>();

    /// <summary>Maps visible-column index → Columns-list index.</summary>
    private int[] _visibleToAllIdx = Array.Empty<int>();

    /// <summary>ColumnDefinitions of the header grid (one per visible column).</summary>
    private ColumnDefinition[]? _headerColDefs;

    /// <summary>ColumnDefinitions of each row grid (one array per row).</summary>
    private readonly List<ColumnDefinition[]> _rowColDefs = new();

    private int      _resizingAllIdx   = -1;
    private int      _resizingVisIdx   = -1;
    private double   _resizeStartX;
    private double   _resizeStartWidth;
    private IPointer? _resizePointer;

    // ── Brushes (dark / Fluent-compatible theme) ──────────────────────────────

    private static readonly IBrush HeaderBrush    = new SolidColorBrush(Color.FromRgb(45,  45,  48));
    private static readonly IBrush RowEven        = new SolidColorBrush(Color.FromRgb(30,  30,  30));
    private static readonly IBrush RowOdd         = new SolidColorBrush(Color.FromRgb(37,  37,  38));
    private static readonly IBrush SelectionBrush = new SolidColorBrush(Color.FromRgb(9,   71,  113));
    private static readonly IBrush DragBrush      = new SolidColorBrush(Color.FromArgb(120, 100, 149, 237));
    private static readonly IBrush DropLineBrush  = Brushes.CornflowerBlue;
    private static readonly IBrush RowBorderBrush = new SolidColorBrush(Color.FromRgb(60,  60,  60));
    private static readonly IBrush FgNormal       = new SolidColorBrush(Color.FromRgb(204, 204, 204));
    private static readonly IBrush FgDim          = new SolidColorBrush(Color.FromRgb(128, 128, 128));
    private static readonly IBrush GripperBrush   = new SolidColorBrush(Color.FromArgb(60,  255, 255, 255));

    // ── Converters ────────────────────────────────────────────────────────────

    private static readonly IValueConverter InverseBool =
        new FuncValueConverter<bool, bool>(b => !b);

    // ── Static initialiser ────────────────────────────────────────────────────

    static DraggableGrid()
    {
        ItemsSourceProperty.Changed .AddClassHandler<DraggableGrid>((g, e) => g.OnItemsSourceChanged(e));
        SelectedItemProperty.Changed.AddClassHandler<DraggableGrid>((g, _) => g.ResetRowStyles());
    }

    public DraggableGrid()
    {
        InitializeComponent();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Build();
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldColl)
            oldColl.CollectionChanged -= OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newColl)
            newColl.CollectionChanged += OnCollectionChanged;

        RebuildRows();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildRows();

    // ── Initial build ─────────────────────────────────────────────────────────

    private void Build()
    {
        // Initialise pixel widths from XAML declarations (preserved across rebuilds).
        if (_colWidths.Length != Columns.Count)
        {
            _colWidths = new double[Columns.Count];
            for (int i = 0; i < Columns.Count; i++)
            {
                var gl = Columns[i].Width;
                _colWidths[i] = gl.GridUnitType == GridUnitType.Pixel ? gl.Value : 100.0;
            }
        }

        _root = new Grid();
        _root.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // 0: header
        _root.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // 1: scroll area

        _headerBorder = MakeHeader();
        Grid.SetRow(_headerBorder, 0);
        _root.Children.Add(_headerBorder);

        _rowsPanel = new StackPanel();
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _rowsPanel
        };
        Grid.SetRow(scroll, 1);
        _root.Children.Add(scroll);

        Content = _root;
        RebuildRows();
    }

    // ── Column visibility API ─────────────────────────────────────────────────

    /// <summary>Shows or hides the column whose <see cref="DraggableGridColumn.Name"/> matches
    /// <paramref name="name"/>. The header and all rows are rebuilt; current column widths
    /// (including any resize the user has performed) are preserved.</summary>
    public void SetColumnVisible(string? name, bool visible)
    {
        var col = Columns.FirstOrDefault(c => c.Name == name);
        if (col is null || col.IsVisible == visible) return;
        col.IsVisible = visible;

        if (_root is null) return; // not yet attached to visual tree

        // Replace header in-place (MakeHeader also rebuilds _headerColDefs + _visibleToAllIdx).
        _root.Children.Remove(_headerBorder!);
        _headerBorder = MakeHeader();
        Grid.SetRow(_headerBorder, 0);
        _root.Children.Insert(0, _headerBorder);

        RebuildRows();
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private Border MakeHeader()
    {
        var visibleCols = Columns.Where(c => c.IsVisible).ToList();

        // Build the visible→all-index mapping.
        _visibleToAllIdx = visibleCols
            .Select(c => Columns.IndexOf(c))
            .ToArray();

        var g = new Grid { Background = HeaderBrush };
        _headerColDefs = new ColumnDefinition[visibleCols.Count];
        for (int i = 0; i < visibleCols.Count; i++)
        {
            var cd = new ColumnDefinition(new GridLength(_colWidths[_visibleToAllIdx[i]], GridUnitType.Pixel));
            _headerColDefs[i] = cd;
            g.ColumnDefinitions.Add(cd);
        }

        for (int i = 0; i < visibleCols.Count; i++)
        {
            var col = visibleCols[i];
            int allIdx = _visibleToAllIdx[i];
            int visIdx = i;

            // Each header cell is a Panel: TextBlock label + right-edge resize gripper.
            var label = new TextBlock
            {
                Text              = col.Header,
                Foreground        = Brushes.White,
                FontWeight        = FontWeight.SemiBold,
                Margin            = new Thickness(6, 5, 14, 5), // leave room for gripper
                VerticalAlignment = VerticalAlignment.Center
            };

            // Gripper: a thin transparent border on the right edge that responds to drag.
            var gripper = new Border
            {
                Width               = 5,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background          = GripperBrush,
                Cursor              = new Cursor(StandardCursorType.SizeWestEast)
            };
            ToolTip.SetTip(gripper, "Drag to resize column");

            gripper.PointerPressed  += (_, e) => OnGripperPointerPressed(gripper, allIdx, visIdx, e);
            gripper.PointerMoved    += (_, e) => OnGripperPointerMoved(e);
            gripper.PointerReleased += (_, e) => OnGripperPointerReleased(e);
            gripper.PointerCaptureLost += (_, _) =>
            {
                _resizePointer    = null;
                _resizingAllIdx   = -1;
                _resizingVisIdx   = -1;
            };

            var cell = new Panel();
            cell.Children.Add(label);
            cell.Children.Add(gripper);

            Grid.SetColumn(cell, i);
            g.Children.Add(cell);
        }

        return new Border
        {
            Child           = g,
            BorderBrush     = RowBorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
    }

    // ── Rows ──────────────────────────────────────────────────────────────────

    private void RebuildRows()
    {
        if (_rowsPanel is null) return;

        _rowsPanel.Children.Clear();
        _rowBorders.Clear();
        _rowColDefs.Clear();

        if (ItemsSource is null) { return; }

        var visibleCols = Columns
            .Select((c, i) => (col: c, allIdx: i))
            .Where(t => t.col.IsVisible)
            .ToList();

        int idx = 0;
        foreach (var item in ItemsSource)
        {
            var border = MakeRow(item, idx, visibleCols);
            _rowBorders.Add(border);
            _rowsPanel.Children.Add(border);
            idx++;
        }

        ResetRowStyles();
    }

    private Border MakeRow(object item, int index,
        IReadOnlyList<(DraggableGridColumn col, int allIdx)> visibleCols)
    {
        var g = new Grid();
        var colDefs = new ColumnDefinition[visibleCols.Count];
        for (int i = 0; i < visibleCols.Count; i++)
        {
            var cd = new ColumnDefinition(new GridLength(_colWidths[visibleCols[i].allIdx], GridUnitType.Pixel));
            colDefs[i] = cd;
            g.ColumnDefinitions.Add(cd);
        }
        _rowColDefs.Add(colDefs);

        for (int i = 0; i < visibleCols.Count; i++)
        {
            Control cell = BuildCell(visibleCols[i].col);
            Grid.SetColumn(cell, i);
            g.Children.Add(cell);
        }

        var border = new Border
        {
            Child           = g,
            DataContext     = item,   // enables Avalonia bindings in descendant cells
            BorderBrush     = RowBorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        border.PointerPressed     += OnRowPointerPressed;
        border.PointerMoved       += OnRowPointerMoved;
        border.PointerReleased    += OnRowPointerReleased;
        border.PointerCaptureLost += (_, _) => CancelDrag();

        return border;
    }

    /// <summary>Creates the appropriate cell control for a column.
    /// DataContext is inherited from the parent row Border, so Avalonia bindings work.</summary>
    private Control BuildCell(DraggableGridColumn col)
    {
        // Custom factory takes priority
        if (col.CellFactory is not null)
            return col.CellFactory(null!); // DataContext supplies the item via inheritance

        // Drag-handle glyph
        if (col.ColumnType == DraggableGridColumnType.DragHandle)
        {
            var handle = new TextBlock
            {
                Text                = "☰",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Cursor              = new Cursor(StandardCursorType.SizeNorthSouth),
                Foreground          = FgDim,
                FontSize            = 13
            };
            ToolTip.SetTip(handle, "Drag to reorder");
            return handle;
        }

        // Checkbox (read-only)
        if (col.ColumnType == DraggableGridColumnType.Checkbox)
        {
            var cb = new CheckBox
            {
                IsEnabled         = false,
                Margin            = new Thickness(6, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            cb.Bind(CheckBox.IsCheckedProperty, new Binding(col.Path));
            return cb;
        }

        // Editable TextBox — conditionally blocked via IsEnabledPath
        if (col.IsEditable)
        {
            var tb = new TextBox
            {
                Background        = Brushes.Transparent,
                BorderThickness   = new Thickness(0),
                Foreground        = FgNormal,
                Padding           = new Thickness(6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth          = 0
            };
            tb.Bind(TextBox.TextProperty, new Binding(col.Path) { Mode = BindingMode.TwoWay });
            tb.LostFocus += (_, _) => EditEnded?.Invoke(this, EventArgs.Empty);

            if (col.IsEnabledPath is null)
                return tb;

            // Wrap in a Panel with a placeholder so the blocked state shows "-"
            // at normal foreground colour rather than a greyed-out disabled TextBox.
            var placeholder = new TextBlock
            {
                Text              = "-",
                Foreground        = FgNormal,
                Margin            = new Thickness(6, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            placeholder.Bind(Visual.IsVisibleProperty,
                new Binding(col.IsEnabledPath) { Converter = InverseBool });

            tb.Bind(Visual.IsVisibleProperty, new Binding(col.IsEnabledPath));

            var panel = new Panel();
            panel.Children.Add(placeholder);
            panel.Children.Add(tb);
            return panel;
        }

        // Read-only TextBlock
        {
            var tb = new TextBlock
            {
                Foreground        = FgNormal,
                Margin            = new Thickness(6, 4),
                VerticalAlignment = VerticalAlignment.Center
            };

            var displayPath = col.DisplayPath ?? col.Path;
            tb.Bind(TextBlock.TextProperty,
                string.IsNullOrEmpty(col.NullText)
                    ? new Binding(displayPath)
                    : new Binding(displayPath) { TargetNullValue = col.NullText });

            if (col.OpacityPath is not null)
                tb.Bind(Visual.OpacityProperty, new Binding(col.OpacityPath)
                    { Converter = BoolToOpacityConverter.Instance });

            return tb;
        }
    }

    // ── Column resize ─────────────────────────────────────────────────────────

    private void OnGripperPointerPressed(Border gripper, int allIdx, int visIdx,
        PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(gripper).Properties.IsLeftButtonPressed) return;
        _resizingAllIdx   = allIdx;
        _resizingVisIdx   = visIdx;
        _resizeStartX     = e.GetPosition(this).X;
        _resizeStartWidth = _colWidths[allIdx];
        _resizePointer    = e.Pointer;
        e.Pointer.Capture(gripper);
        e.Handled = true;
    }

    private void OnGripperPointerMoved(PointerEventArgs e)
    {
        if (_resizingAllIdx < 0) return;
        double delta    = e.GetPosition(this).X - _resizeStartX;
        double newWidth = Math.Max(24, _resizeStartWidth + delta);
        ApplyColumnWidth(_resizingAllIdx, _resizingVisIdx, newWidth);
        e.Handled = true;
    }

    private void OnGripperPointerReleased(PointerReleasedEventArgs e)
    {
        _resizePointer?.Capture(null);
        _resizePointer  = null;
        _resizingAllIdx = -1;
        _resizingVisIdx = -1;
        e.Handled = true;
    }

    /// <summary>Updates the stored pixel width and all live ColumnDefinitions for one column.</summary>
    private void ApplyColumnWidth(int allIdx, int visIdx, double newWidth)
    {
        _colWidths[allIdx] = newWidth;
        var gl = new GridLength(newWidth, GridUnitType.Pixel);

        if (_headerColDefs is not null && (uint)visIdx < (uint)_headerColDefs.Length)
            _headerColDefs[visIdx].Width = gl;

        foreach (var rowDefs in _rowColDefs)
            if ((uint)visIdx < (uint)rowDefs.Length)
                rowDefs[visIdx].Width = gl;
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void ResetRowStyles()
    {
        for (int i = 0; i < _rowBorders.Count; i++)
        {
            bool sel = ReferenceEquals(_rowBorders[i].DataContext, SelectedItem);
            _rowBorders[i].Background      = sel ? SelectionBrush : (i % 2 == 0 ? RowEven : RowOdd);
            _rowBorders[i].BorderBrush     = RowBorderBrush;
            _rowBorders[i].BorderThickness = new Thickness(0, 0, 0, 1);
        }
    }

    // ── Drag-and-drop ─────────────────────────────────────────────────────────

    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        // Always update row selection on any click
        SelectedItem = border.DataContext;

        // Don't start drag when the user clicks inside an editable TextBox
        if (e.Source is TextBox) return;

        _dragIndex = FindItemIndex(border.DataContext);
        if (_dragIndex < 0) return;

        _dragStartPos  = e.GetPosition(this);
        _isDragging    = false;
        _draggedBorder = border;
    }

    private void OnRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragIndex < 0 || _rowsPanel is null) return;

        var delta = e.GetPosition(this) - _dragStartPos;
        if (!_isDragging && (Math.Abs(delta.X) + Math.Abs(delta.Y)) > 5)
        {
            _isDragging      = true;
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(_draggedBorder);
        }

        if (!_isDragging) return;

        ResetRowStyles();
        _draggedBorder!.Background = DragBrush;

        var panelY    = e.GetPosition(_rowsPanel).Y;
        int targetIdx = RowAtY(panelY);
        if (targetIdx >= 0 && targetIdx != _dragIndex)
        {
            bool above                     = panelY < RowCenterY(targetIdx);
            _rowBorders[targetIdx].BorderBrush     = DropLineBrush;
            _rowBorders[targetIdx].BorderThickness = above
                ? new Thickness(0, 2, 0, 0)
                : new Thickness(0, 0, 0, 2);
        }

        e.Handled = true;
    }

    private void OnRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging || _dragIndex < 0 || ItemsSource is null || _rowsPanel is null)
        {
            CancelDrag();
            return;
        }

        var panelY    = e.GetPosition(_rowsPanel).Y;
        int targetIdx = RowAtY(panelY);
        bool above    = panelY < RowCenterY(targetIdx);

        int    src         = _dragIndex;
        object draggedItem = ItemsSource[src]!;
        int    clampedTgt  = Math.Clamp(targetIdx, 0, ItemsSource.Count - 1);
        object targetItem  = ItemsSource[clampedTgt]!;

        ReleaseCaptureAndCancel();

        if (targetIdx >= 0 && targetIdx != src)
        {
            var args = new ItemMovedEventArgs(draggedItem, targetItem, above);
            ItemMoved?.Invoke(this, args);

            if (!args.Handled)
            {
                // Default: mutate the IList directly (standalone usage)
                ItemsSource.RemoveAt(src);
                int dest = targetIdx > src ? targetIdx - 1 : targetIdx;
                dest = above ? dest : dest + 1;
                dest = Math.Clamp(dest, 0, ItemsSource.Count);
                if (dest >= ItemsSource.Count)
                    ItemsSource.Add(draggedItem);
                else
                    ItemsSource.Insert(dest, draggedItem);
            }
        }

        e.Handled = true;
    }

    private void ReleaseCaptureAndCancel()
    {
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
        CancelDrag();
    }

    private void CancelDrag()
    {
        _isDragging    = false;
        _dragIndex     = -1;
        _draggedBorder = null;
        ResetRowStyles();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int FindItemIndex(object? item)
    {
        if (ItemsSource is null || item is null) return -1;
        for (int i = 0; i < ItemsSource.Count; i++)
            if (ReferenceEquals(ItemsSource[i], item)) return i;
        return -1;
    }

    private int RowAtY(double panelY)
    {
        for (int i = 0; i < _rowBorders.Count; i++)
        {
            var b = _rowBorders[i].Bounds;
            if (panelY >= b.Top && panelY < b.Bottom) return i;
        }
        return panelY < 0 ? 0 : Math.Max(0, _rowBorders.Count - 1);
    }

    private double RowCenterY(int index)
    {
        if ((uint)index >= (uint)_rowBorders.Count) return 0;
        var b = _rowBorders[index].Bounds;
        return b.Top + b.Height / 2;
    }
}
