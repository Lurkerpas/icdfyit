using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

public partial class StructureFieldRowViewModel : ObservableObject
{
    public StructureField Model { get; }

    /// <summary>All available data types for the AutoCompleteBox drop-down.</summary>
    public IReadOnlyList<DataType> AvailableTypes { get; }

    /// <summary>Per-row add command that delegates back to the parent dialog VM.</summary>
    public ICommand AddCommand { get; }

    /// <summary>Per-row remove command that delegates back to the parent dialog VM.</summary>
    public ICommand RemoveCommand { get; }

    public StructureFieldRowViewModel(
        StructureField field,
        IReadOnlyList<DataType> availableTypes,
        Action onAdd,
        Action<StructureFieldRowViewModel> onRemove)
    {
        Model          = field;
        AvailableTypes = availableTypes;
        AddCommand     = new RelayCommand(onAdd);
        RemoveCommand  = new RelayCommand(() => onRemove(this));
    }

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); }
    }

    public DataType? DataType
    {
        get => Model.DataType;
        set
        {
            // Ignore null clears that happen while the user is typing.
            if (value is null) return;
            Model.DataType = value;
            OnPropertyChanged();
        }
    }
}
