using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// Coordinates DataModelManager actions and exposes top-level commands (ICD-DES §5.1).
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

    /// <summary>Displayed in the title bar; includes unsaved indicator when dirty.</summary>
    [ObservableProperty]
    private string _title = "icdfyit";

    [RelayCommand]
    private void NewDocument() => throw new NotImplementedException();

    [RelayCommand]
    private void OpenDocument() => throw new NotImplementedException();

    [RelayCommand]
    private void SaveDocument() => throw new NotImplementedException();

    [RelayCommand]
    private void SaveDocumentAs() => throw new NotImplementedException();

    [RelayCommand]
    private void Undo() => throw new NotImplementedException();

    [RelayCommand]
    private void Redo() => throw new NotImplementedException();

    [RelayCommand]
    private void OpenDataTypes() => throw new NotImplementedException();

    [RelayCommand]
    private void OpenParameters() => throw new NotImplementedException();

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
}
