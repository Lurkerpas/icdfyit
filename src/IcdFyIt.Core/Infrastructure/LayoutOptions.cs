using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>Persisted size for a specific window key.</summary>
public class WindowSizeOption
{
    [XmlAttribute]
    public string Key { get; set; } = string.Empty;

    [XmlAttribute]
    public double Width { get; set; }

    [XmlAttribute]
    public double Height { get; set; }
}

/// <summary>Persisted column widths for one DraggableGrid instance.</summary>
public class GridColumnSizeOption
{
    [XmlAttribute]
    public string Key { get; set; } = string.Empty;

    [XmlArray]
    [XmlArrayItem("Width")]
    public List<double> Widths { get; set; } = new();
}