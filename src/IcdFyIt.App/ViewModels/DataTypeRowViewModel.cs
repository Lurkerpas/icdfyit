using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Flat row wrapper around a <see cref="DataType"/> for spreadsheet-style DataGrid display
/// (ICD-IF-120). Properties that don't apply to the underlying type return null; their
/// setters are no-ops, so editing non-applicable cells has no effect.
/// </summary>
public partial class DataTypeRowViewModel : ObservableObject
{
    public sealed record EndiannessOption(Endianness Value, string Label);

    public DataType Model { get; }

    public DataTypeRowViewModel(DataType model) => Model = model;

    // ── Columns that apply to all types ──────────────────────────────────────

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); }
    }

    public string Kind => Model.Kind.ToString();

    // ── Scalar columns (SignedInteger, UnsignedInteger, Float, Boolean, BitString) ──

    public IReadOnlyList<EndiannessOption> AllEndiannesses { get; } =
    [
        new(IcdFyIt.Core.Model.Endianness.LittleEndian, "Little endian"),
        new(IcdFyIt.Core.Model.Endianness.BigEndian, "Big endian")
    ];

    /// <summary>Invoked after any property that modifies the underlying model is changed directly
    /// (i.e. outside DataGrid edit-mode), so the window can mark the model dirty.</summary>
    public Action? OnEdited { get; set; }

    /// <summary>Two-way binding target for the Little-endian RadioButton.</summary>
    public bool IsLittleEndian
    {
        get => Endianness == IcdFyIt.Core.Model.Endianness.LittleEndian;
        set
        {
            if (!value || !IsScalarApplicable) return;
            Endianness = IcdFyIt.Core.Model.Endianness.LittleEndian;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBigEndian));
            OnEdited?.Invoke();
        }
    }

    /// <summary>Two-way binding target for the Big-endian RadioButton.</summary>
    public bool IsBigEndian
    {
        get => Endianness == IcdFyIt.Core.Model.Endianness.BigEndian;
        set
        {
            if (!value || !IsScalarApplicable) return;
            Endianness = IcdFyIt.Core.Model.Endianness.BigEndian;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLittleEndian));
            OnEdited?.Invoke();
        }
    }

    public Endianness? Endianness
    {
        get => Model switch
        {
            EnumeratedType et => et.Endianness,
            _                 => GetScalar()?.Endianness
        };
        set
        {
            if (!value.HasValue) return;

            if (Model is EnumeratedType et)
            {
                et.Endianness = value.Value;
                OnPropertyChanged();
                return;
            }

            var s = GetScalar();
            if (s is null) return;
            s.Endianness = value.Value;
            OnPropertyChanged();
        }
    }

    public string? BitSize
    {
        get => Model is EnumeratedType et ? et.BitSizeStr : GetScalar()?.BitSizeStr;
        set
        {
            if (value is null) return;
            if (Model is EnumeratedType et) { et.BitSizeStr = value; OnPropertyChanged(); return; }
            var s = GetScalar();
            if (s is null) return;
            s.BitSizeStr = value;
            OnPropertyChanged();
        }
    }

    // ── Numeric columns (SignedInteger, UnsignedInteger, Float) ──────────────

    public double? RangeMin
    {
        get => GetNumeric()?.Range.Min;
        set
        {
            var n = GetNumeric();
            if (n is null || !value.HasValue) return;
            n.Range.Min = value.Value;
            OnPropertyChanged();
        }
    }

    public double? RangeMax
    {
        get => GetNumeric()?.Range.Max;
        set
        {
            var n = GetNumeric();
            if (n is null || !value.HasValue) return;
            n.Range.Max = value.Value;
            OnPropertyChanged();
        }
    }

    public string? Unit
    {
        get => GetNumeric()?.Unit;
        set
        {
            var n = GetNumeric();
            if (n is null) return;
            n.Unit = value;
            OnPropertyChanged();
        }
    }

    public string? CalibrationFormula
    {
        get => GetNumeric()?.CalibrationFormula;
        set
        {
            var n = GetNumeric();
            if (n is null) return;
            n.CalibrationFormula = value;
            OnPropertyChanged();
        }
    }

    // ── Summary column (complex types) ───────────────────────────────────────

    /// <summary>Brief summary shown in the "Details" column for complex types.</summary>
    public string? Summary => Model switch
    {
        EnumeratedType et => et.Values.Count == 0
                                 ? "(no values)"
                                 : string.Join(", ", et.Values.Select(v => v.Name)),
        StructureType  st => $"{st.Fields.Count} field(s)",
        ArrayType      at => $"[{at.ElementType?.Name ?? "?"}]",
        _                 => null
    };

    /// <summary>Forces refresh of the Summary property (call after enum values are added/removed).</summary>
    public void RefreshSummary() => OnPropertyChanged(nameof(Summary));

    // ── Applicability flags (used to disable N/A cells) ───────────────────────

    /// <summary>True when this type has endianness (all scalar types plus Enumerated).</summary>
    public bool IsScalarApplicable => GetScalar() is not null || Model is EnumeratedType;

    /// <summary>True when this type has a bit size (all scalar types plus Enumerated).</summary>
    public bool IsBitSizeApplicable => GetScalar() is not null || Model is EnumeratedType;

    /// <summary>True when this type has numeric properties (Range, Unit, Calibration).</summary>
    public bool IsNumericApplicable => Model is SignedIntegerType or UnsignedIntegerType or FloatType;

    // ── Private helpers ───────────────────────────────────────────────────────

    private ScalarProperties? GetScalar() => Model switch
    {
        SignedIntegerType   si  => si.Scalar,
        UnsignedIntegerType ui  => ui.Scalar,
        FloatType           ft  => ft.Scalar,
        BooleanType         bt  => bt.Scalar,
        BitStringType       bst => bst.Scalar,
        _                       => null
    };

    private NumericProperties? GetNumeric() => Model switch
    {
        SignedIntegerType   si  => si.Numeric ??= new(),
        UnsignedIntegerType ui  => ui.Numeric ??= new(),
        FloatType           ft  => ft.Numeric ??= new(),
        _                       => null
    };
}
