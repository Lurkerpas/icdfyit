using CommunityToolkit.Mvvm.ComponentModel;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Add Parameter dialog.
/// </summary>
public partial class AddParameterViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _name = "NewParameter";

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}
