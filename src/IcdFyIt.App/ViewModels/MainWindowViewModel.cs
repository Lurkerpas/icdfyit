using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the main application window (ICD-DES §5.1).
/// File-dialog interactions and modal-dialog delegates are injected from the
/// composition root so the ViewModel remains View-free (testable).
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier   _changeNotifier;
    private readonly DirtyTracker     _dirtyTracker;
    private readonly OptionsManager   _optionsManager;
    private readonly AppOptions       _options;

    public MainWindowViewModel(
        DataModelManager dataModelManager,
        ChangeNotifier   changeNotifier,
        DirtyTracker     dirtyTracker,
        OptionsManager   optionsManager)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier   = changeNotifier;
        _dirtyTracker     = dirtyTracker;
        _optionsManager   = optionsManager;
        _options          = optionsManager.Load();

        foreach (var path in _options.RecentFiles)
            RecentFiles.Add(new RecentFileItemViewModel(path, OpenRecentFile));

        PacketTypeGroups = new ObservableCollection<PacketTypeGroupNode>
        {
            new PacketTypeGroupNode("Telecommands", Telecommands),
            new PacketTypeGroupNode("Telemetries",  Telemetries),
        };

        // Initialise tree collections from whatever is already in the notifier.
        foreach (var pt in _changeNotifier.PacketTypes)
            PlaceNode(CreateNode(pt));

        _changeNotifier.PacketTypes.CollectionChanged += OnPacketTypesChanged;
    }

    // ── Delegates wired from the composition root ──────────────────────────────

    /// <summary>Shows an Open-file-dialog; returns the chosen path or null if cancelled.</summary>
    public Func<Task<string?>>? OpenFileDialog { get; set; }

    /// <summary>Shows a Save-file-dialog; returns the chosen path or null if cancelled.</summary>
    public Func<string?, Task<string?>>? SaveFileDialog { get; set; }

    /// <summary>Opens (or focuses) the Data Types window.</summary>
    public Action? ShowDataTypesWindow { get; set; }

    /// <summary>Opens (or focuses) the Parameters window.</summary>
    public Action? ShowParametersWindow { get; set; }

    /// <summary>Opens (or focuses) the Header Types window.</summary>
    public Action? ShowHeaderTypesWindow { get; set; }

    /// <summary>Opens the Options window.</summary>
    public Action? ShowOptionsWindow { get; set; }

    /// <summary>Opens (or focuses) the Export window.</summary>
    public Action? ShowExportWindow { get; set; }

    /// <summary>Shows the About window as a modal dialog.</summary>
    public Func<Task>? ShowAboutWindowDialog { get; set; }

    /// <summary>
    /// Shows the Add Packet Type name dialog.
    /// Returns the trimmed name, or null when cancelled.
    /// </summary>
    public Func<Task<string?>>? RequestAddPacketTypeName { get; set; }

    /// <summary>
    /// Shows the unsaved-changes guard dialog (ICD-IF-180).
    /// Returns "save", "discard", or null (cancel).
    /// </summary>
    public Func<Task<string?>>? RequestConfirmDiscardChanges { get; set; }

    /// <summary>Shows the Validation-results dialog.</summary>
    public Func<IReadOnlyList<ValidationIssue>, Task>? ShowValidationDialog { get; set; }

    /// <summary>
    /// Shows the Header Type selection dialog for the given Packet Type.
    /// The delegate is responsible for showing the UI; after it returns the
    /// <see cref="PacketType.HeaderType"/> is expected to have been updated.
    /// </summary>
    public Func<PacketType, IReadOnlyList<HeaderType>, Task>? RequestSelectHeaderType { get; set; }

    // ── Packet-type tree collections ──────────────────────────────────────────

    public ObservableCollection<PacketTypeNodeViewModel>  Telecommands     { get; } = new();
    public ObservableCollection<PacketTypeNodeViewModel>  Telemetries      { get; } = new();
    public ObservableCollection<RecentFileItemViewModel>  RecentFiles      { get; } = new();
    public ObservableCollection<PacketTypeGroupNode>      PacketTypeGroups { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemovePacketTypeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicatePacketTypeCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectHeaderTypeCommand))]
    private PacketTypeNodeViewModel? _selectedPacketType;

    public bool CanActOnSelected => SelectedPacketType is not null;

    // ── Title bar ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _title = "icdfyit";

    public bool IsDirty => _dirtyTracker.IsDirty;

    private void RefreshTitle()
    {
        var file = _dataModelManager.CurrentFilePath is { } p
            ? System.IO.Path.GetFileName(p)
            : "Untitled";
        Title = _dirtyTracker.IsDirty
            ? $"{file}* — icdfyit"
            : $"{file} — icdfyit";
    }

    // ── Unsaved-changes guard (used by New/Open and the close guard) ──────────

    /// <summary>
    /// Returns true when it is safe to proceed (user saved or discarded).
    /// Returns false when user cancelled.
    /// </summary>
    private async Task<bool> GuardUnsavedChanges()
    {
        if (!_dirtyTracker.IsDirty) return true;
        if (RequestConfirmDiscardChanges is null) return true;

        var result = await RequestConfirmDiscardChanges();
        if (result is null) return false;           // cancel
        if (result == "save") await SaveDocumentCore();
        return true;
    }

    // ── File commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task NewDocument()
    {
        if (!await GuardUnsavedChanges()) return;
        _dataModelManager.New();
        RefreshTitle();
    }

    [RelayCommand]
    private async Task OpenDocument()
    {
        if (!await GuardUnsavedChanges()) return;
        var path = await (OpenFileDialog?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;
        _dataModelManager.Open(path);
        RecordRecentFile(path);
        RefreshTitle();
    }

    private async Task OpenRecentFile(string path)
    {
        if (!await GuardUnsavedChanges()) return;
        _dataModelManager.Open(path);
        RecordRecentFile(path);
        RefreshTitle();
    }

    private void RecordRecentFile(string path)
    {
        _options.RecentFiles.Remove(path);
        _options.RecentFiles.Insert(0, path);
        if (_options.RecentFiles.Count > 32)
            _options.RecentFiles.RemoveRange(32, _options.RecentFiles.Count - 32);
        _optionsManager.Save(_options);

        RecentFiles.Clear();
        foreach (var p in _options.RecentFiles)
            RecentFiles.Add(new RecentFileItemViewModel(p, OpenRecentFile));
    }

    [RelayCommand]
    private async Task SaveDocument() => await SaveDocumentCore();

    [RelayCommand]
    private async Task SaveDocumentAs()
    {
        var path = await (SaveFileDialog?.Invoke(_dataModelManager.CurrentFilePath)
            ?? Task.FromResult<string?>(null));
        if (path is null) return;
        _dataModelManager.Save(path);
        RefreshTitle();
    }

    private async Task SaveDocumentCore()
    {
        var path = _dataModelManager.CurrentFilePath
            ?? await (SaveFileDialog?.Invoke(null) ?? Task.FromResult<string?>(null));
        if (path is null) return;
        _dataModelManager.Save(path);
        RefreshTitle();
    }

    // ── Edit commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void Undo()
    {
        _dataModelManager.Undo();
        RefreshTitle();
    }

    [RelayCommand]
    private void Redo()
    {
        _dataModelManager.Redo();
        RefreshTitle();
    }

    // ── Packet-type CRUD (ICD-IF-61) ──────────────────────────────────────────

    [RelayCommand]
    private async Task AddTelecommand()
    {
        var name = await (RequestAddPacketTypeName?.Invoke() ?? Task.FromResult<string?>(null));
        if (string.IsNullOrWhiteSpace(name)) return;
        _dataModelManager.AddPacketType(name, PacketTypeKind.Telecommand);
        MarkEdited();
    }

    [RelayCommand]
    private async Task AddTelemetry()
    {
        var name = await (RequestAddPacketTypeName?.Invoke() ?? Task.FromResult<string?>(null));
        if (string.IsNullOrWhiteSpace(name)) return;
        _dataModelManager.AddPacketType(name, PacketTypeKind.Telemetry);
        MarkEdited();
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelected))]
    private void RemovePacketType()
    {
        if (SelectedPacketType is null) return;
        _dataModelManager.RemovePacketType(SelectedPacketType.Model);
        MarkEdited();
    }

    [RelayCommand]
    private void AddField() => SelectedPacketType?.AddField();

    [RelayCommand(CanExecute = nameof(CanActOnSelected))]
    private void DuplicatePacketType()
    {
        if (SelectedPacketType is null) return;
        _dataModelManager.DuplicatePacketType(SelectedPacketType.Model);
        MarkEdited();
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelected))]
    private async Task SelectHeaderType()
    {
        if (SelectedPacketType is null) return;
        if (RequestSelectHeaderType is not null)
            await RequestSelectHeaderType(SelectedPacketType.Model, _changeNotifier.HeaderTypes.ToList());
        SelectedPacketType.RefreshHeaderType();
        MarkEdited();
    }

    // ── Appearance / UI scale ─────────────────────────────────────────────────

    private static readonly double[] _scales = [1.0, 1.5, 2.0, 3.0];

    [ObservableProperty]
    private double _appScale = 1.0;

    partial void OnAppScaleChanged(double value)
    {
        if (Application.Current is not { } app) return;
        app.Resources["AppScale"]       = value;
        app.Resources["AppMenuFontSize"] = value * 14.0;
    }

    [RelayCommand]
    private void IncreaseSize()
    {
        var idx = Array.IndexOf(_scales, AppScale);
        if (idx < _scales.Length - 1)
            AppScale = _scales[idx + 1];
    }

    [RelayCommand]
    private void DecreaseSize()
    {
        var idx = Array.IndexOf(_scales, AppScale);
        if (idx > 0)
            AppScale = _scales[idx - 1];
    }

    [RelayCommand] private void SetSizeSmall()    => AppScale = 1.0;
    [RelayCommand] private void SetSizeMedium()   => AppScale = 1.5;
    [RelayCommand] private void SetSizeLarge()    => AppScale = 2.0;
    [RelayCommand] private void SetSizeVeryLarge() => AppScale = 3.0;

    // ── Window navigation ──────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenDataTypes() => ShowDataTypesWindow?.Invoke();

    [RelayCommand]
    private void OpenParameters() => ShowParametersWindow?.Invoke();

    [RelayCommand]
    private void OpenHeaderTypes() => ShowHeaderTypesWindow?.Invoke();

    [RelayCommand]
    private void OpenExportWindow() => ShowExportWindow?.Invoke();

    [RelayCommand]
    private void OpenOptions() => ShowOptionsWindow?.Invoke();

    [RelayCommand]
    private async Task OpenAbout()
    {
        if (ShowAboutWindowDialog is not null)
            await ShowAboutWindowDialog();
    }

    [RelayCommand]
    private void OpenHelp()
    {
        Process.Start(new ProcessStartInfo("https://github.com/Lurkerpas/icdfyit")
        {
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task RunValidation()
    {
        var validator = new ModelValidator();
        var issues    = validator.Validate(_dataModelManager.CurrentModel);
        if (ShowValidationDialog is not null)
            await ShowValidationDialog(issues);
    }

    // ── Dirty notification ────────────────────────────────────────────────────

    public void NotifyModelEdited() => MarkEdited();

    private void MarkEdited()
    {
        _dirtyTracker.MarkDirty();
        RefreshTitle();
    }

    // ── PacketType collection sync ────────────────────────────────────────────

    private void OnPacketTypesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (PacketType pt in e.NewItems!)
                    PlaceNode(CreateNode(pt));
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (PacketType pt in e.OldItems!)
                    RemoveNodeFromCollections(pt);
                break;

            case NotifyCollectionChangedAction.Reset:
                Telecommands.Clear();
                Telemetries.Clear();
                SelectedPacketType = null;
                foreach (var pt in _changeNotifier.PacketTypes)
                    PlaceNode(CreateNode(pt));
                break;
        }
    }

    private void PlaceNode(PacketTypeNodeViewModel node)
    {
        if (node.Model.Kind == PacketTypeKind.Telecommand)
            Telecommands.Add(node);
        else
            Telemetries.Add(node);
    }

    private void RemoveNodeFromCollections(PacketType pt)
    {
        var tc = Telecommands.FirstOrDefault(n => n.Model == pt);
        if (tc is not null) { Telecommands.Remove(tc); if (SelectedPacketType == tc) SelectedPacketType = null; return; }
        var tm = Telemetries.FirstOrDefault(n => n.Model == pt);
        if (tm is not null) { Telemetries.Remove(tm);  if (SelectedPacketType == tm) SelectedPacketType = null; }
    }

    private PacketTypeNodeViewModel CreateNode(PacketType pt)
    {
        var node = new PacketTypeNodeViewModel(pt, _changeNotifier.Parameters);
        node.OnEdited = MarkEdited;
        return node;
    }
}
