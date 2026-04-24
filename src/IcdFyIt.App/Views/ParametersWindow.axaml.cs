using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IcdFyIt.App.Controls;
using IcdFyIt.App.Services;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class ParametersWindow : Window
{
    public ParametersWindow()
    {
        InitializeComponent();
        LayoutPersistenceManager.Register(this);

        // Avalonia disconnects ContextMenu bindings while the popup is closed;
        // force a refresh when it opens so CanExecute values are current.
        ParamsGrid.ContextMenu!.Opened += (_, _) =>
            (DataContext as ParametersWindowViewModel)?.ForceCanEditNotify();

        // Notify the ViewModel whenever an inline cell edit is committed.
        ParamsGrid.EditEnded += (_, _) =>
            (DataContext as ParametersWindowViewModel)?.MarkEdited();

        // Delegate drag-to-reorder to the ViewModel so the underlying model is
        // updated correctly (not just the FilteredRows ObservableCollection).
        ParamsGrid.ItemMoved += OnItemMoved;
    }

    private void OnItemMoved(object? sender, ItemMovedEventArgs e)
    {
        if (DataContext is not ParametersWindowViewModel vm) return;
        e.Handled = true; // suppress the default IList.RemoveAt / Insert path
        vm.MoveRow(
            (ParameterRowViewModel)e.DraggedItem,
            (ParameterRowViewModel)e.TargetItem);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not ParametersWindowViewModel vm) return;

        vm.RequestSaveCsvPath = async () =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title             = "Export Parameters CSV",
                SuggestedFileName = "parameters.csv",
                FileTypeChoices   = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return file?.Path.LocalPath;
        };

        vm.RequestOpenCsvPath = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title          = "Import Parameters CSV",
                AllowMultiple  = false,
                FileTypeFilter = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        };
    }

    /// <summary>
    /// Toggles column visibility from the integrated Columns menu (ICD-IF-150).
    /// The MenuItem x:Name maps to the DraggableGridColumn.Name used in SetColumnVisible.
    /// </summary>
    private void OnColumnMenuItemChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        var colName = mi.Name switch
        {
            "MnemonicItem"      => "Mnemonic",
            "KindItem"          => "Kind",
            "NumericIdItem"     => "NumericId",
            "ShortDescItem"     => "ShortDescription",
            "LongDescItem"      => "LongDescription",
            "DataTypeItem"      => "DataType",
            "FormulaItem"       => "Formula",
            "HexValueItem"      => "HexValue",
            "MemoryItem"        => "Memory",
            "MemoryOffsetItem"  => "MemoryOffset",
            "ValidityParamItem" => "ValidityParam",
            "AlarmLowItem"      => "AlarmLow",
            "AlarmHighItem"     => "AlarmHigh",
            _                   => null
        };
        if (colName is not null)
            ParamsGrid.SetColumnVisible(colName, mi.IsChecked);
    }
}

