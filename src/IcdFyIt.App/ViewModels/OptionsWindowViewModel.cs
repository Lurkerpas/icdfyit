using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Options window (ICD-DES §5.6).
/// Works on a private working copy of the options; Save persists, Cancel discards (ICD-FUN-100).
/// </summary>
public partial class OptionsWindowViewModel : ObservableObject
{
    private readonly OptionsManager _optionsManager;

    /// <summary>Injected by App.axaml.cs to open a file-picker for template files.</summary>
    public Func<string?, Task<string?>>? RequestBrowseTemplateFile { get; set; }

    /// <summary>Invoked by Save/Cancel commands to request the window to close.</summary>
    public Action? RequestClose { get; set; }

    public OptionsWindowViewModel(OptionsManager optionsManager)
    {
        _optionsManager = optionsManager;
    }

    // ── General tab ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private int _undoDepth = 64;

    [ObservableProperty]
    private string _pythonPath = string.Empty;

    // ── Template Sets tab ─────────────────────────────────────────────────────

    public ObservableCollection<TemplateSetRowViewModel> TemplateSets { get; } = new();
    public ObservableCollection<TemplateRowViewModel>    Templates    { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemplateSectionTitle))]
    [NotifyCanExecuteChangedFor(nameof(RemoveTemplateSetCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddTemplateCommand))]
    private TemplateSetRowViewModel? _selectedTemplateSet;

    partial void OnSelectedTemplateSetChanged(TemplateSetRowViewModel? value) => RebuildTemplates(value);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveTemplateCommand))]
    private TemplateRowViewModel? _selectedTemplate;

    public string TemplateSectionTitle => SelectedTemplateSet is not null
        ? $"Templates \u2014 {SelectedTemplateSet.Name}"
        : "Templates";

    // ── Load ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates all editable fields from disk.
    /// Called each time the window is about to be shown so Cancel always discards to disk state.
    /// </summary>
    public void LoadFromOptions()
    {
        var opts = _optionsManager.Load();
        UndoDepth  = opts.UndoDepth;
        PythonPath = opts.PythonPath ?? string.Empty;

        TemplateSets.Clear();
        Templates.Clear();
        SelectedTemplateSet = null;
        SelectedTemplate    = null;

        foreach (var ts in opts.TemplateSets)
            TemplateSets.Add(new TemplateSetRowViewModel(new TemplateSetConfig
            {
                Name        = ts.Name,
                Description = ts.Description,
                Templates   = ts.Templates.Select(t => new TemplateConfig
                {
                    Name              = t.Name,
                    Description       = t.Description,
                    FilePath          = t.FilePath,
                    OutputNamePattern = t.OutputNamePattern,
                }).ToList()
            }));
    }

    // ── Save / Cancel ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        // Reload to preserve RecentFiles and any other settings not owned by this window.
        var opts = _optionsManager.Load();
        opts.UndoDepth    = UndoDepth;
        opts.PythonPath   = string.IsNullOrWhiteSpace(PythonPath) ? null : PythonPath;
        opts.TemplateSets = TemplateSets.Select(vm => vm.Model).ToList();
        _optionsManager.Save(opts);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    // ── Template Set CRUD ─────────────────────────────────────────────────────

    [RelayCommand]
    private void AddTemplateSet()
    {
        var cfg = new TemplateSetConfig { Name = "NewTemplateSet" };
        var row = new TemplateSetRowViewModel(cfg);
        TemplateSets.Add(row);
        SelectedTemplateSet = row;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTemplateSet))]
    private void RemoveTemplateSet()
    {
        if (SelectedTemplateSet is null) return;
        TemplateSets.Remove(SelectedTemplateSet);
        SelectedTemplateSet = TemplateSets.Count > 0 ? TemplateSets[0] : null;
    }

    private bool HasSelectedTemplateSet => SelectedTemplateSet is not null;

    // ── Template CRUD ─────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(HasSelectedTemplateSet))]
    private void AddTemplate()
    {
        if (SelectedTemplateSet is null) return;
        var cfg = new TemplateConfig { Name = "NewTemplate" };
        SelectedTemplateSet.Model.Templates.Add(cfg);
        var row = new TemplateRowViewModel(cfg) { RequestBrowseFile = RequestBrowseTemplateFile };
        Templates.Add(row);
        SelectedTemplate = row;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTemplate))]
    private void RemoveTemplate()
    {
        if (SelectedTemplateSet is null || SelectedTemplate is null) return;
        SelectedTemplateSet.Model.Templates.Remove(SelectedTemplate.Model);
        Templates.Remove(SelectedTemplate);
        SelectedTemplate = Templates.Count > 0 ? Templates[^1] : null;
    }

    private bool HasSelectedTemplate => SelectedTemplate is not null;

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RebuildTemplates(TemplateSetRowViewModel? ts)
    {
        Templates.Clear();
        SelectedTemplate = null;
        OnPropertyChanged(nameof(TemplateSectionTitle));
        if (ts is null) return;
        foreach (var cfg in ts.Model.Templates)
            Templates.Add(new TemplateRowViewModel(cfg) { RequestBrowseFile = RequestBrowseTemplateFile });
    }
}
