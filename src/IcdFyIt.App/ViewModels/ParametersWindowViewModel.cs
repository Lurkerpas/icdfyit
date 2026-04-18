using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Parameters window (ICD-DES §5.3).
/// </summary>
public partial class ParametersWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier _changeNotifier;

    public ParametersWindowViewModel(DataModelManager dataModelManager, ChangeNotifier changeNotifier)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier = changeNotifier;
    }

    /// <summary>Live collection of parameters; bound to the list/DataGrid in the view.</summary>
    public ObservableCollection<Parameter> Parameters => _changeNotifier.Parameters;

    [ObservableProperty]
    private Parameter? _selected;

    [RelayCommand]
    private void Add() => throw new NotImplementedException();

    [RelayCommand]
    private void Remove() => throw new NotImplementedException();

    [RelayCommand]
    private void Duplicate() => throw new NotImplementedException();
}
