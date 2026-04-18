using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Array Data Type (ICD-DAT-90, ICD-DAT-91).
/// Element type is held as a composite reference; may be null if the
/// referenced type was deleted (ICD-FUN-40).
/// </summary>
public sealed class ArrayType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.Array;

    [XmlIgnore]
    public DataType? ElementType { get; set; }

    /// <summary>GUID reference for XML serialization; resolved post-load.</summary>
    [XmlAttribute("ElementTypeRef")]
    public string? ElementTypeIdRef
    {
        get => ElementType?.Id.ToString();
        set => _storedElementTypeIdRef = value;
    }
    internal string? _storedElementTypeIdRef;

    public ArraySizeDescriptor? ArraySize { get; set; }
}
