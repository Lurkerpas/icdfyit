using System.Xml.Serialization;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.Core.Model;

/// <summary> Size descriptor for an Array Data Type (ICD-DAT-91). </summary>
public class ArraySizeDescriptor
{
    public Endianness Endianness { get; set; }

    private int _bitSize;
    private string? _bitSizeStr;

    [XmlIgnore]
    public int BitSize
    {
        get => _bitSize;
        set { _bitSize = value; _bitSizeStr = null; }
    }

    [XmlElement("BitSize")]
    public string BitSizeStr
    {
        get => _bitSizeStr ?? _bitSize.ToString();
        set { _bitSizeStr = value; _bitSize = HexInt.Parse(value); }
    }

    public NumericRange Range { get; set; } = new();
}
