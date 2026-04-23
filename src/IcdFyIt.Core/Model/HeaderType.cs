using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A Header Type entity describing the common header shared by Packet Types (ICD-DAT-710 to ICD-DAT-730).
/// </summary>
public class HeaderType
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Ordered list of ID entries defined by this Header Type (ICD-DAT-730).</summary>
    public List<HeaderTypeId> Ids { get; set; } = new();

    public override string ToString() => Name;
}
