using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Signed integer Data Type (ICD-DAT-101, ICD-DAT-102).
/// Scalar and Numeric characteristics are held as composite objects.
/// </summary>
public sealed class SignedIntegerType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.SignedInteger;

    public ScalarProperties Scalar { get; set; } = new() { Endianness = Endianness.LittleEndian, BitSize = 16 };

    public NumericProperties? Numeric { get; set; }
}
