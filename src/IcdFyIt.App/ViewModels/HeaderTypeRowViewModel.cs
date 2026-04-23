using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Row wrapper around a <see cref="HeaderType"/> for the Header Types grid (ICD-IF-220).
/// </summary>
public partial class HeaderTypeRowViewModel : ObservableObject
{
    public HeaderType Model { get; }

    public Action? OnEdited { get; set; }

    public HeaderTypeRowViewModel(HeaderType model) => Model = model;

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public string Description
    {
        get => Model.Description;
        set { Model.Description = value ?? string.Empty; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public int IdCount => Model.Ids.Count;

    public void NotifyIdCountChanged() => OnPropertyChanged(nameof(IdCount));
}
