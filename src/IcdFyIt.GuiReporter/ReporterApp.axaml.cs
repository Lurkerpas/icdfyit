using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace IcdFyIt.GuiReporter;

public partial class ReporterApp : Application
{
    private readonly string _modelPath;
    private readonly string _outputDir;

    // Parameterless constructor satisfies the Avalonia XAML loader (AVLN3001).
    public ReporterApp() : this(string.Empty, string.Empty) { }

    public ReporterApp(string modelPath, string outputDir)
    {
        _modelPath  = modelPath;
        _outputDir  = outputDir;
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Kick off capture after the framework is fully ready.
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var runner = new ScreenshotRunner(_modelPath, _outputDir);
                    await runner.RunAsync();
                    Console.WriteLine($"GuiReport saved to: {Path.GetFullPath(_outputDir)}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ERROR: {ex}");
                }
                finally
                {
                    desktop.Shutdown();
                }
            }, DispatcherPriority.Background);
        }
    }
}
