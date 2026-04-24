using Avalonia.Controls;
using Avalonia.Platform.Storage;
using IcdFyIt.App.Controls;
using IcdFyIt.App.Services;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class MetadataWindow : Window
{
    public MetadataWindow()
    {
        InitializeComponent();
        LayoutPersistenceManager.Register(this);

        MetadataGrid.ContextMenu!.Opened += (_, _) =>
            (DataContext as MetadataWindowViewModel)?.ForceCanEditNotify();

        MetadataGrid.EditEnded += (_, _) =>
            (DataContext as MetadataWindowViewModel)?.CommitEdits();

        MetadataGrid.ItemMoved += OnItemMoved;
    }

    private void OnItemMoved(object? sender, ItemMovedEventArgs e)
    {
        if (DataContext is not MetadataWindowViewModel vm) return;
        e.Handled = true;
        vm.MoveRow(
            (MetadataRowViewModel)e.DraggedItem,
            (MetadataRowViewModel)e.TargetItem);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not MetadataWindowViewModel vm) return;

        vm.RequestSaveCsvPath = async () =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Metadata CSV",
                SuggestedFileName = "metadata.csv",
                FileTypeChoices = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return file?.Path.LocalPath;
        };

        vm.RequestOpenCsvPath = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Metadata CSV",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        };
    }
}