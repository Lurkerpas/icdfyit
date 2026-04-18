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

    public List<PacketField> Fields { get; set; } = new();
}
