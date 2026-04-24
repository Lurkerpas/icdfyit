using System.Xml.Serialization;
using Serilog;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Loads and saves <see cref="AppOptions"/> to/from settings.xml (ICD-FUN-101).
/// The settings file lives in a per-user application data directory.
/// </summary>
public class OptionsManager
{
    private const string AppFolderName = "icdfyit";
    private static readonly string SettingsDirectoryPath = ResolveSettingsDirectory();
    private static readonly string SettingsPath = Path.Combine(SettingsDirectoryPath, "settings.xml");

    /// <summary>Directory that contains settings.xml; used to resolve relative template paths.</summary>
    public string SettingsDirectory => SettingsDirectoryPath;

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
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load settings from {SettingsPath}; defaults will be used", SettingsPath);
            return new AppOptions();
        }
    }

    /// <summary>Persists options to disk (ICD-FUN-100).</summary>
    public void Save(AppOptions options)
    {
        Directory.CreateDirectory(SettingsDirectoryPath);
        using var stream = new FileStream(SettingsPath, FileMode.Create, FileAccess.Write, FileShare.None);
        Serializer.Serialize(stream, options);
    }

    private static string ResolveSettingsDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(root))
            root = Directory.GetCurrentDirectory();

        return Path.Combine(root, AppFolderName);
    }
}
