using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A single ID entry within a <see cref="HeaderType"/> (ICD-DAT-730).
/// DataType reference may be null if the referenced type was deleted (ICD-FUN-51).
/// </summary>
public class HeaderTypeId
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Nullable: reference to a DataType; may be null if deleted.</summary>
    [XmlIgnore]
    public DataType? DataType { get; set; }

    /// <summary>GUID reference for XML serialization; resolved post-load by XmlPersistence.</summary>
    [XmlAttribute("DataTypeRef")]
    public string? DataTypeIdRef
    {
        get => DataType?.Id.ToString();
        set => _storedDataTypeIdRef = value;
    }
    internal string? _storedDataTypeIdRef;
}
