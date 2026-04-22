using System.Xml.Serialization;
using IcdFyIt.Core.Export;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Persistence;
using IcdFyIt.Core.Services;

namespace IcdFyIt.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintGeneralHelp();
            return 0;
        }

        var verb = args[0].ToLowerInvariant();
        var verbArgs = args.Skip(1).ToArray();

        return verb switch
        {
            "validate" => HandleValidate(verbArgs),
            "export" => HandleExport(verbArgs),
            _ => HandleUnknownVerb(verb),
        };
    }

    private static int HandleValidate(string[] args)
    {
        if (args.Length == 0 || args.Any(IsHelp))
        {
            PrintValidateHelp();
            return 0;
        }

        if (!TryParseOptions(args, out var options, out var error))
        {
            Console.Error.WriteLine($"Error: {error}");
            PrintValidateHelp();
            return 2;
        }

        if (!TryGetOption(options, "m", "model", out var modelPath))
        {
            Console.Error.WriteLine("Error: Missing required option --model (-m).");
            PrintValidateHelp();
            return 2;
        }

        try
        {
            var model = new XmlPersistence().Load(modelPath);
            var issues = new ModelValidator().Validate(model);

            if (issues.Count == 0)
            {
                Console.WriteLine("Validation successful: no issues found.");
                return 0;
            }

            Console.WriteLine($"Validation failed: {issues.Count} issue(s) found.");
            for (var i = 0; i < issues.Count; i++)
                Console.WriteLine($"{i + 1}. {issues[i].Message}");

            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Validation failed: {ex.Message}");
            return 1;
        }
    }

    private static int HandleExport(string[] args)
    {
        if (args.Length == 0 || args.Any(IsHelp))
        {
            PrintExportHelp();
            return 0;
        }

        if (!TryParseOptions(args, out var options, out var error))
        {
            Console.Error.WriteLine($"Error: {error}");
            PrintExportHelp();
            return 2;
        }

        if (!TryGetOption(options, "m", "model", out var modelPath))
        {
            Console.Error.WriteLine("Error: Missing required option --model (-m).");
            PrintExportHelp();
            return 2;
        }

        if (!TryGetOption(options, "o", "output", out var outputDir))
        {
            Console.Error.WriteLine("Error: Missing required option --output (-o).");
            PrintExportHelp();
            return 2;
        }

        if (!TryGetOption(options, "t", "template-set", out var templateSetName))
        {
            Console.Error.WriteLine("Error: Missing required option --template-set (-t).");
            PrintExportHelp();
            return 2;
        }

        options.TryGetValue("s", out var settingsPath);
        settingsPath ??= options.GetValueOrDefault("settings");

        try
        {
            var model = new XmlPersistence().Load(modelPath);
            var issues = new ModelValidator().Validate(model);
            if (issues.Count > 0)
            {
                Console.Error.WriteLine($"Export aborted: model has {issues.Count} validation issue(s).");
                for (var i = 0; i < issues.Count; i++)
                    Console.Error.WriteLine($"{i + 1}. {issues[i].Message}");
                return 1;
            }

            if (!TryLoadOptions(settingsPath, out var appOptions, out var settingsDir, out var optionsError))
            {
                Console.Error.WriteLine($"Export failed: {optionsError}");
                return 1;
            }

            var templateSet = appOptions.TemplateSets
                .FirstOrDefault(ts => string.Equals(ts.Name, templateSetName, StringComparison.OrdinalIgnoreCase));

            if (templateSet is null)
            {
                Console.Error.WriteLine($"Export failed: template set \"{templateSetName}\" was not found.");
                if (appOptions.TemplateSets.Count == 0)
                    Console.Error.WriteLine("No template sets are configured.");
                else
                    Console.Error.WriteLine("Available template sets: " +
                                            string.Join(", ", appOptions.TemplateSets.Select(ts => ts.Name)));
                return 1;
            }

            var engine = new ExportEngine();
            engine.Export(model, templateSet, settingsDir, outputDir, appOptions.PythonPath);

            Console.WriteLine($"Export successful: wrote output to \"{outputDir}\".");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Export failed: {ex.Message}");
            return 1;
        }
    }

    private static int HandleUnknownVerb(string verb)
    {
        Console.Error.WriteLine($"Error: Unknown verb \"{verb}\".");
        PrintGeneralHelp();
        return 2;
    }

    private static bool TryLoadOptions(
        string? settingsPath,
        out AppOptions appOptions,
        out string? settingsDir,
        out string? error)
    {
        appOptions = new AppOptions();
        settingsDir = null;
        error = null;

        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            var manager = new OptionsManager();
            appOptions = manager.Load();
            settingsDir = manager.SettingsDirectory;
            return true;
        }

        if (!File.Exists(settingsPath))
        {
            error = $"settings file does not exist: \"{settingsPath}\".";
            return false;
        }

        try
        {
            var serializer = new XmlSerializer(typeof(AppOptions));
            using var stream = File.OpenRead(settingsPath);
            appOptions = (AppOptions?)serializer.Deserialize(stream) ?? new AppOptions();
            settingsDir = Path.GetDirectoryName(Path.GetFullPath(settingsPath));
            return true;
        }
        catch (Exception ex)
        {
            error = $"could not load settings file \"{settingsPath}\": {ex.Message}";
            return false;
        }
    }

    private static bool TryParseOptions(
        IReadOnlyList<string> args,
        out Dictionary<string, string> options,
        out string? error)
    {
        options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = null;

        for (var i = 0; i < args.Count; i++)
        {
            var token = args[i];
            if (!token.StartsWith('-'))
            {
                error = $"Unexpected positional argument: \"{token}\".";
                return false;
            }

            var key = token switch
            {
                _ when token.StartsWith("--") => token[2..],
                _ when token.StartsWith("-") && token.Length == 2 => token[1..],
                _ => string.Empty,
            };

            if (string.IsNullOrWhiteSpace(key))
            {
                error = $"Invalid option format: \"{token}\".";
                return false;
            }

            if (i + 1 >= args.Count || args[i + 1].StartsWith('-'))
            {
                error = $"Option \"{token}\" requires a value.";
                return false;
            }

            options[key] = args[++i];
        }

        return true;
    }

    private static bool TryGetOption(
        IReadOnlyDictionary<string, string> options,
        string shortName,
        string longName,
        out string value)
    {
        if (options.TryGetValue(shortName, out value!)) return true;
        if (options.TryGetValue(longName, out value!)) return true;

        value = string.Empty;
        return false;
    }

    private static bool IsHelp(string arg)
        => string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase);

    private static void PrintGeneralHelp()
    {
        Console.WriteLine("icdfyit-cli");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  icdfyit-cli validate --model <path>");
        Console.WriteLine("  icdfyit-cli export --model <path> --output <dir> --template-set <name> [--settings <path>]");
        Console.WriteLine();
        Console.WriteLine("Verbs:");
        Console.WriteLine("  validate   Validate the model and print issues.");
        Console.WriteLine("  export     Generate files by applying a template set.");
        Console.WriteLine();
        Console.WriteLine("Use --help with a verb for details, e.g.:");
        Console.WriteLine("  icdfyit-cli validate --help");
    }

    private static void PrintValidateHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  icdfyit-cli validate --model <path>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -m, --model <path>     Input model XML path.");
        Console.WriteLine("  -h, --help             Show this help.");
    }

    private static void PrintExportHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  icdfyit-cli export --model <path> --output <dir> --template-set <name> [--settings <path>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -m, --model <path>           Input model XML path.");
        Console.WriteLine("  -o, --output <dir>           Output directory for generated files.");
        Console.WriteLine("  -s, --settings <path>        Optional settings.xml path (defaults to app settings). ");
        Console.WriteLine("  -t, --template-set <name>    Template set name from settings.");
        Console.WriteLine("  -h, --help                   Show this help.");
    }
}