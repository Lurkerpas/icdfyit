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

    /// <summary>
    /// The element type of this array.
    /// Nullable: set to null when the referenced type is deleted (ICD-FUN-40).
    /// </summary>
    public DataType? ElementType { get; set; }

    public ArraySizeDescriptor? ArraySize { get; set; }
}
