using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Structure Data Type (ICD-DAT-80).
/// Carries an ordered list of named fields as a composite collection.
/// </summary>
public sealed class StructureType : DataType
{
    [XmlIgnore]
    public override BaseType Kind => BaseType.Structure;

    public List<StructureField> Fields { get; set; } = new();
}
