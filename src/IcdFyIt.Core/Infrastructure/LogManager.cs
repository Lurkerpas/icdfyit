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
        string logPath = Path.Combine(logDirectory, "icdfyit-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 1)
            .CreateLogger();
    }

    /// <summary>Flushes and closes the log on graceful shutdown.</summary>
    public static void Shutdown() => Log.CloseAndFlush();
}
