using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Flat row wrapper around a <see cref="Parameter"/> for spreadsheet-style DataGrid display
/// (ICD-IF-92). Inline-editable properties write through to the model. Kind, DataType, Memory,
/// and ValidityParameter are modified via the <see cref="Views.ParameterAttributesDialog"/> popup.
/// </summary>
public partial class ParameterRowViewModel : ObservableObject
{
    public Parameter Model { get; }

    /// <summary>Invoked after any inline property change so the window can mark the model dirty.</summary>
    public Action? OnEdited { get; set; }

    public ParameterRowViewModel(Parameter model) => Model = model;

    // ── Inline-editable columns ───────────────────────────────────────────────

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); }
    }

    public string? Mnemonic
    {
        get => Model.Mnemonic;
        set { Model.Mnemonic = value; OnPropertyChanged(); }
    }

    public string NumericId
    {
        get => Model.NumericIdStr;
        set { Model.NumericIdStr = value; OnPropertyChanged(); }
    }

    public string? ShortDescription
    {
        get => Model.ShortDescription;
        set { Model.ShortDescription = value; OnPropertyChanged(); }
    }

    public string? LongDescription
    {
        get => Model.LongDescription;
        set { Model.LongDescription = value; OnPropertyChanged(); }
    }

    /// <summary>Formula string; only meaningful when Kind == SyntheticValue.</summary>
    public string? Formula
    {
        get => Model.Formula;
        set { Model.Formula = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormulaDisplay)); }
    }

    /// <summary>Hex value string; only meaningful when Kind == FixedValue.</summary>
    public string? HexValue
    {
        get => Model.HexValue;
        set { Model.HexValue = value; OnPropertyChanged(); }
    }

    public string MemoryOffset
    {
        get => Model.MemoryOffsetStr;
        set { Model.MemoryOffsetStr = value; OnPropertyChanged(); }
    }

    public string? AlarmLow
    {
        get => Model.AlarmLow?.ToString();
        set { Model.AlarmLow = double.TryParse(value, out var v) ? v : null; OnPropertyChanged(); }
    }

    public string? AlarmHigh
    {
        get => Model.AlarmHigh?.ToString();
        set { Model.AlarmHigh = double.TryParse(value, out var v) ? v : null; OnPropertyChanged(); }
    }

    // ── Read-only display columns (changed via ParameterAttributesDialog) ─────

    public string Kind => Model.Kind.ToString();

    /// <summary>Display name of the assigned data type, or "—" when none is assigned.</summary>
    public string DataTypeName => Model.DataType?.Name ?? "-";

    /// <summary>Display name of the associated memory, or empty when none.</summary>
    public string MemoryName => Model.Memory?.Name ?? string.Empty;

    /// <summary>Display name of the validity parameter, or empty when none.</summary>
    public string ValidityParameterName => Model.ValidityParameter?.Name ?? string.Empty;

    // ── Applicability helpers for conditional opacity / edit-guard ─────────────

    public bool IsFormulaApplicable  => Model.Kind == ParameterKind.SyntheticValue;
    public bool IsHexValueApplicable => Model.Kind == ParameterKind.FixedValue;
    public bool IsNumericApplicable  => Model.DataType?.Kind is
        BaseType.SignedInteger or BaseType.UnsignedInteger or BaseType.Float;
    public bool HasMemory => Model.Memory is not null;

    /// <summary>Returns the formula for display, or null (shown as —) when kind is not SyntheticValue.</summary>
    public string? FormulaDisplay => IsFormulaApplicable ? Formula : null;

    // ── Post-dialog refresh ────────────────────────────────────────────────────

    /// <summary>Forces property-change notifications for all values that may have been
    /// changed by the <see cref="Views.ParameterAttributesDialog"/>.</summary>
    public void RefreshAfterAttributesEdit()
    {
        OnPropertyChanged(nameof(Kind));
        OnPropertyChanged(nameof(DataTypeName));
        OnPropertyChanged(nameof(IsFormulaApplicable));
        OnPropertyChanged(nameof(IsHexValueApplicable));
        OnPropertyChanged(nameof(FormulaDisplay));
        OnPropertyChanged(nameof(IsNumericApplicable));
        OnPropertyChanged(nameof(HasMemory));
        OnPropertyChanged(nameof(MemoryName));
        OnPropertyChanged(nameof(ValidityParameterName));
        OnPropertyChanged(nameof(AlarmLow));
        OnPropertyChanged(nameof(AlarmHigh));
        OnPropertyChanged(nameof(MemoryOffset));
    }
}

