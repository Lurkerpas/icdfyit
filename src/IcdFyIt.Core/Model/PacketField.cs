using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A single field within a PacketType (ICD-DAT-170 to ICD-DAT-172).
/// Parameter reference may be null if the referenced parameter was deleted (ICD-FUN-40).
/// </summary>
public class PacketField
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Nullable: reference to a Parameter; may be null if deleted.</summary>
    public Parameter? Parameter { get; set; }

    /// <summary>Whether this field acts as a type indicator within a packet.</summary>
    [XmlAttribute]
    public bool IsTypeIndicator { get; set; }

    /// <summary>Value compared against this field to identify the enclosing packet type.</summary>
    [XmlAttribute]
    public string? IndicatorValue { get; set; }
}
