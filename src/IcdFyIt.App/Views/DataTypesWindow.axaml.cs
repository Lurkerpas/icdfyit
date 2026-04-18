using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class DataTypesWindow : Window
{
    public DataTypesWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not DataTypesWindowViewModel vm) return;

        vm.RequestSaveCsvPath = async () =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Data Types CSV",
                SuggestedFileName = "datatypes.csv",
                FileTypeChoices = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return file?.Path.LocalPath;
        };

        vm.RequestOpenCsvPath = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Data Types CSV",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        };
    }

    /// <summary>Marks the model edited when a DataGrid cell edit completes.</summary>
    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is DataTypesWindowViewModel vm) vm.MarkEdited();
    }

    /// <summary>
    /// Commits the Endianness cell immediately after a ComboBox selection.
    /// The dropdown is pre-opened (IsDropDownOpen=True) so selection is a single click.
    /// </summary>
    private void OnEndiannessSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        Dispatcher.UIThread.Post(() =>
        {
            TypesGrid.CommitEdit();
            if (DataContext is DataTypesWindowViewModel vm) vm.MarkEdited();
        });
    }

    /// <summary>
    /// Cancels attempts to edit cells that are not applicable to the selected row's type
    /// (e.g. Range columns for an Enumerated type).
    /// </summary>
    private void OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.DataContext is not DataTypeRowViewModel row) return;
        var header = e.Column.Header?.ToString();
        bool applicable = header switch
        {
            "Endianness"                                            => row.IsScalarApplicable,
            "Bit Size"                                              => row.IsBitSizeApplicable,
            "Range Min" or "Range Max" or "Unit" or "Calibration"  => row.IsNumericApplicable,
            _                                                       => true
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

        if (mi.Name == "RangeItem")
        {
            SetColumnVisible("Range Min", visible);
            SetColumnVisible("Range Max", visible);
        }
        else
        {
            var header = mi.Name switch
            {
                "EndiannessItem"  => "Endianness",
                "BitSizeItem"     => "Bit Size",
                "UnitItem"        => "Unit",
                "CalibrationItem" => "Calibration",
                "DetailsItem"     => "Details",
                _                 => null
            };
            if (header is not null) SetColumnVisible(header, visible);
        }
    }

    private void SetColumnVisible(string header, bool visible)
    {
        var col = TypesGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == header);
        if (col is not null) col.IsVisible = visible;
    }
}


