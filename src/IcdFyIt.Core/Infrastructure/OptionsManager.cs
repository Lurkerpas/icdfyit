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

    /// <summary>Loads options from disk; returns defaults if file does not exist.</summary>
    public AppOptions Load() => throw new NotImplementedException();

    /// <summary>Persists options to disk (ICD-FUN-100).</summary>
    public void Save(AppOptions options) => throw new NotImplementedException();
}
