using System.Xml.Serialization;

namespace IcdFyIt.Core.Model;

/// <summary>
/// Abstract base for all Data Type entities (ICD-DAT-50, ICD-FUN-41).
/// Each concrete subclass represents exactly one <see cref="BaseType"/>.
/// Circular references are forbidden (ICD-FUN-42).
/// <see cref="System.Xml.Serialization.XmlIncludeAttribute"/> entries let
/// <see cref="System.Xml.Serialization.XmlSerializer"/> round-trip the polymorphic hierarchy.
/// </summary>
[XmlInclude(typeof(SignedIntegerType))]
[XmlInclude(typeof(UnsignedIntegerType))]
[XmlInclude(typeof(FloatType))]
[XmlInclude(typeof(BooleanType))]
[XmlInclude(typeof(BitStringType))]
[XmlInclude(typeof(EnumeratedType))]
[XmlInclude(typeof(StructureType))]
[XmlInclude(typeof(ArrayType))]
public abstract class DataType
{
    [XmlAttribute]
    public Guid Id { get; set; } = Guid.NewGuid();

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Returns the fixed <see cref="BaseType"/> value for this concrete class.
    /// Used by the UI for display without run-time type checks.
    /// Not serialised — the concrete XML element name carries the type information.
    /// </summary>
    [XmlIgnore]
    public abstract BaseType Kind { get; }

    public override string ToString() => Name;
}
