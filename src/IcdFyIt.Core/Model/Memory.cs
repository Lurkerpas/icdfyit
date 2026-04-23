using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// A Memory entity representing a named memory region (ICD-DAT-510 to ICD-DAT-560).
/// </summary>
public class Memory
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public int NumericId { get; set; }

    [XmlAttribute]
    public string? Mnemonic { get; set; }

    [XmlAttribute]
    public int Size { get; set; }

    [XmlAttribute]
    public string? Address { get; set; }

    public string? Description { get; set; }

    [XmlAttribute]
    public int Alignment { get; set; } = 1;

    [XmlAttribute]
    public bool IsWritable { get; set; }

    [XmlAttribute]
    public bool IsReadable { get; set; } = true;

    public override string ToString() => Name;
}
