using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Array Type editor pop-up.
/// Edits element type, size-field endianness/bit-size and min/max array length.
/// </summary>
public partial class ArrayTypeDialogViewModel : ObservableObject
{
    private readonly ArrayType _type;

    public ArrayTypeDialogViewModel(ArrayType type, IReadOnlyList<DataType> availableTypes)
    {
        _type          = type;
        AvailableTypes = availableTypes;

        // Ensure ArraySize is initialised so bindings never hit null.
        _type.ArraySize ??= new ArraySizeDescriptor();
    }

    public string TypeName => _type.Name;

    public IReadOnlyList<DataType> AvailableTypes { get; }

    // ── Element type (AutoCompleteBox) ───────────────────────────────────────

    public DataType? ElementType
    {
        get => _type.ElementType;
        set
        {
            if (value is null) return;
            _type.ElementType = value;
            OnPropertyChanged();
        }
    }

    // ── Array size descriptor ────────────────────────────────────────────────

    public string SizeBitSize
    {
        get => _type.ArraySize!.BitSizeStr;
        set { _type.ArraySize!.BitSizeStr = value; OnPropertyChanged(); }
    }

    public double SizeMin
    {
        get => _type.ArraySize!.Range.Min;
        set { _type.ArraySize!.Range.Min = value; OnPropertyChanged(); }
    }

    public double SizeMax
    {
        get => _type.ArraySize!.Range.Max;
        set { _type.ArraySize!.Range.Max = value; OnPropertyChanged(); }
    }

    // ── Endianness of the size field (RadioButton, no popup) ─────────────────

    public bool SizeIsLittleEndian
    {
        get => _type.ArraySize!.Endianness == Endianness.LittleEndian;
        set
        {
            if (!value) return;
            _type.ArraySize!.Endianness = Endianness.LittleEndian;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SizeIsBigEndian));
        }
    }

    public bool SizeIsBigEndian
    {
        get => _type.ArraySize!.Endianness == Endianness.BigEndian;
        set
        {
            if (!value) return;
            _type.ArraySize!.Endianness = Endianness.BigEndian;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SizeIsLittleEndian));
        }
    }
}
