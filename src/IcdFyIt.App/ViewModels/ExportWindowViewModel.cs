using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Export;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Export window (ICD-DES §5.5).
/// </summary>
public partial class ExportWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly OptionsManager   _optionsManager;
    private readonly ExportEngine     _exportEngine;

    /// <summary>Delegate for opening a folder picker; returns the selected path or null.</summary>
    public Func<Task<string?>>? RequestBrowseOutputFolder { get; set; }

    public ExportWindowViewModel(
        DataModelManager dataModelManager,
        OptionsManager   optionsManager,
        ExportEngine     exportEngine)
    {
        _dataModelManager = dataModelManager;
        _optionsManager   = optionsManager;
        _exportEngine     = exportEngine;
    }

    // ── Bindable state ────────────────────────────────────────────────────────

    public ObservableCollection<TemplateSetConfig> TemplateSets { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private TemplateSetConfig? _selectedTemplateSet;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private string _outputFolder = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    [NotifyPropertyChangedFor(nameof(IsNotExporting))]
    private bool _isExporting;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Convenience inverse of <see cref="IsExporting"/> for AXAML <c>IsEnabled</c> bindings.</summary>
    public bool IsNotExporting => !IsExporting;

    // ── Refresh ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reloads the template-set list from disk settings.
    /// Call each time the window is about to be shown so the list reflects the latest saved options.
    /// </summary>
    public void Refresh()
    {
        var opts = _optionsManager.Load();
        TemplateSets.Clear();
        foreach (var ts in opts.TemplateSets)
            TemplateSets.Add(ts);
        SelectedTemplateSet = TemplateSets.Count > 0 ? TemplateSets[0] : null;
        StatusMessage       = null;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task BrowseOutputFolder()
    {
        if (RequestBrowseOutputFolder is null) return;
        var path = await RequestBrowseOutputFolder();
        if (path is not null)
            OutputFolder = path;
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task Export()
    {
        if (SelectedTemplateSet is null) return;

        IsExporting   = true;
        StatusMessage = "Exporting…";
        try
        {
            var model       = _dataModelManager.CurrentModel;
            var settingsDir = _optionsManager.SettingsDirectory;
            var pythonPath  = _optionsManager.Load().PythonPath;
            var set         = SelectedTemplateSet;
            var folder      = OutputFolder;

            await Task.Run(() =>
                _exportEngine.Export(model, set, settingsDir, folder, pythonPath));

            StatusMessage = $"Export complete — {set.Templates.Count} file(s) written to: {folder}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanExport =>
        SelectedTemplateSet is not null &&
        !string.IsNullOrWhiteSpace(OutputFolder) &&
        !IsExporting;
}
