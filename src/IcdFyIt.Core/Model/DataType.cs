using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A Data Type entity. Base type determines which optional component properties are populated.
/// All entities carry a GUID (ICD-FUN-41). Circular references are forbidden (ICD-FUN-42).
/// </summary>
public class DataType
{
    [XmlAttribute]
    public Guid Id { get; init; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public BaseType BaseType { get; set; }

    // Scalar types: SignedInteger, UnsignedInteger, Float, Boolean, BitString (ICD-DAT-101)
    public ScalarProperties? Scalar { get; set; }

    // Numeric types: SignedInteger, UnsignedInteger, Float (ICD-DAT-102)
    public NumericProperties? Numeric { get; set; }

    // Enumerated (ICD-DAT-60, ICD-DAT-61)
    public List<EnumeratedValue>? EnumeratedValues { get; set; }

    // Structure (ICD-DAT-80)
    public List<StructureField>? StructureFields { get; set; }

    // Array (ICD-DAT-90, ICD-DAT-91)
    public DataType? ArrayElementType { get; set; }
    public ArraySizeDescriptor? ArraySize { get; set; }
}
