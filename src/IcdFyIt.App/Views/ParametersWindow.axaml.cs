using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class ParametersWindow : Window
{
    public ParametersWindow()
    {
        InitializeComponent();

        // Avalonia disconnects ContextMenu bindings while the popup is closed;
        // force a refresh when it opens so CanExecute values are current.
        ParamsGrid.ContextMenu!.Opened += (_, _) =>
            (DataContext as ParametersWindowViewModel)?.ForceCanEditNotify();
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
}

