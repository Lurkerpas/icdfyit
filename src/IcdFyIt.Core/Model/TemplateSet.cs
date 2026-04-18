using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A named collection of Templates (ICD-DAT-180, ICD-IF-73, ICD-DES-81).
/// </summary>
public class TemplateSet
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Description { get; set; } = string.Empty;

    public List<Template> Templates { get; set; } = new();
}
