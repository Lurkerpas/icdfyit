using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Parameter Attributes editor pop-up.
/// Edits Kind, DataType, Memory, ValidityParameter (AutoCompleteBox / ComboBox controls that
/// are safe only in a standalone Window, not a DataGrid cell template).
/// Also exposes inline-editable alarm thresholds and memory offset.
/// </summary>
public partial class ParameterAttributesDialogViewModel : ObservableObject
{
    private readonly Parameter _parameter;

    public ParameterAttributesDialogViewModel(
        Parameter parameter,
        IReadOnlyList<DataType> availableTypes,
        IReadOnlyList<Memory> availableMemories,
        IReadOnlyList<Parameter> availableParameters)
    {
        _parameter        = parameter;
        AvailableTypes    = availableTypes;
        AvailableMemories = availableMemories;
        // Exclude self from the validity parameter list.
        AvailableParameters = availableParameters.Where(p => p != parameter).ToList();

        _selectedKind     = parameter.Kind;
        _memoryOffset     = parameter.MemoryOffsetStr;
        _alarmLow         = parameter.AlarmLow?.ToString() ?? string.Empty;
        _alarmHigh        = parameter.AlarmHigh?.ToString() ?? string.Empty;
    }

    public string ParameterName => _parameter.Name;

    // ── Kind ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<ParameterKind> AllKinds { get; } = Enum.GetValues<ParameterKind>();

    [ObservableProperty]
    private ParameterKind _selectedKind;

    partial void OnSelectedKindChanged(ParameterKind value) => _parameter.Kind = value;

    // ── Data Type ─────────────────────────────────────────────────────────────

    public IReadOnlyList<DataType> AvailableTypes { get; }

    public DataType? DataType
    {
        get => _parameter.DataType;
        set
        {
            if (value is null) return;
            _parameter.DataType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNumericApplicable));
        }
    }

    /// <summary>True when the current DataType is numeric (SignedInteger, UnsignedInteger, Float).</summary>
    public bool IsNumericApplicable => _parameter.DataType?.Kind is
        BaseType.SignedInteger or BaseType.UnsignedInteger or BaseType.Float;

    // ── Memory reference ──────────────────────────────────────────────────────

    public IReadOnlyList<Memory> AvailableMemories { get; }

    public Memory? Memory
    {
        get => _parameter.Memory;
        set
        {
            _parameter.Memory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMemory));
        }
    }

    public bool HasMemory => _parameter.Memory is not null;

    [ObservableProperty]
    private string _memoryOffset;

    partial void OnMemoryOffsetChanged(string value) => _parameter.MemoryOffsetStr = value;

    // ── Validity parameter ────────────────────────────────────────────────────

    public IReadOnlyList<Parameter> AvailableParameters { get; }

    public Parameter? ValidityParameter
    {
        get => _parameter.ValidityParameter;
        set
        {
            _parameter.ValidityParameter = value;
            OnPropertyChanged();
        }
    }

    // ── Alarm thresholds ──────────────────────────────────────────────────────

    [ObservableProperty]
    private string _alarmLow;

    partial void OnAlarmLowChanged(string value)
    {
        _parameter.AlarmLow = double.TryParse(value, out var v) ? v : null;
    }

    [ObservableProperty]
    private string _alarmHigh;

    partial void OnAlarmHighChanged(string value)
    {
        _parameter.AlarmHigh = double.TryParse(value, out var v) ? v : null;
    }
}

