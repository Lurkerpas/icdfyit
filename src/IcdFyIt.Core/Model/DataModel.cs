using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Root aggregate of a single ICD project file (ICD-DAT-10 to ICD-DAT-30).
/// Schema version is incremented on breaking changes; see XmlPersistence for migration (ICD-DES-91).
/// </summary>
[XmlRoot("DataModel")]
public class DataModel
{
    /// <summary>Schema version; bumped on breaking serialization changes.</summary>
    [XmlAttribute]
    public int SchemaVersion { get; set; } = 1;

    public List<DataType> DataTypes { get; set; } = new();

    public List<Parameter> Parameters { get; set; } = new();

    public List<PacketType> PacketTypes { get; set; } = new();
}
