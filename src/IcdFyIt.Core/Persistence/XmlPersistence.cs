using System.Xml;
using System.Xml.Serialization;
using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Persistence;

/// <summary>
/// Serialises and deserialises a <see cref="DataModel"/> to/from XML.
/// Cross-entity references are serialised as GUID strings and resolved back to
/// object references after deserialisation (ICD-DES-92).
/// Supports sequential schema migration; refuses files with a newer schema version (ICD-DES-91).
/// </summary>
public class XmlPersistence
{
    /// <summary>Latest schema version understood by this build.</summary>
    public const int CurrentSchemaVersion = 1;

    private static readonly XmlSerializer Serializer = new(typeof(DataModel));

    /// <summary>Serialises <paramref name="model"/> to the specified file path.</summary>
    public void Save(DataModel model, string filePath)
    {
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  " };
        using var writer = XmlWriter.Create(filePath, settings);
        Serializer.Serialize(writer, model);
    }

    /// <summary>
    /// Deserialises a DataModel from the specified file path, resolves GUID references,
    /// and applies any required schema migrations.
    /// </summary>
    public DataModel Load(string filePath)
    {
        DataModel model;
        using (var reader = XmlReader.Create(filePath))
            model = (DataModel)Serializer.Deserialize(reader)!;

        if (model.SchemaVersion > CurrentSchemaVersion)
            throw new NotSupportedException(
                $"File schema version {model.SchemaVersion} is newer than the " +
                $"supported version {CurrentSchemaVersion}. Please upgrade the application.");

        ResolveReferences(model);
        return model;
    }

    // ── Reference resolution ───────────────────────────────────────────────────

    private static void ResolveReferences(DataModel model)
    {
        var typeById = model.DataTypes.ToDictionary(dt => dt.Id);

        // DataType internal cross-references (Structure fields, Array element types)
        foreach (var dt in model.DataTypes)
        {
            if (dt is StructureType st)
                foreach (var field in st.Fields)
                    field.DataType = Resolve(field._storedDataTypeIdRef, typeById);

            if (dt is ArrayType at)
                at.ElementType = Resolve(at._storedElementTypeIdRef, typeById);
        }

        // Parameter → DataType
        foreach (var p in model.Parameters)
            p.DataType = Resolve(p._storedDataTypeIdRef, typeById);

        // PacketField → Parameter
        var paramById = model.Parameters.ToDictionary(p => p.Id);
        foreach (var pt in model.PacketTypes)
            foreach (var f in pt.Fields)
                f.Parameter = Resolve(f._storedParameterIdRef, paramById);

        // HeaderTypeId → DataType
        foreach (var ht in model.HeaderTypes)
            foreach (var htId in ht.Ids)
                htId.DataType = Resolve(htId._storedDataTypeIdRef, typeById);

        // PacketType → HeaderType
        var headerTypeById = model.HeaderTypes.ToDictionary(ht => ht.Id);
        foreach (var pt in model.PacketTypes)
            pt.HeaderType = Resolve(pt._storedHeaderTypeIdRef, headerTypeById);
    }

    private static T? Resolve<T>(string? guidStr, Dictionary<Guid, T> map)
        where T : class
        => guidStr != null && Guid.TryParse(guidStr, out var id)
            ? map.GetValueOrDefault(id)
            : null;
}
