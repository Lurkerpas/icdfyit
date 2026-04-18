using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using IcdFyIt.App.ViewModels;
using IcdFyIt.App.Views;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // ── Service layer (singletons) ──────────────────────────────────────
            var changeNotifier  = new ChangeNotifier();
            var dirtyTracker    = new DirtyTracker();
            var undoRedoManager = new UndoRedoManager();
            var dataModelManager = new DataModelManager(changeNotifier, dirtyTracker, undoRedoManager);
            dataModelManager.New();

            // ── ViewModels ─────────────────────────────────────────────────────
            var mainVm        = new MainWindowViewModel(dataModelManager, dirtyTracker);
            var dataTypesVm   = new DataTypesWindowViewModel(dataModelManager, changeNotifier,
                                                             dirtyTracker, mainVm);

            // ── Main window ────────────────────────────────────────────────────
            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVm;
            desktop.MainWindow = mainWindow;

            // ── File-dialog delegates ──────────────────────────────────────────
            mainVm.OpenFileDialog = async () =>
            {
                var files = await mainWindow.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Open ICD File",
                        AllowMultiple = false,
                        FileTypeFilter =
                        [
                            new FilePickerFileType("ICD Files") { Patterns = ["*.xml"] },
                            new FilePickerFileType("All Files") { Patterns = ["*"] }
                        ]
                    });
                return files.Count > 0 ? files[0].Path.LocalPath : null;
            };

            mainVm.SaveFileDialog = async suggestedPath =>
            {
                var file = await mainWindow.StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "Save ICD File",
                        DefaultExtension = "xml",
                        SuggestedFileName = suggestedPath is { } p
                            ? System.IO.Path.GetFileName(p)
                            : "untitled.xml",
                        FileTypeChoices =
                        [
                            new FilePickerFileType("ICD Files") { Patterns = ["*.xml"] }
                        ]
                    });
                return file?.Path.LocalPath;
            };

            // ── Data Types window lifecycle ────────────────────────────────────
            DataTypesWindow? dataTypesWindow = null;

            // Wire Add dialog: shown attached to the DataTypes window (or mainWindow as fallback).
            dataTypesVm.RequestAddDataType = async () =>
            {
                Window owner = (dataTypesWindow is { IsVisible: true })
                    ? (Window)dataTypesWindow
                    : (Window)mainWindow;
                var dialog = new AddDataTypeDialog();
                return await dialog.ShowDialog<DataTypeCreationArgs?>(owner);
            };

            // Wire Enumerated Values pop-up dialog.
            dataTypesVm.RequestEditEnumeratedValues = async (et) =>
            {
                Window owner = (dataTypesWindow is { IsVisible: true })
                    ? (Window)dataTypesWindow
                    : (Window)mainWindow;
                var dialog = new EnumeratedValuesDialog
                {
                    DataContext = new EnumeratedValuesDialogViewModel(et)
                };
                await dialog.ShowDialog(owner);
            };

            // Wire Structure Fields pop-up dialog.
            dataTypesVm.RequestEditStructureFields = async (st) =>
            {
                Window owner = (dataTypesWindow is { IsVisible: true })
                    ? (Window)dataTypesWindow
                    : (Window)mainWindow;
                var dialog = new StructureFieldsDialog
                {
                    DataContext = new StructureFieldsDialogViewModel(st, changeNotifier.DataTypes.ToList())
                };
                await dialog.ShowDialog(owner);
            };

            // Wire Array Type pop-up dialog.
            dataTypesVm.RequestEditArrayType = async (at) =>
            {
                Window owner = (dataTypesWindow is { IsVisible: true })
                    ? (Window)dataTypesWindow
                    : (Window)mainWindow;
                var dialog = new ArrayTypeDialog
                {
                    DataContext = new ArrayTypeDialogViewModel(at, changeNotifier.DataTypes.ToList())
                };
                await dialog.ShowDialog(owner);
            };

            mainVm.ShowDataTypesWindow = () =>
            {
                if (dataTypesWindow is { IsVisible: true })
                {
                    dataTypesWindow.Activate();
                    return;
                }
                dataTypesWindow = new DataTypesWindow();
                dataTypesWindow.DataContext = dataTypesVm;
                dataTypesWindow.Show(mainWindow);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
