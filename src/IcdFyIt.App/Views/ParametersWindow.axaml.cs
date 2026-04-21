using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class ParametersWindow : Window
{
    // ── Drag-to-reorder state ────────────────────────────────────────────────
    private ParameterRowViewModel? _dragSource;
    private ParameterRowViewModel? _dragTarget;
    private Point _dragStartPoint;
    private bool  _dragging;
    private bool  _isEditing;

    public ParametersWindow()
    {
        InitializeComponent();

        // Avalonia disconnects ContextMenu bindings while the popup is closed;
        // force a refresh when it opens so CanExecute values are current.
        ParamsGrid.ContextMenu!.Opened += (_, _) =>
            (DataContext as ParametersWindowViewModel)?.ForceCanEditNotify();

        // Drag-to-reorder: record press on the DataGrid (tunnel),
        // but move/release on the Window so they fire even after the DataGrid
        // captures the pointer for its own selection handling.
        ParamsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel);
        this.AddHandler(PointerMovedEvent,    OnPointerMoved,    RoutingStrategies.Tunnel, handledEventsToo: true);
        this.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        ParamsGrid.BeginningEdit += (_, _) => _isEditing = true;
        ParamsGrid.CellEditEnded += (_, _) => _isEditing = false;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not ParametersWindowViewModel vm) return;

        vm.RequestSaveCsvPath = async () =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Parameters CSV",
                SuggestedFileName = "parameters.csv",
                FileTypeChoices = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return file?.Path.LocalPath;
        };

        vm.RequestOpenCsvPath = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Parameters CSV",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        };
    }

    /// <summary>Marks the model edited when a DataGrid cell edit completes.</summary>
    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is ParametersWindowViewModel vm) vm.MarkEdited();
    }

    /// <summary>
    /// Cancels edit attempts for cells that are not applicable to the selected row's kind
    /// (e.g. Formula column for a non-SyntheticValue parameter).
    /// </summary>
    private void OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.DataContext is not ParameterRowViewModel row) return;
        var header = e.Column.Header?.ToString();
        bool applicable = header switch
        {
            "Formula"   => row.IsFormulaApplicable,
            "Hex Value" => row.IsHexValueApplicable,
            _           => true
        };
        if (!applicable) e.Cancel = true;
    }

    /// <summary>
    /// Toggles DataGrid column visibility from the integrated Columns menu (ICD-IF-150).
    /// </summary>
    private void OnColumnMenuItemChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        var visible = mi.IsChecked;
        var header = mi.Name switch
        {
            "MnemonicItem"  => "Mnemonic",
            "KindItem"      => "Kind",
            "NumericIdItem" => "Numeric ID",
            "ShortDescItem" => "Short Description",
            "LongDescItem"  => "Long Description",
            "DataTypeItem"  => "Data Type",
            "FormulaItem"   => "Formula",
            "HexValueItem"  => "Hex Value",
            _               => null
        };
        if (header is not null) SetColumnVisible(header, visible);
    }

    private void SetColumnVisible(string header, bool visible)
    {
        var col = ParamsGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == header);
        if (col is not null) col.IsVisible = visible;
    }

    // ── Drag-to-reorder handlers ──────────────────────────────────────────────

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(ParamsGrid).Properties.IsLeftButtonPressed) return;
        // Only start a drag when the press originates on the dedicated drag handle column.
        if (!IsOnDragHandle(e.Source)) return;

        var row = (e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true);
        _dragSource     = row?.DataContext as ParameterRowViewModel;
        _dragTarget     = null;
        _dragStartPoint = e.GetPosition(this);   // store relative to Window
        _dragging       = false;
        // Mark handled so the DataGrid does NOT capture the pointer for its own
        // selection/edit logic, leaving move/release events fully visible to us.
        e.Handled       = true;
    }

    /// <summary>Returns true when <paramref name="source"/> is inside a drag-handle cell.</summary>
    private static bool IsOnDragHandle(object? source)
    {
        var v = source as Visual;
        while (v is { } and not DataGridRow)
        {
            if (v.Classes.Contains("drag-handle")) return true;
            v = v.GetVisualParent();
        }
        return false;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSource is null || _isEditing) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) { CancelDrag(); return; }

        var pos = e.GetPosition(this);
        if (!_dragging)
        {
            if (Math.Abs(pos.X - _dragStartPoint.X) <= 5 && Math.Abs(pos.Y - _dragStartPoint.Y) <= 5) return;
            _dragging = true;
        }

        // Position-based hit test: works even when DataGrid has captured the pointer.
        var gridPos = e.GetPosition(ParamsGrid);
        var hitRow  = ParamsGrid.GetVisualsAt(gridPos).OfType<DataGridRow>().FirstOrDefault();
        var hovered = hitRow?.DataContext as ParameterRowViewModel;
        if (hovered is not null && hovered != _dragSource)
            _dragTarget = hovered;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var shouldMove = _dragging && !_isEditing && _dragSource is not null && _dragTarget is not null;
        var source = _dragSource;
        var target = _dragTarget;
        CancelDrag();
        if (shouldMove && DataContext is ParametersWindowViewModel vm)
            vm.MoveRow(source!, target!);
    }

    private void CancelDrag()
    {
        _dragSource = null;
        _dragTarget = null;
        _dragging   = false;
    }
}

