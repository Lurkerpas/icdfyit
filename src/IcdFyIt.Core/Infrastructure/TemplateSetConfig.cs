using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Reference entry stored inside <see cref="AppOptions"/> pointing to a TemplateSet
/// definition on disk (ICD-DES-81).
/// </summary>
public class TemplateSetConfig
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    /// <summary>Path to the TemplateSet XML file, relative to settings.xml directory.</summary>
    [XmlAttribute]
    public string FilePath { get; set; } = string.Empty;
}
