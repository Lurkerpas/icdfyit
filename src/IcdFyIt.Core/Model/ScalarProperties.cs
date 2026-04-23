using System.Xml.Serialization;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.Core.Model;

/// <summary> Endianness and bit size common to all scalar Data Types (ICD-DAT-101, ICD-DAT-110, ICD-DAT-120). </summary>
public class ScalarProperties
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
}
