using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Stores the fixed hex value assigned to one Header Type ID within a Packet Type (ICD-DAT-414).
/// </summary>
public class HeaderIdValue
{
    /// <summary>The <see cref="HeaderTypeId.Id"/> this value belongs to.</summary>
    [XmlAttribute]
    public Guid IdRef { get; set; }

    /// <summary>The fixed hex value (e.g. "0x1A").</summary>
    [XmlAttribute]
    public string Value { get; set; } = string.Empty;
}
