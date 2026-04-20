using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// One template entry inside a <see cref="TemplateSetConfig"/> (ICD-DAT-620 to ICD-DAT-650).
/// </summary>
public class TemplateConfig
{
    /// <summary>Display name of the template (ICD-DAT-630).</summary>
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable description (ICD-DAT-640).</summary>
    [XmlElement]
    public string Description { get; set; } = string.Empty;

    /// <summary>Absolute or relative path to the Mako template file (ICD-DAT-620).</summary>
    [XmlElement]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Mako expression rendered to produce the output file name (ICD-DAT-650).</summary>
    [XmlElement]
    public string OutputNamePattern { get; set; } = string.Empty;
}
