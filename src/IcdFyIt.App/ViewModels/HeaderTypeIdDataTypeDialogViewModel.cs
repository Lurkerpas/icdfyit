using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the "Set Data Type" dialog for a Header Type ID entry.
/// Uses an AutoCompleteBox (popup-safe in a standalone Window).
/// </summary>
public partial class HeaderTypeIdDataTypeDialogViewModel : ObservableObject
{
    private readonly HeaderTypeId _entry;

    public HeaderTypeIdDataTypeDialogViewModel(HeaderTypeId entry, IReadOnlyList<DataType> availableTypes)
    {
        _entry         = entry;
        AvailableTypes = availableTypes;
    }

    public string EntryName => _entry.Name;

    public IReadOnlyList<DataType> AvailableTypes { get; }

    public DataType? SelectedDataType
    {
        get => _entry.DataType;
        set
        {
            if (value is null) return;
            _entry.DataType = value;
            OnPropertyChanged();
        }
    }
}
