using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Floating-point Data Type (ICD-DAT-101, ICD-DAT-102).
/// Scalar and Numeric characteristics are held as composite objects.
/// </summary>
public sealed class FloatType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.Float;

    public ScalarProperties Scalar { get; set; } = new() { Endianness = Endianness.LittleEndian, BitSize = 32 };

    public NumericProperties? Numeric { get; set; }
}
