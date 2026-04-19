using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Row wrapper around a <see cref="HeaderTypeId"/> for the ID entries grid (ICD-DAT-730).
/// </summary>
public partial class HeaderTypeIdRowViewModel : ObservableObject
{
    public HeaderTypeId Model { get; }

    public Action? OnEdited { get; set; }

    public HeaderTypeIdRowViewModel(HeaderTypeId model) => Model = model;

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public string? Description
    {
        get => Model.Description;
        set { Model.Description = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    /// <summary>Display name of the assigned data type, or "—" when none is assigned.</summary>
    public string DataTypeName => Model.DataType?.Name ?? "\u2014";

    public void RefreshDataType() => OnPropertyChanged(nameof(DataTypeName));
}
