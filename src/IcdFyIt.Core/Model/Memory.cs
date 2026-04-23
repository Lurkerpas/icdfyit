using System.Xml.Serialization;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A Memory entity representing a named memory region (ICD-DAT-510 to ICD-DAT-560).
/// Numeric fields use a dual int+string pattern so that hexadecimal notation entered
/// by the user is preserved through save/reload (ICD-DAT-800).
/// </summary>
public class Memory
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    // ── NumericId ─────────────────────────────────────────────────────────────
    private int _numericId;
    private string? _numericIdStr;

    /// <summary>Parsed integer value; used by internal logic (duplicate checks, auto-increment).</summary>
    [XmlIgnore]
    public int NumericId
    {
        get => _numericId;
        set { _numericId = value; _numericIdStr = null; }
    }

    /// <summary>String representation preserved from user input (may be "0x…" or decimal).</summary>
    [XmlAttribute("NumericId")]
    public string NumericIdStr
    {
        get => _numericIdStr ?? _numericId.ToString();
        set { _numericIdStr = value; _numericId = HexInt.Parse(value); }
    }

    [XmlAttribute]
    public string? Mnemonic { get; set; }

    // ── Size ──────────────────────────────────────────────────────────────────
    private int _size;
    private string? _sizeStr;

    [XmlIgnore]
    public int Size
    {
        get => _size;
        set { _size = value; _sizeStr = null; }
    }

    [XmlAttribute("Size")]
    public string SizeStr
    {
        get => _sizeStr ?? _size.ToString();
        set { _sizeStr = value; _size = HexInt.Parse(value); }
    }

    [XmlAttribute]
    public string? Address { get; set; }

    public string? Description { get; set; }

    // ── Alignment ─────────────────────────────────────────────────────────────
    private int _alignment = 1;
    private string? _alignmentStr;

    [XmlIgnore]
    public int Alignment
    {
        get => _alignment;
        set { _alignment = value; _alignmentStr = null; }
    }

    [XmlAttribute("Alignment")]
    public string AlignmentStr
    {
        get => _alignmentStr ?? _alignment.ToString();
        set { _alignmentStr = value; _alignment = HexInt.Parse(value); }
    }

    [XmlAttribute]
    public bool IsWritable { get; set; }

    [XmlAttribute]
    public bool IsReadable { get; set; } = true;

    public override string ToString() => Name;
}

