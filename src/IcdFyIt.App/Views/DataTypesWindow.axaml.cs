using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using IcdFyIt.App.Controls;
using IcdFyIt.App.Converters;
using IcdFyIt.App.Services;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class DataTypesWindow : Window
{
    private static readonly IBrush FgNormal = new SolidColorBrush(Color.FromRgb(204, 204, 204));

    public DataTypesWindow()
    {
        InitializeComponent();
        LayoutPersistenceManager.Register(this);

        // CellFactory for the Endianness column (RadioButtons, no popup — safe on X11)
        TypesGrid.Columns.First(c => c.Name == "Endianness").CellFactory = _ =>
        {
            var le = new RadioButton { Content = "LE", Margin = new Thickness(0, 0, 8, 0) };
            le.Bind(RadioButton.IsCheckedProperty,
                new Binding(nameof(DataTypeRowViewModel.IsLittleEndian)) { Mode = BindingMode.TwoWay });
            ToolTip.SetTip(le, "Little endian");

            var be = new RadioButton { Content = "BE" };
            be.Bind(RadioButton.IsCheckedProperty,
                new Binding(nameof(DataTypeRowViewModel.IsBigEndian)) { Mode = BindingMode.TwoWay });
            ToolTip.SetTip(be, "Big endian");

            var sp = new StackPanel
            {
                Orientation       = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(6, 0)
            };
            sp.Bind(StackPanel.IsEnabledProperty,
                new Binding(nameof(DataTypeRowViewModel.IsScalarApplicable)));
            sp.Bind(Visual.OpacityProperty,
                new Binding(nameof(DataTypeRowViewModel.IsScalarApplicable))
                    { Converter = BoolToOpacityConverter.Instance });
            sp.Children.Add(le);
            sp.Children.Add(be);
            return sp;
        };

        // CellFactory for the Details column (text trimming + tooltip)
        TypesGrid.Columns.First(c => c.Name == "Details").CellFactory = _ =>
        {
            var tb = new TextBlock
            {
                TextTrimming      = TextTrimming.CharacterEllipsis,
                Foreground        = FgNormal,
                Padding           = new Thickness(6, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            tb.Bind(TextBlock.TextProperty,
                new Binding(nameof(DataTypeRowViewModel.Summary)) { TargetNullValue = "—" });
            tb.Bind(ToolTip.TipProperty,
                new Binding(nameof(DataTypeRowViewModel.Summary)));
            return tb;
        };

        // Avalonia disconnects ContextMenu bindings while the popup is closed,
        // so IsVisible values become stale. Force a refresh when it opens.
        TypesGrid.ContextMenu!.Opened += (_, _) =>
            (DataContext as DataTypesWindowViewModel)?.ForceCanEditNotify();

        // Notify the ViewModel whenever an inline cell edit is committed.
        TypesGrid.EditEnded += (_, _) =>
            (DataContext as DataTypesWindowViewModel)?.MarkEdited();
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

    /// <summary>
    /// Toggles DraggableGrid column visibility from the integrated Columns menu (ICD-IF-150).
    /// </summary>
    private void OnColumnMenuItemChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        var visible = mi.IsChecked;

        if (mi.Name == "RangeItem")
        {
            TypesGrid.SetColumnVisible("RangeMin", visible);
            TypesGrid.SetColumnVisible("RangeMax", visible);
        }
        else
        {
            var name = mi.Name switch
            {
                "EndiannessItem"  => "Endianness",
                "BitSizeItem"     => "BitSize",
                "UnitItem"        => "Unit",
                "CalibrationItem" => "Calibration",
                "DetailsItem"     => "Details",
                _                 => null
            };
            if (name is not null) TypesGrid.SetColumnVisible(name, visible);
        }
    }
}


