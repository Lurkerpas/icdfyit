using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Structure Fields pop-up dialog.
/// Manages Add/Remove operations on a single <see cref="StructureType"/>.
/// </summary>
public partial class StructureFieldsDialogViewModel : ObservableObject
{
    private readonly StructureType          _type;
    private readonly IReadOnlyList<DataType> _availableTypes;

    public StructureFieldsDialogViewModel(StructureType type, IReadOnlyList<DataType> availableTypes)
    {
        _type           = type;
        _availableTypes = availableTypes;
        Fields = new ObservableCollection<StructureFieldRowViewModel>(
            type.Fields.Select(f => new StructureFieldRowViewModel(f, availableTypes)));
    }

    public string TypeName => _type.Name;

    public ObservableCollection<StructureFieldRowViewModel> Fields { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRemove))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private StructureFieldRowViewModel? _selectedField;

    public bool CanRemove => SelectedField is not null;

    [RelayCommand]
    private void Add()
    {
        var f   = new StructureField { Name = "NewField" };
        _type.Fields.Add(f);
        var row = new StructureFieldRowViewModel(f, _availableTypes);
        Fields.Add(row);
        SelectedField = row;
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedField is null) return;
        _type.Fields.Remove(SelectedField.Model);
        Fields.Remove(SelectedField);
        SelectedField = null;
    }
}
