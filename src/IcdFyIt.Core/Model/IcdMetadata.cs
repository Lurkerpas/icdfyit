using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Document-level ICD metadata.
/// </summary>
public class IcdMetadata
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Version { get; set; } = string.Empty;

    [XmlAttribute]
    public string Date { get; set; } = string.Empty;

    [XmlAttribute]
    public string Status { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [XmlArray("Fields")]
    [XmlArrayItem("Field")]
    public List<MetadataField> Fields { get; set; } = new();
}