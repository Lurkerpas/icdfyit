using System.Xml.Serialization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Loads and saves <see cref="AppOptions"/> to/from settings.xml (ICD-FUN-101).
/// The settings file lives in the OS-appropriate user-profile directory.
/// </summary>
public class OptionsManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "icdfyit",
        "settings.xml");

    /// <summary>Directory that contains settings.xml; used to resolve relative template paths.</summary>
    public string SettingsDirectory => Path.GetDirectoryName(SettingsPath)!;

    private static readonly XmlSerializer Serializer = new(typeof(AppOptions));

    /// <summary>Loads options from disk; returns defaults if file does not exist.</summary>
    public AppOptions Load()
    {
        if (!File.Exists(SettingsPath)) return new AppOptions();
        try
        {
            using var stream = File.OpenRead(SettingsPath);
            return (AppOptions?)Serializer.Deserialize(stream) ?? new AppOptions();
        }
        catch
        {
            return new AppOptions();
        }
    }

    /// <summary>Persists options to disk (ICD-FUN-100).</summary>
    public void Save(AppOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        using var stream = new FileStream(SettingsPath, FileMode.Create, FileAccess.Write, FileShare.None);
        Serializer.Serialize(stream, options);
    }
}
