using Serilog;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Bootstraps Serilog for the application lifetime.
/// Writes to a rolling log file that rotates daily and retains at most one day of history (ICD-IF-201).
/// </summary>
public static class LogManager
{
    /// <summary>Call once at application start to configure the global Serilog logger.</summary>
    public static void Initialise(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        // Delete any pre-existing log files so that only one log survives (ICD-IF-201).
        foreach (var old in Directory.GetFiles(logDirectory, "log*.txt"))
        {
            try
            {
                File.Delete(old);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[icdfyit] Failed to delete old log file '{old}': {ex}");
            }
        }

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string logPath   = Path.Combine(logDirectory, $"log{timestamp}.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath)
            .CreateLogger();
    }

    /// <summary>Flushes and closes the log on graceful shutdown.</summary>
    public static void Shutdown() => Log.CloseAndFlush();
}
