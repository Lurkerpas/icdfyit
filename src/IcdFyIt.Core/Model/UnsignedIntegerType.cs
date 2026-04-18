using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Unsigned integer Data Type (ICD-DAT-101, ICD-DAT-102).
/// Scalar and Numeric characteristics are held as composite objects.
/// </summary>
public sealed class UnsignedIntegerType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.UnsignedInteger;

    public ScalarProperties Scalar { get; set; } = new() { Endianness = Endianness.LittleEndian, BitSize = 16 };

    public NumericProperties? Numeric { get; set; }
}
