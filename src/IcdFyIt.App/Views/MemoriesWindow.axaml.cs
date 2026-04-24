using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IcdFyIt.App.Controls;
using IcdFyIt.App.Services;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class MemoriesWindow : Window
{
    public MemoriesWindow()
    {
        InitializeComponent();
        LayoutPersistenceManager.Register(this);

        MemoriesGrid.ContextMenu!.Opened += (_, _) =>
            (DataContext as MemoriesWindowViewModel)?.ForceCanEditNotify();

        MemoriesGrid.EditEnded += (_, _) =>
            (DataContext as MemoriesWindowViewModel)?.MarkEdited();

        MemoriesGrid.ItemMoved += OnItemMoved;
    }

    private void OnItemMoved(object? sender, ItemMovedEventArgs e)
    {
        if (DataContext is not MemoriesWindowViewModel vm) return;
        e.Handled = true;
        vm.MoveRow(
            (MemoryRowViewModel)e.DraggedItem,
            (MemoryRowViewModel)e.TargetItem);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not MemoriesWindowViewModel vm) return;

        vm.RequestSaveCsvPath = async () =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title             = "Export Memories CSV",
                SuggestedFileName = "memories.csv",
                FileTypeChoices   = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }],
            });
            return file?.Path.LocalPath;
        };

        vm.RequestOpenCsvPath = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title          = "Import Memories CSV",
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
            "NumericIdItem"   => "NumericId",
            "MnemonicItem"    => "Mnemonic",
            "SizeItem"        => "Size",
            "AddressItem"     => "Address",
            "DescriptionItem" => "Description",
            "AlignmentItem"   => "Alignment",
            "WritableItem"    => "IsWritable",
            "ReadableItem"    => "IsReadable",
            _                 => null
        };
        if (colName is not null)
            MemoriesGrid.SetColumnVisible(colName, mi.IsChecked);
    }
}
