using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Enumerated Data Type (ICD-DAT-60, ICD-DAT-61).
/// Carries a list of named enumeration values as a composite collection.
/// </summary>
public sealed class EnumeratedType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.Enumerated;

    public List<EnumeratedValue> Values { get; set; } = new();
}
