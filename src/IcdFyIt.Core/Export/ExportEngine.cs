using System.Diagnostics;
using System.Runtime.InteropServices;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Model;
using Python.Runtime;

namespace IcdFyIt.Core.Export;

/// <summary>
/// Renders Mako templates against the current DataModel using Python.NET (ICD-FUN-70, ICD-DES §4.4).
/// The Python.NET runtime is initialised lazily on first use and kept alive for the application lifetime.
/// </summary>
public class ExportEngine
{
    private static bool _pythonReady;
    private static readonly object _initLock = new();

    /// <summary>
    /// Renders every template in <paramref name="templateSet"/> and writes the results to
    /// <paramref name="outputFolder"/> (ICD-FUN-90).
    /// </summary>
    /// <param name="settingsDir">
    /// Directory of settings.xml; used to resolve template file paths that are relative (ICD-DAT-620).
    /// </param>
    /// <param name="pythonPath">
    /// Absolute path to the Python 3 executable, or null to search PATH.
    /// </param>
    public void Export(
        DataModel         model,
        TemplateSetConfig templateSet,
        string?           settingsDir,
        string            outputFolder,
        string?           pythonPath = null)
    {
        EnsurePythonInitialised(pythonPath);
        Directory.CreateDirectory(outputFolder);

        using (Py.GIL())
        {
            foreach (var tmpl in templateSet.Templates)
            {
                var absPath    = ResolveFilePath(tmpl.FilePath, settingsDir);
                var outputName = RenderText(tmpl.OutputNamePattern, model).Trim();
                var content    = RenderFile(absPath, model);

                var outPath = Path.Combine(outputFolder, outputName);
                var outDir  = Path.GetDirectoryName(outPath);
                if (outDir is not null) Directory.CreateDirectory(outDir);
                File.WriteAllText(outPath, content);
            }
        }
    }

    // ── Private rendering helpers ─────────────────────────────────────────────

    private static string RenderText(string text, DataModel model)
    {
        using var scope = Py.CreateScope();
        scope.Set("__model__",   model.ToPython());
        scope.Set("__template__", text.ToPython());
        scope.Exec(
            "from mako.template import Template\n" +
            "__result__ = Template(text=__template__).render(model=__model__)\n");
        return scope.Get<string>("__result__") ?? string.Empty;
    }

    private static string RenderFile(string filePath, DataModel model)
    {
        using var scope = Py.CreateScope();
        scope.Set("__model__",    model.ToPython());
        scope.Set("__filename__", filePath.ToPython());
        scope.Exec(
            "from mako.template import Template\n" +
            "__result__ = Template(filename=__filename__).render(model=__model__)\n");
        return scope.Get<string>("__result__") ?? string.Empty;
    }

    // ── Python runtime initialisation ─────────────────────────────────────────

    private static void EnsurePythonInitialised(string? pythonPath)
    {
        if (_pythonReady) return;
        lock (_initLock)
        {
            if (_pythonReady) return;
            Runtime.PythonDLL = LocatePythonDll(pythonPath);
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            _pythonReady = true;
        }
    }

    private static string LocatePythonDll(string? configuredExePath)
    {
        var exe = !string.IsNullOrWhiteSpace(configuredExePath)
            ? configuredExePath!
            : DetectPythonExecutable();

        var script = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "import sys,os; print(os.path.join(os.path.dirname(sys.executable)," +
              "f'python{sys.version_info.major}{sys.version_info.minor}.dll'))"
            : "import sysconfig,os; d=sysconfig.get_config_var('LIBDIR') or '';" +
              "n=sysconfig.get_config_var('INSTSONAME') or '';" +
              "print(os.path.join(d,n) if d and n else n)";

        return RunPythonCommand(exe, script).Trim();
    }

    private static string DetectPythonExecutable()
    {
        foreach (var name in new[] { "python3", "python" })
        {
            if (CanRunPython(name)) return name;
        }
        throw new InvalidOperationException(
            "Python 3 was not found in PATH. Install Python 3 or configure the Python path in Options → General.");
    }

    private static bool CanRunPython(string exeName)
    {
        try
        {
            using var p = new Process();
            p.StartInfo = BuildStartInfo(exeName);
            p.StartInfo.ArgumentList.Add("--version");
            p.Start();
            p.WaitForExit(5_000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    private static string RunPythonCommand(string exeName, string script)
    {
        using var p = new Process();
        p.StartInfo = BuildStartInfo(exeName);
        p.StartInfo.ArgumentList.Add("-c");
        p.StartInfo.ArgumentList.Add(script);
        p.Start();
        var output = p.StandardOutput.ReadToEnd().Trim();
        var errors = p.StandardError.ReadToEnd().Trim();
        p.WaitForExit(10_000);
        if (p.ExitCode != 0 || string.IsNullOrEmpty(output))
            throw new InvalidOperationException(
                $"Could not locate the Python shared library. Make sure Python 3 is installed." +
                (errors.Length > 0 ? $" Detail: {errors}" : string.Empty));
        return output;
    }

    private static ProcessStartInfo BuildStartInfo(string exeName) => new()
    {
        FileName               = exeName,
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
        UseShellExecute        = false,
    };

    private static string ResolveFilePath(string filePath, string? settingsDir)
    {
        if (Path.IsPathRooted(filePath) || settingsDir is null) return filePath;
        return Path.Combine(settingsDir, filePath);
    }
}
