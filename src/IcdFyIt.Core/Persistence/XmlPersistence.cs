using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Persistence;

/// <summary>
/// Serialises and deserialises a <see cref="DataModel"/> to/from XML.
/// Uses GUID-string references to avoid duplicate objects in the XML (ICD-DES-92).
/// Supports sequential schema migration on load; refuses files with a newer schema version (ICD-DES-91).
/// </summary>
public class XmlPersistence
{
    /// <summary>Latest schema version understood by this build.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Serialises <paramref name="model"/> to the specified file path.</summary>
    public void Save(DataModel model, string filePath) => throw new NotImplementedException();

    /// <summary>
    /// Deserialises a DataModel from the specified file path.
    /// Migrates older schemas up to <see cref="CurrentSchemaVersion"/>.
    /// Throws <see cref="NotSupportedException"/> if the file schema version is newer than this build.
    /// </summary>
    public DataModel Load(string filePath) => throw new NotImplementedException();
}
