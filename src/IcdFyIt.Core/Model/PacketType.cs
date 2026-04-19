using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A Packet Type entity (ICD-DAT-170 to ICD-DAT-174).
/// </summary>
public class PacketType
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [XmlAttribute]
    public PacketTypeKind Kind { get; set; }

    /// <summary>Nullable: reference to a Header Type; may be null if deleted (ICD-DAT-413).</summary>
    [XmlIgnore]
    public HeaderType? HeaderType { get; set; }

    /// <summary>GUID reference for XML serialization; resolved post-load by XmlPersistence.</summary>
    [XmlAttribute("HeaderTypeRef")]
    public string? HeaderTypeIdRef
    {
        get => HeaderType?.Id.ToString();
        set => _storedHeaderTypeIdRef = value;
    }
    internal string? _storedHeaderTypeIdRef;

    public List<PacketField> Fields { get; set; } = new();

    /// <summary>Fixed hex values for each Header Type ID (ICD-DAT-414).</summary>
    [XmlArray("HeaderIdValues")]
    [XmlArrayItem("Entry")]
    public List<HeaderIdValue> HeaderIdValues { get; set; } = new();
}
