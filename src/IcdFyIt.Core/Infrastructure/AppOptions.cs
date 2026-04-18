using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Application settings persisted in settings.xml (ICD-FUN-100, ICD-FUN-101, ICD-DES-81).
/// </summary>
[XmlRoot("Settings")]
public class AppOptions
{
    /// <summary>Maximum number of undo steps retained in memory (ICD-FUN-51). Default: 64.</summary>
    [XmlElement]
    public int UndoDepth { get; set; } = 64;

    /// <summary>
    /// Absolute path to the Python interpreter used by ExportEngine.
    /// Null means the system PATH is searched.
    /// </summary>
    [XmlElement]
    public string? PythonPath { get; set; }

    /// <summary>Named export template sets configured by the user (ICD-IF-73, ICD-DES-81).</summary>
    [XmlArray]
    public List<TemplateSetConfig> TemplateSets { get; set; } = new();
}
