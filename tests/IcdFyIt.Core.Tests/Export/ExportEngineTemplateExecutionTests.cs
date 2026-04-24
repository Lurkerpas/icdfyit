using System.Diagnostics;
using FluentAssertions;
using IcdFyIt.Core.Export;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Tests.Export;

public class ExportEngineTemplateExecutionTests
{
    [Fact]
    public void Export_ExecutesTemplateAndWritesExpectedOutput()
    {
        var pythonExe = FindPythonWithMako();
        if (pythonExe is null)
            return;

        var root = Path.Combine(Path.GetTempPath(), $"icdfyit_export_{Guid.NewGuid():N}");
        var templateDir = Path.Combine(root, "templates");
        var outputDir = Path.Combine(root, "out");
        Directory.CreateDirectory(templateDir);
        Directory.CreateDirectory(outputDir);

        try
        {
            var templatePath = Path.Combine(templateDir, "report.mako");
            File.WriteAllText(templatePath,
                "ICD=${model.Metadata.Name}\n" +
                "Version=${model.Metadata.Version}\n" +
                "ParamCount=${len(model.Parameters)}\n" +
                "% for p in model.Parameters:\n" +
                "P:${p.Name}|${p.NumericId}\n" +
                "% endfor\n");

            var model = new DataModel();
            model.Metadata.Name = "MYICD";
            model.Metadata.Version = "v2";
            model.Parameters.Add(new Parameter { Name = "A", NumericId = 10 });
            model.Parameters.Add(new Parameter { Name = "B", NumericId = 11 });

            var set = new TemplateSetConfig
            {
                Name = "test",
                Description = "template execution test",
                Templates =
                [
                    new TemplateConfig
                    {
                        Name = "report",
                        Description = "report template",
                        FilePath = "report.mako",
                        OutputNamePattern = "${model.Metadata.Name}_${model.Metadata.Version}.txt",
                    }
                ]
            };

            var sut = new ExportEngine();
            sut.Export(model, set, templateDir, outputDir, pythonExe);

            var outFile = Path.Combine(outputDir, "MYICD_v2.txt");
            File.Exists(outFile).Should().BeTrue();

            var content = File.ReadAllText(outFile);
            content.Should().Contain("ICD=MYICD");
            content.Should().Contain("Version=v2");
            content.Should().Contain("ParamCount=2");
            content.Should().Contain("P:A|10");
            content.Should().Contain("P:B|11");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static string? FindPythonWithMako()
    {
        foreach (var exe in new[] { "python3", "python" })
        {
            try
            {
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                p.StartInfo.ArgumentList.Add("-c");
                p.StartInfo.ArgumentList.Add("import mako,sys; print(sys.executable)");
                p.Start();
                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(5_000);
                if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    return exe;
            }
            catch
            {
                // Try the next candidate.
            }
        }

        return null;
    }
}