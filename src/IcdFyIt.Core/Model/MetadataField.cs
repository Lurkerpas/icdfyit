using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// User-defined ICD metadata field represented as a name:value pair.
/// </summary>
public class MetadataField
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Value { get; set; } = string.Empty;

    public override string ToString() => Name;
}