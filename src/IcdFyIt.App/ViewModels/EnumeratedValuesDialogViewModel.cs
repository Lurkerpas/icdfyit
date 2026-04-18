using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Enumerated Values pop-up dialog.
/// Manages Add/Remove operations on a single <see cref="EnumeratedType"/>.
/// </summary>
public partial class EnumeratedValuesDialogViewModel : ObservableObject
{
    private readonly EnumeratedType _type;

    public EnumeratedValuesDialogViewModel(EnumeratedType type)
    {
        _type = type;
        Values = new ObservableCollection<EnumeratedValue>(type.Values);
    }

    public string TypeName => _type.Name;

    public ObservableCollection<EnumeratedValue> Values { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRemove))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private EnumeratedValue? _selectedValue;

    public bool CanRemove => SelectedValue is not null;

    [RelayCommand]
    private void Add()
    {
        var ev = new EnumeratedValue { Name = "NewValue" };
        _type.Values.Add(ev);
        Values.Add(ev);
        SelectedValue = ev;
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedValue is null) return;
        _type.Values.Remove(SelectedValue);
        Values.Remove(SelectedValue);
        SelectedValue = null;
    }
}
