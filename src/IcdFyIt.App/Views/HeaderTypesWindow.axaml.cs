using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IcdFyIt.App.Controls;
using IcdFyIt.App.Services;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class HeaderTypesWindow : Window
{
    public HeaderTypesWindow()
    {
        InitializeComponent();
        LayoutPersistenceManager.Register(this);

        HeaderTypesGrid.EditEnded += (_, _) =>
            (DataContext as HeaderTypesWindowViewModel)?.MarkEdited();
        IdsGrid.EditEnded += (_, _) =>
            (DataContext as HeaderTypesWindowViewModel)?.MarkEdited();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not HeaderTypesWindowViewModel vm) return;

        vm.RequestSaveCsvPath = async () =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title             = "Export Header Types CSV",
                SuggestedFileName = "header_types.csv",
                FileTypeChoices   = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return file?.Path.LocalPath;
        };

        vm.RequestOpenCsvPath = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title          = "Import Header Types CSV",
                AllowMultiple  = false,
                FileTypeFilter = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        };
    }

    private void OnColumnMenuItemChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        var colName = mi.Name switch
        {
            "NameColItem"     => "Name",
            "MnemonicColItem" => "Mnemonic",
            "DescColItem"     => "Description",
            "IdsColItem"      => "IDs",
            _                 => null
        };
        if (colName is not null)
            HeaderTypesGrid.SetColumnVisible(colName, mi.IsChecked);
    }
}
