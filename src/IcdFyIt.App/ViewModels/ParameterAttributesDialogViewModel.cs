using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Parameter Attributes editor pop-up.
/// Edits Kind (ComboBox) and DataType (AutoCompleteBox) — both popup controls that are
/// safe here because this is a standalone Window, not a DataGrid cell template.
/// </summary>
public partial class ParameterAttributesDialogViewModel : ObservableObject
{
    private readonly Parameter _parameter;

    public ParameterAttributesDialogViewModel(Parameter parameter, IReadOnlyList<DataType> availableTypes)
    {
        _parameter     = parameter;
        AvailableTypes = availableTypes;
        _selectedKind  = parameter.Kind;
    }

    public string ParameterName => _parameter.Name;

    // ── Kind ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<ParameterKind> AllKinds { get; } = Enum.GetValues<ParameterKind>();

    [ObservableProperty]
    private ParameterKind _selectedKind;

    partial void OnSelectedKindChanged(ParameterKind value) => _parameter.Kind = value;

    // ── Data Type (AutoCompleteBox) ───────────────────────────────────────────

    public IReadOnlyList<DataType> AvailableTypes { get; }

    public DataType? DataType
    {
        get => _parameter.DataType;
        set
        {
            if (value is null) return;
            _parameter.DataType = value;
            OnPropertyChanged();
        }
    }
}
