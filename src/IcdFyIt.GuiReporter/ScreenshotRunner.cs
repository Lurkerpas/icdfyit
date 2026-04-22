using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using IcdFyIt.App.ViewModels;
using IcdFyIt.App.Views;
using IcdFyIt.Core.Export;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.GuiReporter;

/// <summary>
/// Captures PNG screenshots of every window and dialog in the app at every supported
/// UI scale (1.0, 1.5, 2.0, 3.0) and writes them to <c>GuiReport/&lt;scale&gt;x/</c>.
/// </summary>
public sealed class ScreenshotRunner
{
    // UI scale levels that match the app's View → Options → Size settings.
    private static readonly double[] Scales = [1.0, 1.5, 2.0, 3.0];

    // Base window dimensions (logical pixels at scale 1.0), matched to the XAML declarations.
    // At a given scale the window is sized to base * scale so the LayoutTransformControl
    // scaled content fills the window exactly.
    private static readonly Dictionary<string, (double W, double H)> BaseSizes = new()
    {
        ["main"]              = (1024, 768),
        ["data_types"]        = (1100, 640),
        ["parameters"]        = (1100, 640),
        ["header_types"]      = (950,  620),
        ["options"]           = (760,  540),
        ["export"]            = (560,  320),
        ["about"]             = (380,  220),
        // Dialogs
        ["add_packet_type"]   = (340,  160),
        ["add_data_type"]     = (340,  160),
        ["add_parameter"]     = (340,  160),
        ["unsaved_changes"]   = (400,  180),
        ["validation"]        = (600,  400),
        ["enum_values"]       = (480,  380),
        ["struct_fields"]     = (520,  420),
        ["array_type"]        = (480,  340),
        ["param_attributes"]  = (380,  220),
        ["select_header_type"]= (400,  220),
        ["header_id_datatype"]= (380,  130),
    };

    private readonly string _modelPath;
    private readonly string _outputDir;

    // ── Shared service layer (created once, reused across scales) ─────────────

    private DataModelManager  _dataModelManager = null!;
    private ChangeNotifier    _changeNotifier   = null!;
    private DirtyTracker      _dirtyTracker     = null!;
    private UndoRedoManager   _undoRedoManager  = null!;
    private OptionsManager    _optionsManager   = null!;
    private ExportEngine      _exportEngine     = null!;

    public ScreenshotRunner(string modelPath, string outputDir)
    {
        _modelPath = modelPath;
        _outputDir = outputDir;
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public async Task RunAsync()
    {
        SetupServices();

        Directory.CreateDirectory(_outputDir);
        Console.WriteLine($"Model loaded from: {_modelPath}");
        Console.WriteLine($"Output directory : {Path.GetFullPath(_outputDir)}");

        foreach (var scale in Scales)
        {
            Console.WriteLine($"\n── Scale {scale:F1}x ────────────────────────────────────────────");
            ApplyScale(scale);

            var scaleDir = Path.Combine(_outputDir, $"{scale:F1}x");
            Directory.CreateDirectory(scaleDir);

            await CaptureAllAsync(scaleDir, scale);
        }
    }

    // ── Service setup ─────────────────────────────────────────────────────────

    private void SetupServices()
    {
        _changeNotifier   = new ChangeNotifier();
        _dirtyTracker     = new DirtyTracker();
        _undoRedoManager  = new UndoRedoManager();
        _dataModelManager = new DataModelManager(_changeNotifier, _dirtyTracker, _undoRedoManager);
        _optionsManager   = new OptionsManager();
        _exportEngine     = new ExportEngine();

        _dataModelManager.Open(_modelPath);
    }

    private static void ApplyScale(double scale)
    {
        if (Application.Current is not { } app) return;
        app.Resources["AppScale"]       = scale;
        app.Resources["AppMenuFontSize"] = Math.Round(14.0 * scale, 0);
    }

    // ── Main capture loop ─────────────────────────────────────────────────────

    private async Task CaptureAllAsync(string dir, double scale)
    {
        // ── Main window ───────────────────────────────────────────────────────
        await CaptureAsync(dir, "01_main_window", scale, "main", () =>
        {
            var vm = BuildMainVm();
            var w = new MainWindow { DataContext = vm };
            return w;
        });

        // ── Data Types window ─────────────────────────────────────────────────
        await CaptureAsync(dir, "02_data_types_window", scale, "data_types", () =>
        {
            var mainVm = BuildMainVm();
            var vm = new DataTypesWindowViewModel(_dataModelManager, _changeNotifier, _dirtyTracker, mainVm);
            StubDataTypesDialogs(vm);
            var w = new DataTypesWindow { DataContext = vm };
            return w;
        });

        // ── Parameters window ─────────────────────────────────────────────────
        await CaptureAsync(dir, "03_parameters_window", scale, "parameters", () =>
        {
            var mainVm = BuildMainVm();
            var vm = new ParametersWindowViewModel(_dataModelManager, _changeNotifier, _dirtyTracker, mainVm);
            StubParametersDialogs(vm);
            var w = new ParametersWindow { DataContext = vm };
            return w;
        });

        // ── Header Types window ───────────────────────────────────────────────
        await CaptureAsync(dir, "04_header_types_window", scale, "header_types", () =>
        {
            var mainVm = BuildMainVm();
            var vm = new HeaderTypesWindowViewModel(_dataModelManager, _changeNotifier, mainVm);
            StubHeaderTypesDialogs(vm);
            var w = new HeaderTypesWindow { DataContext = vm };
            return w;
        });

        // ── Options window ────────────────────────────────────────────────────
        await CaptureAsync(dir, "05_options_window", scale, "options", () =>
        {
            var vm = new OptionsWindowViewModel(_optionsManager);
            return new OptionsWindow { DataContext = vm };
        });

        // ── Export window ─────────────────────────────────────────────────────
        await CaptureAsync(dir, "06_export_window", scale, "export", () =>
        {
            var vm = new ExportWindowViewModel(_dataModelManager, _optionsManager, _exportEngine);
            return new ExportWindow { DataContext = vm };
        });

        // ── About window ──────────────────────────────────────────────────────
        await CaptureAsync(dir, "07_about_window", scale, "about", () =>
            new AboutWindow { DataContext = new AboutWindowViewModel() });

        // ── Add Packet Type dialog ─────────────────────────────────────────────
        await CaptureAsync(dir, "08_add_packet_type_dialog", scale, "add_packet_type",
            () => new AddPacketTypeDialog());

        // ── Add Data Type dialog ──────────────────────────────────────────────
        await CaptureAsync(dir, "09_add_data_type_dialog", scale, "add_data_type",
            () => new AddDataTypeDialog());

        // ── Add Parameter dialog ──────────────────────────────────────────────
        await CaptureAsync(dir, "10_add_parameter_dialog", scale, "add_parameter",
            () => new AddParameterDialog());

        // ── Unsaved Changes dialog ────────────────────────────────────────────
        await CaptureAsync(dir, "11_unsaved_changes_dialog", scale, "unsaved_changes",
            () => new UnsavedChangesDialog());

        // ── Validation dialog ─────────────────────────────────────────────────
        await CaptureAsync(dir, "12_validation_dialog", scale, "validation", () =>
        {
            var issues = new[]
            {
                new ValidationIssue("Packet field 'N' references a removed parameter."),
                new ValidationIssue("Parameter 'FixedN1' has no data type assigned."),
                new ValidationIssue("Header type 'PUSC-HTM' has no ID fields."),
            };
            var vm = new ValidationDialogViewModel(issues);
            return new ValidationDialog { DataContext = vm };
        });

        // ── Enumerated Values dialog ──────────────────────────────────────────
        var enumType = _changeNotifier.DataTypes.OfType<EnumeratedType>().FirstOrDefault();
        if (enumType is not null)
            await CaptureAsync(dir, "13_enum_values_dialog", scale, "enum_values", () =>
            {
                var vm = new EnumeratedValuesDialogViewModel(enumType);
                return new EnumeratedValuesDialog { DataContext = vm };
            });

        // ── Structure Fields dialog ───────────────────────────────────────────
        var structType = _changeNotifier.DataTypes.OfType<StructureType>().FirstOrDefault();
        if (structType is not null)
            await CaptureAsync(dir, "14_struct_fields_dialog", scale, "struct_fields", () =>
            {
                var allTypes = _changeNotifier.DataTypes.ToList();
                var vm = new StructureFieldsDialogViewModel(structType, allTypes);
                return new StructureFieldsDialog { DataContext = vm };
            });

        // ── Array Type dialog ─────────────────────────────────────────────────
        var arrayType = _changeNotifier.DataTypes.OfType<ArrayType>().FirstOrDefault();
        if (arrayType is not null)
            await CaptureAsync(dir, "15_array_type_dialog", scale, "array_type", () =>
            {
                var allTypes = _changeNotifier.DataTypes.ToList();
                var vm = new ArrayTypeDialogViewModel(arrayType, allTypes);
                return new ArrayTypeDialog { DataContext = vm };
            });

        // ── Parameter Attributes dialog ───────────────────────────────────────
        var param = _changeNotifier.Parameters.FirstOrDefault();
        if (param is not null)
            await CaptureAsync(dir, "16_param_attributes_dialog", scale, "param_attributes", () =>
            {
                var allTypes = _changeNotifier.DataTypes.ToList();
                var vm = new ParameterAttributesDialogViewModel(param, allTypes);
                return new ParameterAttributesDialog { DataContext = vm };
            });

        // ── Select Header Type dialog ─────────────────────────────────────────
        var packetType = _changeNotifier.PacketTypes.FirstOrDefault();
        var allHeaderTypes = _changeNotifier.HeaderTypes.ToList();
        if (packetType is not null && allHeaderTypes.Count > 0)
            await CaptureAsync(dir, "17_select_header_type_dialog", scale, "select_header_type", () =>
            {
                var vm = new SelectHeaderTypeDialogViewModel(packetType, allHeaderTypes);
                return new SelectHeaderTypeDialog { DataContext = vm };
            });

        // ── Header Type ID Data Type dialog ───────────────────────────────────
        var headerType = _changeNotifier.HeaderTypes.FirstOrDefault();
        var firstId = headerType?.Ids.FirstOrDefault();
        if (firstId is not null)
            await CaptureAsync(dir, "18_header_id_datatype_dialog", scale, "header_id_datatype", () =>
            {
                var allTypes = _changeNotifier.DataTypes.ToList();
                var vm = new HeaderTypeIdDataTypeDialogViewModel(firstId, allTypes);
                return new HeaderTypeIdDataTypeDialog { DataContext = vm };
            });
    }

    // ── Capture a single window ───────────────────────────────────────────────

    /// <param name="dir">Output directory for this scale.</param>
    /// <param name="filename">File name without extension.</param>
    /// <param name="scale">Current UI scale factor.</param>
    /// <param name="sizeKey">Key in <see cref="BaseSizes"/> to look up base dimensions.</param>
    /// <param name="factory">Creates the window. Called on the UI thread.</param>
    private async Task CaptureAsync(string dir, string filename, double scale,
        string sizeKey, Func<Window> factory)
    {
        var path = Path.Combine(dir, filename + ".png");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Window? window = null;
            try
            {
                window = factory();

                // Set window dimensions to base * scale so the LayoutTransformControl
                // scaled content fits exactly inside the window.
                if (BaseSizes.TryGetValue(sizeKey, out var base_))
                {
                    window.Width  = base_.W * scale;
                    window.Height = base_.H * scale;
                }

                window.Show();

                // Force a synchronous layout pass.
                window.Measure(new Size(window.Width, window.Height));
                window.Arrange(new Rect(new Size(window.Width, window.Height)));

                Capture(window, path);

                Console.WriteLine($"  {filename}.png");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  [SKIP] {filename}: {ex.Message}");
            }
            finally
            {
                window?.Close();
            }
        });

        // Flush pending dispatcher work between windows (layout, close animations, etc.).
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    // ── Bitmap capture ────────────────────────────────────────────────────────

    private static void Capture(Window window, string path)
    {
        int w = (int)Math.Max(1, window.Bounds.Width  > 0 ? window.Bounds.Width  : window.Width);
        int h = (int)Math.Max(1, window.Bounds.Height > 0 ? window.Bounds.Height : window.Height);

        using var rtb = new RenderTargetBitmap(new PixelSize(w, h), new Vector(96, 96));
        rtb.Render(window);
        rtb.Save(path);
    }

    // ── ViewModel factory helpers ─────────────────────────────────────────────

    private MainWindowViewModel BuildMainVm()
    {
        var vm = new MainWindowViewModel(_dataModelManager, _changeNotifier, _dirtyTracker, _optionsManager);
        // Wire no-op file/dialog delegates so the VM doesn't throw on commands.
        vm.OpenFileDialog  = () => Task.FromResult<string?>(null);
        vm.SaveFileDialog  = _ => Task.FromResult<string?>(null);
        vm.RequestAddPacketTypeName    = () => Task.FromResult<string?>(null);
        vm.RequestConfirmDiscardChanges = () => Task.FromResult<string?>(null);
        vm.ShowAboutWindowDialog       = () => Task.CompletedTask;
        vm.ShowValidationDialog        = _ => Task.CompletedTask;
        vm.ShowDataTypesWindow         = () => { };
        vm.ShowParametersWindow        = () => { };
        vm.ShowHeaderTypesWindow       = () => { };
        vm.ShowExportWindow            = () => { };
        vm.ShowOptionsWindow           = () => { };
        vm.RequestSelectHeaderType     = (_, _) => Task.CompletedTask;
        return vm;
    }

    private void StubDataTypesDialogs(DataTypesWindowViewModel vm)
    {
        vm.RequestAddDataType           = () => Task.FromResult<DataTypeCreationArgs?>(null);
        vm.RequestEditEnumeratedValues  = _ => Task.CompletedTask;
        vm.RequestEditStructureFields   = _ => Task.CompletedTask;
        vm.RequestEditArrayType         = _ => Task.CompletedTask;
    }

    private void StubParametersDialogs(ParametersWindowViewModel vm)
    {
        vm.RequestAddParameter    = () => Task.FromResult<string?>(null);
        vm.RequestEditAttributes = _ => Task.CompletedTask;
        vm.RequestSaveCsvPath    = () => Task.FromResult<string?>(null);
        vm.RequestOpenCsvPath    = () => Task.FromResult<string?>(null);
    }

    private void StubHeaderTypesDialogs(HeaderTypesWindowViewModel vm)
    {
        vm.RequestEditIdDataType = _ => Task.CompletedTask;
    }
}
