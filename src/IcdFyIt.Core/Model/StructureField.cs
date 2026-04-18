using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary> One field in a Structure Data Type. </summary>
public class StructureField
{
    public string Name { get; set; } = string.Empty;

    [XmlIgnore]
    public DataType? DataType { get; set; }

    /// <summary>GUID reference for XML serialization; resolved post-load.</summary>
    [XmlAttribute("DataTypeRef")]
    public string? DataTypeIdRef
    {
        get => DataType?.Id.ToString();
        set => _storedDataTypeIdRef = value;
    }
    internal string? _storedDataTypeIdRef;
}
