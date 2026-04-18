using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A single export template belonging to a TemplateSet (ICD-DAT-180).
/// FilePath is relative to the directory containing settings.xml (ICD-DES-81).
/// </summary>
public class Template
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Description { get; set; } = string.Empty;

    /// <summary>Path to the Mako template file, relative to settings.xml directory.</summary>
    [XmlAttribute]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Pattern used to derive the output file name, e.g. "${name}_types.h".</summary>
    [XmlAttribute]
    public string OutputNamePattern { get; set; } = string.Empty;
}
