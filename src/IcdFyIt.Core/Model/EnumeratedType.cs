using System.Xml.Serialization;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Enumerated Data Type (ICD-DAT-60, ICD-DAT-61).
/// Carries a list of named enumeration values as a composite collection.
/// </summary>
public sealed class EnumeratedType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.Enumerated;

    [XmlAttribute]
    public Endianness Endianness { get; set; } = Endianness.LittleEndian;

    /// <summary>Wire-size of the encoded enumeration value in bits.</summary>
    private int _bitSize = 32;
    private string? _bitSizeStr;

    [XmlIgnore]
    public int BitSize
    {
        get => _bitSize;
        set { _bitSize = value; _bitSizeStr = null; }
    }

    [XmlAttribute("BitSize")]
    public string BitSizeStr
    {
        get => _bitSizeStr ?? _bitSize.ToString();
        set { _bitSizeStr = value; _bitSize = HexInt.Parse(value); }
    }

    public List<EnumeratedValue> Values { get; set; } = new();
}
