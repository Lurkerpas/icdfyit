using Avalonia;
using Avalonia.Headless;

namespace IcdFyIt.GuiReporter;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Resolve paths from command-line args or defaults (relative to CWD).
        string modelPath  = ParseArg(args, "--model",  "testmodel.xml");
        string outputDir  = ParseArg(args, "--output", "GuiReport");

        if (!File.Exists(modelPath))
        {
            // Try falling back to the demo/ directory relative to the repo root.
            var candidate = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "..", "demo", "testmodel.xml");
            if (File.Exists(candidate))
                modelPath = Path.GetFullPath(candidate);
            else
            {
                Console.Error.WriteLine($"Model file not found: {modelPath}");
                Console.Error.WriteLine("Pass --model <path> to specify the XML model file.");
                Environment.Exit(1);
            }
        }

        BuildAvaloniaApp(modelPath, outputDir)
            .StartWithClassicDesktopLifetime(args);
    }

    static AppBuilder BuildAvaloniaApp(string modelPath, string outputDir)
        => AppBuilder
            .Configure(() => new ReporterApp(modelPath, outputDir))
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia()
            .WithInterFont()
            .LogToTrace();

    static string ParseArg(string[] args, string flag, string defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == flag) return args[i + 1];
        return defaultValue;
    }
}
