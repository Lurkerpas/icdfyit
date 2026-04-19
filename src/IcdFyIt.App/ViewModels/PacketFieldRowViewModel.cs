using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Row wrapper for a <see cref="PacketField"/> used inside the Main Window detail panel.
/// Follows the same ItemsControl pattern as <see cref="StructureFieldRowViewModel"/> to
/// avoid the Avalonia/X11 DataGrid popup crash.
/// </summary>
public partial class PacketFieldRowViewModel : ObservableObject
{
    public PacketField Model { get; }

    public IReadOnlyList<Parameter> AvailableParameters { get; }

    public ICommand AddCommand    { get; }
    public ICommand RemoveCommand { get; }

    public PacketFieldRowViewModel(
        PacketField field,
        IReadOnlyList<Parameter> availableParameters,
        Action onAdd,
        Action<PacketFieldRowViewModel> onRemove)
    {
        Model               = field;
        AvailableParameters = availableParameters;
        AddCommand          = new RelayCommand(onAdd);
        RemoveCommand       = new RelayCommand(() => onRemove(this));
    }

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); }
    }

    public string? Description
    {
        get => Model.Description;
        set { Model.Description = value; OnPropertyChanged(); }
    }

    public Parameter? Parameter
    {
        get => Model.Parameter;
        set
        {
            if (value is null) return;
            Model.Parameter = value;
            OnPropertyChanged();
        }
    }

    public bool IsTypeIndicator
    {
        get => Model.IsTypeIndicator;
        set
        {
            Model.IsTypeIndicator = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowIndicatorValue));
        }
    }

    public string? IndicatorValue
    {
        get => Model.IndicatorValue;
        set { Model.IndicatorValue = value; OnPropertyChanged(); }
    }

    /// <summary>Controls visibility of the IndicatorValue TextBox.</summary>
    public bool ShowIndicatorValue => Model.IsTypeIndicator;
}
