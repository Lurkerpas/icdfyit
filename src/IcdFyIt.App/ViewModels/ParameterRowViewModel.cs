using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Flat row wrapper around a <see cref="Parameter"/> for spreadsheet-style DataGrid display
/// (ICD-IF-92). Inline-editable properties write through to the model. Kind and DataType are
/// read-only here and are modified via the <see cref="Views.ParameterAttributesDialog"/> popup.
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

    public int NumericId
    {
        get => Model.NumericId;
        set { Model.NumericId = value; OnPropertyChanged(); }
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

    // ── Read-only display columns (changed via ParameterAttributesDialog) ─────

    public string Kind => Model.Kind.ToString();

    /// <summary>Display name of the assigned data type, or "—" when none is assigned.</summary>
    public string DataTypeName => Model.DataType?.Name ?? "-";

    // ── Applicability helpers for conditional opacity / edit-guard ─────────────

    public bool IsFormulaApplicable  => Model.Kind == ParameterKind.SyntheticValue;
    public bool IsHexValueApplicable => Model.Kind == ParameterKind.FixedValue;

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
    }
}
