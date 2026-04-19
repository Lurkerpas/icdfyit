using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A Parameter entity (ICD-DAT-110 to ICD-DAT-160).
/// DataType reference may be null if the referenced type was deleted (ICD-FUN-40).
/// </summary>
public class Parameter
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? LongDescription { get; set; }

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

    [XmlAttribute]
    public int NumericId { get; set; }

    [XmlAttribute]
    public string? Mnemonic { get; set; }

    [XmlAttribute]
    public ParameterKind Kind { get; set; }

    /// <summary>Python formula string; only meaningful when Kind == SyntheticValue.</summary>
    public string? Formula { get; set; } = string.Empty;

    /// <summary>Hex string value; only meaningful when Kind == FixedValue.</summary>
    public string? HexValue { get; set; }

    public override string ToString() => Name;
}
