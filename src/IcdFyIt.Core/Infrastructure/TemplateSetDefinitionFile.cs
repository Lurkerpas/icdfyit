using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Serialization root for a Template Set definition XML file (ICD-DAT-660, ICD-DAT-661).
/// These files travel alongside their template files and describe a complete Template Set
/// so users can distribute and import the set in one step (ICD-FUN-140).
/// </summary>
[XmlRoot("TemplateSetDefinition")]
public class TemplateSetDefinitionFile
{
    /// <summary>
    /// File format version (ICD-DAT-661).
    /// Must equal <see cref="CurrentVersion"/>; migrations are applied when loading older versions.
    /// </summary>
    [XmlAttribute]
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Display name of the Template Set (ICD-DAT-600).</summary>
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable description of the Template Set (ICD-DAT-601).</summary>
    [XmlElement]
    public string Description { get; set; } = string.Empty;

    /// <summary>Template entries described by this definition file (ICD-DAT-610, ICD-DAT-662).</summary>
    [XmlArray("Templates")]
    [XmlArrayItem("Template")]
    public List<TemplateDefinitionEntry> Templates { get; set; } = new();

    /// <summary>Highest file-format version this code can read.</summary>
    public const int CurrentVersion = 1;
}

/// <summary>
/// One template entry inside a <see cref="TemplateSetDefinitionFile"/> (ICD-DAT-620 to ICD-DAT-650).
/// </summary>
public class TemplateDefinitionEntry
{
    /// <summary>Display name of the template (ICD-DAT-630).</summary>
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable description (ICD-DAT-640).</summary>
    [XmlAttribute]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Path to the Mako template file (ICD-DAT-662).
    /// May be relative (resolved against the definition XML's directory at import time),
    /// absolute, or contain <c>${VAR_NAME}</c> environment variable references
    /// (expanded at access time per ICD-FUN-142).
    /// </summary>
    [XmlElement]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Mako expression rendered to produce the output file name (ICD-DAT-650).</summary>
    [XmlElement]
    public string OutputNamePattern { get; set; } = string.Empty;
}
