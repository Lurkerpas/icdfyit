namespace IcdFyIt.Core.Model;

/// <summary> Range, optional unit, and optional calibration formula for numeric Data Types (ICD-DAT-102). </summary>
public class NumericProperties
{
    public NumericRange Range { get; set; } = new();
    public string? Unit { get; set; }
    public string? CalibrationFormula { get; set; }
}
