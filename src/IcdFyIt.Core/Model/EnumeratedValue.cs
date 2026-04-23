using System.Xml.Serialization;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.Core.Model;

/// <summary> One entry in an Enumerated Data Type: a symbolic name mapped to a set of integer raw values. </summary>
public class EnumeratedValue
{
    public string Name { get; set; } = string.Empty;
    public List<int> RawValues { get; set; } = [];

    /// <summary>
    /// Stores the raw text entered by the user for <see cref="RawValues"/> so that hex notation
    /// ("0xFF, 0x01") is preserved through save/reload (ICD-DAT-800). Null means use decimal display.
    /// </summary>
    [XmlAttribute]
    public string? RawValuesDisplay { get; set; }

    /// <summary>
    /// UI-facing comma-separated representation of <see cref="RawValues"/>
    /// (e.g. "0xFF, 0x01" or "1, 2, 3"). Not persisted directly — <see cref="RawValuesDisplay"/>
    /// stores the user's format and <see cref="RawValues"/> stores the parsed integers.
    /// </summary>
    [XmlIgnore]
    public string RawValuesText
    {
        get => RawValuesDisplay ?? string.Join(", ", RawValues);
        set
        {
            RawValues.Clear();
            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var display = new System.Text.StringBuilder();
            bool first = true;
            foreach (var part in parts)
            {
                if (HexInt.TryParse(part, out var v))
                {
                    RawValues.Add(v);
                    if (!first) display.Append(", ");
                    display.Append(part);
                    first = false;
                }
            }
            RawValuesDisplay = display.Length > 0 ? display.ToString() : null;
        }
    }
}
