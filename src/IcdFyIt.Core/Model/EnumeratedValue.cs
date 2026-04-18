using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary> One entry in an Enumerated Data Type: a symbolic name mapped to a set of integer raw values. </summary>
public class EnumeratedValue
{
    public string Name { get; set; } = string.Empty;
    public List<int> RawValues { get; set; } = [];

    /// <summary>
    /// UI-facing comma-separated representation of <see cref="RawValues"/>
    /// (e.g. "1, 2, 3"). Not persisted.
    /// </summary>
    [XmlIgnore]
    public string RawValuesText
    {
        get => string.Join(", ", RawValues);
        set
        {
            RawValues.Clear();
            foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var v))
                    RawValues.Add(v);
            }
        }
    }
}
