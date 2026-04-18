using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Simple result record returned by <see cref="Views.AddDataTypeDialog"/>.
/// </summary>
public sealed record DataTypeCreationArgs(string Name, BaseType Kind);

/// <summary>
/// ViewModel for the Add Data Type dialog.
/// </summary>
public partial class AddDataTypeViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _name = "NewType";

    [ObservableProperty]
    private BaseType _selectedKind = BaseType.SignedInteger;

    public IReadOnlyList<BaseType> AllKinds { get; } = Enum.GetValues<BaseType>();

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}
