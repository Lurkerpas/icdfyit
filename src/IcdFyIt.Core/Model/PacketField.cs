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

    [XmlIgnore]
    public Parameter? Parameter { get; set; }

    /// <summary>GUID reference for XML serialization; resolved post-load.</summary>
    [XmlAttribute("ParameterRef")]
    public string? ParameterIdRef
    {
        get => Parameter?.Id.ToString();
        set => _storedParameterIdRef = value;
    }
    internal string? _storedParameterIdRef;

    [XmlAttribute]
    public bool IsTypeIndicator { get; set; }

    [XmlAttribute]
    public string? IndicatorValue { get; set; }
}
