using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using IcdFyIt.App.ViewModels;
using IcdFyIt.App.Views;
using IcdFyIt.Core.Export;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Services;
using System.IO;

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
            // ── Logging ────────────────────────────────────────────────────────
            LogManager.Initialise(Directory.GetCurrentDirectory());
            desktop.Exit += (_, _) => LogManager.Shutdown();

            // ── Service layer (singletons) ──────────────────────────────────────
            var changeNotifier   = new ChangeNotifier();
            var dirtyTracker     = new DirtyTracker();
            var undoRedoManager  = new UndoRedoManager();
            var dataModelManager = new DataModelManager(changeNotifier, dirtyTracker, undoRedoManager);
            var optionsManager   = new OptionsManager();
            var exportEngine     = new ExportEngine();
            // Apply persisted undo depth (ICD-IF-170, NC-04)
            undoRedoManager.MaxDepth = optionsManager.Load().UndoDepth;
            dataModelManager.New();

            // ── ViewModels ─────────────────────────────────────────────────────
            var mainVm         = new MainWindowViewModel(dataModelManager, changeNotifier, dirtyTracker, optionsManager);
            var dataTypesVm    = new DataTypesWindowViewModel(dataModelManager, changeNotifier,
                                                              dirtyTracker, mainVm);
            var parametersVm   = new ParametersWindowViewModel(dataModelManager, changeNotifier,
                                                               dirtyTracker, mainVm);
            var headerTypesVm  = new HeaderTypesWindowViewModel(dataModelManager, changeNotifier, mainVm);
            var memoriesVm     = new MemoriesWindowViewModel(dataModelManager, changeNotifier,
                                                            dirtyTracker, mainVm);
            var metadataVm     = new MetadataWindowViewModel(dataModelManager, changeNotifier, mainVm);
            var optionsVm      = new OptionsWindowViewModel(optionsManager);
            var exportVm       = new ExportWindowViewModel(dataModelManager, optionsManager, exportEngine);

            // ── Main window ────────────────────────────────────────────────────
            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVm;
            desktop.MainWindow = mainWindow;

            // ── Global unhandled exception handlers (ICD-IF-190) ──────────────
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                ShowFatalErrorDialog(mainWindow, e.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                e.SetObserved();
                // Suppress known benign platform errors (e.g. missing DBus AppMenu service on Linux).
                if (e.Exception.InnerExceptions.Any(ex => ex.Message.Contains("com.canonical.AppMenu")))
                    return;
                ShowFatalErrorDialog(mainWindow, e.Exception);
            };

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

            // ── Modal dialog delegates ─────────────────────────────────────────

            mainVm.RequestAddPacketTypeName = async () =>
            {
                var dialog = new AddPacketTypeDialog();
                return await dialog.ShowDialog<string?>(mainWindow);
            };

            mainVm.RequestConfirmDiscardChanges = async () =>
            {
                var dialog = new UnsavedChangesDialog();
                return await dialog.ShowDialog<string?>(mainWindow);
            };

            mainVm.ShowAboutWindowDialog = async () =>
            {
                var vm = new AboutWindowViewModel();
                var dialog = new AboutWindow { DataContext = vm };
                vm.RequestClose = dialog.Close;
                await dialog.ShowDialog(mainWindow);
            };

            mainVm.ShowValidationDialog = async issues =>
            {
                var dialog = new ValidationDialog
                {
                    DataContext = new ValidationDialogViewModel(issues)
                };
                await dialog.ShowDialog(mainWindow);
            };

            // ── Data Types window lifecycle ────────────────────────────────────
            DataTypesWindow? dataTypesWindow = null;

            // ── Parameters window lifecycle ────────────────────────────────────
            ParametersWindow? parametersWindow = null;

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

            // Wire Add Parameter dialog.
            parametersVm.RequestAddParameter = async () =>
            {
                Window owner = (parametersWindow is { IsVisible: true })
                    ? (Window)parametersWindow
                    : (Window)mainWindow;
                var dialog = new AddParameterDialog();
                return await dialog.ShowDialog<string?>(owner);
            };

            // Wire Parameter Attributes pop-up dialog.
            parametersVm.RequestEditAttributes = async (p) =>
            {
                Window owner = (parametersWindow is { IsVisible: true })
                    ? (Window)parametersWindow
                    : (Window)mainWindow;
                var dialog = new ParameterAttributesDialog
                {
                    DataContext = new ParameterAttributesDialogViewModel(
                        p,
                        changeNotifier.DataTypes.ToList(),
                        changeNotifier.Memories.ToList(),
                        changeNotifier.Parameters.ToList())
                };
                await dialog.ShowDialog(owner);
            };

            mainVm.ShowParametersWindow = () =>
            {
                if (parametersWindow is { IsVisible: true })
                {
                    parametersWindow.Activate();
                    return;
                }
                parametersWindow = new ParametersWindow();
                parametersWindow.DataContext = parametersVm;
                parametersWindow.Show(mainWindow);
            };

            // ── Header Types window lifecycle ──────────────────────────────────
            HeaderTypesWindow? headerTypesWindow = null;

            // ── Memories window lifecycle ──────────────────────────────────────
            MemoriesWindow? memoriesWindow = null;

            // ── Metadata window lifecycle ──────────────────────────────────────
            MetadataWindow? metadataWindow = null;

            headerTypesVm.RequestEditIdDataType = async (entry) =>
            {
                Window owner = (headerTypesWindow is { IsVisible: true })
                    ? (Window)headerTypesWindow
                    : (Window)mainWindow;
                var dialog = new HeaderTypeIdDataTypeDialog
                {
                    DataContext = new HeaderTypeIdDataTypeDialogViewModel(
                        entry, changeNotifier.DataTypes.ToList())
                };
                await dialog.ShowDialog(owner);
            };

            mainVm.ShowHeaderTypesWindow = () =>
            {
                if (headerTypesWindow is { IsVisible: true })
                {
                    headerTypesWindow.Activate();
                    return;
                }
                headerTypesWindow = new HeaderTypesWindow();
                headerTypesWindow.DataContext = headerTypesVm;
                headerTypesWindow.Show(mainWindow);
            };

            mainVm.ShowMemoriesWindow = () =>
            {
                if (memoriesWindow is { IsVisible: true })
                {
                    memoriesWindow.Activate();
                    return;
                }
                memoriesWindow = new MemoriesWindow();
                memoriesWindow.DataContext = memoriesVm;

                memoriesVm.RequestAddMemory = async () =>
                {
                    Window owner = (memoriesWindow is { IsVisible: true })
                        ? (Window)memoriesWindow
                        : (Window)mainWindow;
                    var dialog = new AddMemoryDialog();
                    return await dialog.ShowDialog<string?>(owner);
                };

                memoriesWindow.Show(mainWindow);
            };

            mainVm.ShowMetadataWindow = () =>
            {
                if (metadataWindow is { IsVisible: true })
                {
                    metadataWindow.Activate();
                    return;
                }

                metadataWindow = new MetadataWindow();
                metadataWindow.DataContext = metadataVm;
                metadataWindow.Show(mainWindow);
            };

            mainVm.RequestSelectHeaderType = async (packetType, availableTypes) =>
            {
                var dialog = new SelectHeaderTypeDialog
                {
                    DataContext = new SelectHeaderTypeDialogViewModel(packetType, availableTypes)
                };
                await dialog.ShowDialog(mainWindow);
            };

            // ── Options window lifecycle ───────────────────────────────────────
            OptionsWindow? optionsWindow = null;

            optionsVm.RequestBrowseTemplateFile = async currentPath =>
            {
                Window owner = (optionsWindow is { IsVisible: true })
                    ? (Window)optionsWindow
                    : (Window)mainWindow;
                var files = await owner.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Select Template File",
                        AllowMultiple = false,
                        FileTypeFilter =
                        [
                            new FilePickerFileType("Mako Templates") { Patterns = ["*.mako", "*.mak", "*.txt"] },
                            new FilePickerFileType("All Files")       { Patterns = ["*"] }
                        ]
                    });
                return files.Count > 0 ? files[0].Path.LocalPath : null;
            };

            mainVm.ShowOptionsWindow = () =>
            {
                if (optionsWindow is { IsVisible: true })
                {
                    optionsWindow.Activate();
                    return;
                }
                optionsVm.LoadFromOptions();
                optionsWindow = new OptionsWindow();
                optionsWindow.DataContext = optionsVm;
                optionsWindow.Show(mainWindow);
            };

            optionsVm.OnScaleSaved      = scale => mainVm.AppScale = scale;
            optionsVm.OnUndoDepthSaved  = depth => undoRedoManager.MaxDepth = depth;

            // ── Export window lifecycle ────────────────────────────────────────
            ExportWindow? exportWindow = null;

            exportVm.RequestBrowseOutputFolder = async () =>
            {
                Window owner = (exportWindow is { IsVisible: true })
                    ? (Window)exportWindow
                    : (Window)mainWindow;
                var folders = await owner.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "Select Output Folder", AllowMultiple = false });
                return folders.Count > 0 ? folders[0].Path.LocalPath : null;
            };

            mainVm.ShowExportWindow = () =>
            {
                if (exportWindow is { IsVisible: true })
                {
                    exportWindow.Activate();
                    return;
                }
                exportVm.Refresh();
                exportWindow = new ExportWindow();
                exportWindow.DataContext = exportVm;
                exportWindow.Show(mainWindow);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ShowFatalErrorDialog(Window? owner, Exception? ex)
    {
        var message = ex is not null
            ? $"An unexpected error occurred:\n\n{ex.Message}\n\n{ex.StackTrace}"
            : "An unexpected error occurred.";

        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Window? dialog = null;

            var textBlock = new Avalonia.Controls.SelectableTextBlock
            {
                Text        = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin      = new Avalonia.Thickness(12),
            };

            var copyButton = new Avalonia.Controls.Button
            {
                Content = "Copy to Clipboard",
                Margin  = new Avalonia.Thickness(4),
            };
            copyButton.Click += async (_, _) =>
            {
                if (dialog is not null)
                    await (TopLevel.GetTopLevel(dialog)?.Clipboard?.SetTextAsync(message) ?? Task.CompletedTask);
            };

            var closeButton = new Avalonia.Controls.Button
            {
                Content = "Close",
                Margin  = new Avalonia.Thickness(4),
            };
            closeButton.Click += (_, _) => dialog?.Close();

            dialog = new Avalonia.Controls.Window
            {
                Title          = "Unexpected Error",
                Width          = 600,
                MinHeight      = 200,
                SizeToContent  = Avalonia.Controls.SizeToContent.Height,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                Content        = new Avalonia.Controls.DockPanel
                {
                    Children =
                    {
                        new Avalonia.Controls.StackPanel
                        {
                            [Avalonia.Controls.DockPanel.DockProperty] = Avalonia.Controls.Dock.Bottom,
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Margin = new Avalonia.Thickness(8),
                            Children = { copyButton, closeButton }
                        },
                        new Avalonia.Controls.ScrollViewer
                        {
                            Content = textBlock
                        }
                    }
                }
            };

            if (owner is { IsVisible: true })
                await dialog.ShowDialog(owner);
            else
                dialog.Show();
        });
    }
}
