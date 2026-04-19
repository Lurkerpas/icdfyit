using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the main application window (ICD-DES §5.1).
/// File-dialog interactions are injected as delegates from the composition root so the
/// ViewModel remains View-free (testable).
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly DirtyTracker _dirtyTracker;

    public MainWindowViewModel(DataModelManager dataModelManager, DirtyTracker dirtyTracker)
    {
        _dataModelManager = dataModelManager;
        _dirtyTracker = dirtyTracker;
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

    // ── Title bar ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _title = "icdfyit";

    private void RefreshTitle()
    {
        var file = _dataModelManager.CurrentFilePath is { } p
            ? System.IO.Path.GetFileName(p)
            : "Untitled";
        Title = _dirtyTracker.IsDirty
            ? $"{file}* — icdfyit"
            : $"{file} — icdfyit";
    }

    // ── File commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void NewDocument()
    {
        _dataModelManager.New();
        RefreshTitle();
    }

    [RelayCommand]
    private async Task OpenDocument()
    {
        var path = await (OpenFileDialog?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;
        _dataModelManager.Open(path);
        RefreshTitle();
    }

    [RelayCommand]
    private async Task SaveDocument()
    {
        var path = _dataModelManager.CurrentFilePath
            ?? await (SaveFileDialog?.Invoke(null) ?? Task.FromResult<string?>(null));
        if (path is null) return;
        _dataModelManager.Save(path);
        RefreshTitle();
    }

    [RelayCommand]
    private async Task SaveDocumentAs()
    {
        var path = await (SaveFileDialog?.Invoke(_dataModelManager.CurrentFilePath)
            ?? Task.FromResult<string?>(null));
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

    // ── Window navigation ──────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenDataTypes() => ShowDataTypesWindow?.Invoke();

    [RelayCommand]
    private void OpenParameters() => ShowParametersWindow?.Invoke();

    [RelayCommand]
    private void OpenExportWindow() => throw new NotImplementedException();

    [RelayCommand]
    private void OpenOptions() => throw new NotImplementedException();

    [RelayCommand]
    private void OpenAbout() => throw new NotImplementedException();

    [RelayCommand]
    private void OpenHelp() => throw new NotImplementedException();

    [RelayCommand]
    private void RunValidation() => throw new NotImplementedException();

    // ── Dirty notification (called from DataTypesWindow after inline edits) ───

    public void NotifyModelEdited() => RefreshTitle();
}
