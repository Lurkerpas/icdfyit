using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Data Types window (ICD-DES §5.2).
/// </summary>
public partial class DataTypesWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier _changeNotifier;

    public DataTypesWindowViewModel(DataModelManager dataModelManager, ChangeNotifier changeNotifier)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier = changeNotifier;
    }

    /// <summary>Live collection of data types; bound to the list/DataGrid in the view.</summary>
    public ObservableCollection<DataType> DataTypes => _changeNotifier.DataTypes;

    [ObservableProperty]
    private DataType? _selected;

    [RelayCommand]
    private void Add() => throw new NotImplementedException();

    [RelayCommand]
    private void Remove() => throw new NotImplementedException();

    [RelayCommand]
    private void Duplicate() => throw new NotImplementedException();
}
