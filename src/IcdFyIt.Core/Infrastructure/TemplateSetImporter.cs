using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Loads a Template Set definition XML file and produces a <see cref="TemplateSetConfig"/>
/// ready to be added to <see cref="AppOptions.TemplateSets"/> (ICD-FUN-140, ICD-FUN-141).
/// </summary>
public static class TemplateSetImporter
{
    private static readonly XmlSerializer _serializer =
        new(typeof(TemplateSetDefinitionFile));

    /// <summary>
    /// Imports a Template Set from the given definition XML file.
    /// Relative template file paths are resolved to absolute paths anchored at the
    /// directory containing <paramref name="xmlFilePath"/> (ICD-FUN-141).
    /// Paths that contain <c>${VAR_NAME}</c> environment variable references are left
    /// unexpanded so they are resolved at access time (ICD-FUN-142).
    /// </summary>
    /// <param name="xmlFilePath">Absolute or relative path to the definition XML file.</param>
    /// <returns>A populated <see cref="TemplateSetConfig"/> instance.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when the file cannot be deserialized or its version is unsupported.
    /// </exception>
    public static TemplateSetConfig Import(string xmlFilePath)
    {
        var definition = Deserialize(xmlFilePath);

        if (definition.Version > TemplateSetDefinitionFile.CurrentVersion)
            throw new InvalidDataException(
                $"Template set definition file '{xmlFilePath}' uses format version " +
                $"{definition.Version}, but this application only supports up to version " +
                $"{TemplateSetDefinitionFile.CurrentVersion}.");

        var xmlDir = Path.GetDirectoryName(Path.GetFullPath(xmlFilePath))
                     ?? Directory.GetCurrentDirectory();

        return new TemplateSetConfig
        {
            Name        = definition.Name,
            Description = definition.Description,
            Templates   = definition.Templates
                .Select(t => new TemplateConfig
                {
                    Name              = t.Name,
                    Description       = t.Description,
                    FilePath          = ResolveTemplatePath(t.FilePath, xmlDir),
                    OutputNamePattern = t.OutputNamePattern,
                })
                .ToList(),
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a template file path relative to the XML file's directory.
    /// Paths that are already absolute, or that contain <c>${VAR_NAME}</c> references,
    /// are returned unchanged (ICD-FUN-141, ICD-FUN-142).
    /// </summary>
    private static string ResolveTemplatePath(string filePath, string baseDir)
    {
        // Paths containing env-var placeholders are expanded at access time; preserve them.
        if (filePath.Contains("${", StringComparison.Ordinal)) return filePath;

        if (Path.IsPathRooted(filePath)) return filePath;

        return Path.GetFullPath(Path.Combine(baseDir, filePath));
    }

    private static TemplateSetDefinitionFile Deserialize(string xmlFilePath)
    {
        using var stream = File.OpenRead(xmlFilePath);
        return _serializer.Deserialize(stream) as TemplateSetDefinitionFile
               ?? throw new InvalidDataException(
                   $"Could not deserialize template set definition file '{xmlFilePath}'.");
    }
}
