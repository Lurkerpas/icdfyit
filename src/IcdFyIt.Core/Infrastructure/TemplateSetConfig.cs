using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// A named export template set stored in settings.xml (ICD-DAT-600, ICD-DAT-601, ICD-DES-81).
/// </summary>
public class TemplateSetConfig
{
    /// <summary>Display name of the template set (ICD-DAT-600).</summary>
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable description (ICD-DAT-601).</summary>
    [XmlElement]
    public string Description { get; set; } = string.Empty;

    /// <summary>Ordered list of templates belonging to this set (ICD-DAT-610).</summary>
    [XmlArray("Templates")]
    [XmlArrayItem("Template")]
    public List<TemplateConfig> Templates { get; set; } = new();
}
