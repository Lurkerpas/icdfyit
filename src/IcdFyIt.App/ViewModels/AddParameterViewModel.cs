using CommunityToolkit.Mvvm.ComponentModel;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Add Parameter dialog.
/// </summary>
public partial class AddParameterViewModel : ObservableObject
{
    public AddParameterViewModel(string defaultName = "NewParameter")
    {
        _name = defaultName;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _name;

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}
