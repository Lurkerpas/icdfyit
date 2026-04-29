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

    /// <summary>UI scale factor. Allowed values: 1.0 (Small), 1.5 (Medium), 2.0 (Large), 3.0 (Very Large). Default: 1.0.</summary>
    [XmlElement]
    public double UiScale { get; set; } = 1.0;

    /// <summary>
    /// Absolute path to the Python interpreter used by ExportEngine.
    /// Null means the system PATH is searched.
    /// </summary>
    [XmlElement]
    public string? PythonPath { get; set; }

    /// <summary>Named export template sets configured by the user (ICD-IF-73, ICD-DES-81).</summary>
    [XmlArray]
    public List<TemplateSetConfig> TemplateSets { get; set; } = new();

    /// <summary>Paths of the 32 most recently opened files, newest first.</summary>
    [XmlArray]
    [XmlArrayItem("File")]
    public List<string> RecentFiles { get; set; } = new();

    /// <summary>Persisted window sizes keyed by window type name.</summary>
    [XmlArray]
    [XmlArrayItem("Window")]
    public List<WindowSizeOption> WindowSizes { get; set; } = new();

    /// <summary>Persisted DraggableGrid column widths keyed by window/grid identity.</summary>
    [XmlArray]
    [XmlArrayItem("Grid")]
    public List<GridColumnSizeOption> GridColumnSizes { get; set; } = new();

    /// <summary>Last output folder chosen in the Export window; restored on next open.</summary>
    [XmlElement]
    public string? LastExportFolder { get; set; }
}
