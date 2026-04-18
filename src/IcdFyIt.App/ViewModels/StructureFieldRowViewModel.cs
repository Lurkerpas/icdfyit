using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

public partial class StructureFieldRowViewModel : ObservableObject
{
    public StructureField Model { get; }

    /// <summary>All available data types for the AutoCompleteBox drop-down.</summary>
    public IReadOnlyList<DataType> AvailableTypes { get; }

    public StructureFieldRowViewModel(StructureField field, IReadOnlyList<DataType> availableTypes)
    {
        Model          = field;
        AvailableTypes = availableTypes;
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
            // Only propagate when a real type is chosen; ignore null clears that
            // happen while the user is typing in the AutoCompleteBox text box.
            if (value is null) return;
            Model.DataType = value;
            OnPropertyChanged();
        }
    }
}
