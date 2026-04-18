using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Bit-string Data Type (ICD-DAT-101).
/// Scalar characteristics are held as a composite object.
/// </summary>
public sealed class BitStringType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.BitString;

    public ScalarProperties Scalar { get; set; } = new() { Endianness = Endianness.LittleEndian, BitSize = 16 };
}
