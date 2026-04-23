using CommunityToolkit.Mvvm.ComponentModel;

namespace IcdFyIt.App.ViewModels;

/// <summary>ViewModel for the Add Memory dialog.</summary>
public partial class AddMemoryViewModel : ObservableObject
{
    public AddMemoryViewModel(string defaultName = "NewMemory")
    {
        _name = defaultName;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _name;

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}
