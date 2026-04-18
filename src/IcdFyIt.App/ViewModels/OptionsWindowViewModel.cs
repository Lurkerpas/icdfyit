using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Options window (ICD-DES §5.5).
/// Works on a cloned copy of AppOptions; cancelled changes are discarded (ICD-FUN-100).
/// </summary>
public partial class OptionsWindowViewModel : ObservableObject
{
    private readonly OptionsManager _optionsManager;

    public OptionsWindowViewModel(OptionsManager optionsManager)
    {
        _optionsManager = optionsManager;
    }

    [ObservableProperty]
    private int _undoDepth;

    [ObservableProperty]
    private string _pythonPath = string.Empty;

    /// <summary>Loads the current options into the editable fields.</summary>
    public void LoadFromOptions(AppOptions options) => throw new NotImplementedException();

    [RelayCommand]
    private void Save() => throw new NotImplementedException();

    [RelayCommand]
    private void Cancel() => throw new NotImplementedException();

    [RelayCommand]
    private void AddTemplateSet() => throw new NotImplementedException();

    [RelayCommand]
    private void RemoveTemplateSet() => throw new NotImplementedException();
}
