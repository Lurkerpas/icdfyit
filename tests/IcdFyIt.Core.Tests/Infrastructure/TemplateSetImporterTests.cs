using System.Xml.Serialization;
using FluentAssertions;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.Core.Tests.Infrastructure;

public class TemplateSetImporterTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), $"icdfyit_importer_{Guid.NewGuid():N}");

    public TemplateSetImporterTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private string WriteDefinition(TemplateSetDefinitionFile def)
    {
        var path = Path.Combine(_dir, "set.xml");
        var ser = new XmlSerializer(typeof(TemplateSetDefinitionFile));
        using var w = File.Create(path);
        ser.Serialize(w, def);
        return path;
    }

    // ── Name / Description round-trip ─────────────────────────────────────────

    [Fact]
    public void Import_CopiesNameAndDescription()
    {
        var path = WriteDefinition(new TemplateSetDefinitionFile
        {
            Name        = "MySet",
            Description = "A test set",
        });

        var result = TemplateSetImporter.Import(path);

        result.Name.Should().Be("MySet");
        result.Description.Should().Be("A test set");
    }

    // ── Relative path resolution (ICD-FUN-141) ───────────────────────────────

    [Fact]
    public void Import_ResolvesRelativePathToAbsolute()
    {
        var path = WriteDefinition(new TemplateSetDefinitionFile
        {
            Name = "S",
            Templates =
            [
                new TemplateDefinitionEntry
                {
                    Name              = "T",
                    FilePath          = "templates/foo.mako",
                    OutputNamePattern = "out.txt",
                }
            ]
        });

        var result = TemplateSetImporter.Import(path);

        var expected = Path.GetFullPath(Path.Combine(_dir, "templates/foo.mako"));
        result.Templates[0].FilePath.Should().Be(expected);
        Path.IsPathRooted(result.Templates[0].FilePath).Should().BeTrue();
    }

    [Fact]
    public void Import_PreservesAlreadyAbsolutePath()
    {
        var absPath = Path.Combine(_dir, "absolute", "bar.mako");
        var path = WriteDefinition(new TemplateSetDefinitionFile
        {
            Name = "S",
            Templates =
            [
                new TemplateDefinitionEntry
                {
                    Name              = "T",
                    FilePath          = absPath,
                    OutputNamePattern = "out.txt",
                }
            ]
        });

        var result = TemplateSetImporter.Import(path);

        result.Templates[0].FilePath.Should().Be(absPath);
    }

    // ── Environment variable paths preserved (ICD-FUN-142) ──────────────────

    [Fact]
    public void Import_PreservesPathWithLeadingEnvVar()
    {
        const string rawPath = "${HOME}/templates/icd.mako";
        var path = WriteDefinition(new TemplateSetDefinitionFile
        {
            Name = "S",
            Templates =
            [
                new TemplateDefinitionEntry
                {
                    Name              = "T",
                    FilePath          = rawPath,
                    OutputNamePattern = "out.txt",
                }
            ]
        });

        var result = TemplateSetImporter.Import(path);

        result.Templates[0].FilePath.Should().Be(rawPath);
    }

    [Fact]
    public void Import_PreservesPathWithEmbeddedEnvVar()
    {
        const string rawPath = "templates/${FOO}/icd.mako";
        var path = WriteDefinition(new TemplateSetDefinitionFile
        {
            Name = "S",
            Templates =
            [
                new TemplateDefinitionEntry
                {
                    Name              = "T",
                    FilePath          = rawPath,
                    OutputNamePattern = "out.txt",
                }
            ]
        });

        var result = TemplateSetImporter.Import(path);

        result.Templates[0].FilePath.Should().Be(rawPath);
    }

    // ── Version guard ─────────────────────────────────────────────────────────

    [Fact]
    public void Import_ThrowsForNewerVersion()
    {
        var path = WriteDefinition(new TemplateSetDefinitionFile
        {
            Version = TemplateSetDefinitionFile.CurrentVersion + 1,
            Name    = "S",
        });

        var act = () => TemplateSetImporter.Import(path);

        act.Should().Throw<InvalidDataException>()
           .WithMessage("*version*");
    }

    // ── Multiple templates ─────────────────────────────────────────────────────

    [Fact]
    public void Import_CopiesAllTemplatesInOrder()
    {
        var path = WriteDefinition(new TemplateSetDefinitionFile
        {
            Name = "S",
            Templates =
            [
                new TemplateDefinitionEntry { Name = "A", FilePath = "a.mako", OutputNamePattern = "a.txt" },
                new TemplateDefinitionEntry { Name = "B", FilePath = "b.mako", OutputNamePattern = "b.txt" },
                new TemplateDefinitionEntry { Name = "C", FilePath = "c.mako", OutputNamePattern = "c.txt" },
            ]
        });

        var result = TemplateSetImporter.Import(path);

        result.Templates.Should().HaveCount(3);
        result.Templates.Select(t => t.Name).Should().Equal("A", "B", "C");
    }
}
