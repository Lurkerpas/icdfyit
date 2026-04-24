using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using IcdFyIt.App.ViewModels;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.Core.Tests.Application;

public class CsvRoundTripTests
{
    [Fact]
    public async Task ParametersCsv_ExportThenImport_RoundTripsSupportedFields()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"params_{Guid.NewGuid():N}.csv");

        try
        {
            var src = CreateContext();
            var p1 = src.Manager.AddParameter("ParamA");
            p1.Mnemonic = "PA";
            p1.NumericIdStr = "0x10";
            p1.ShortDescription = "Synthetic parameter";
            p1.Kind = ParameterKind.SyntheticValue;
            p1.Formula = "x+1";

            var p2 = src.Manager.AddParameter("ParamB");
            p2.Mnemonic = "PB";
            p2.NumericIdStr = "42";
            p2.ShortDescription = "Fixed value parameter";
            p2.Kind = ParameterKind.FixedValue;
            p2.HexValue = "0x2A";

            var srcVm = new ParametersWindowViewModel(src.Manager, src.Notifier, src.Dirty, src.MainVm)
            {
                RequestSaveCsvPath = () => Task.FromResult<string?>(tempPath)
            };

            await ExecuteAsync(srcVm.ExportCsvCommand);

            var dst = CreateContext();
            var dstVm = new ParametersWindowViewModel(dst.Manager, dst.Notifier, dst.Dirty, dst.MainVm)
            {
                RequestOpenCsvPath = () => Task.FromResult<string?>(tempPath)
            };

            await ExecuteAsync(dstVm.ImportCsvCommand);

            var actual = dst.Notifier.Parameters
                .Select(p => new
                {
                    p.Name,
                    p.Mnemonic,
                    p.NumericIdStr,
                    p.ShortDescription,
                    p.Kind,
                    p.Formula,
                    p.HexValue,
                })
                .OrderBy(p => p.Name)
                .ToList();

            var expected = new[] { p1, p2 }
                .Select(p => new
                {
                    p.Name,
                    p.Mnemonic,
                    p.NumericIdStr,
                    p.ShortDescription,
                    p.Kind,
                    p.Formula,
                    p.HexValue,
                })
                .OrderBy(p => p.Name)
                .ToList();

            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task MemoriesCsv_ExportThenImport_RoundTrips()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"memories_{Guid.NewGuid():N}.csv");

        try
        {
            var src = CreateContext();
            var m1 = src.Manager.AddMemory("EEPROM");
            m1.NumericIdStr = "0x1";
            m1.Mnemonic = "EEP";
            m1.SizeStr = "0x100";
            m1.Address = "0x8000";
            m1.Description = "Persistent memory";
            m1.AlignmentStr = "4";
            m1.IsWritable = true;
            m1.IsReadable = true;

            var m2 = src.Manager.AddMemory("ROM");
            m2.NumericIdStr = "2";
            m2.Mnemonic = "ROM";
            m2.SizeStr = "2048";
            m2.Address = "0x9000";
            m2.Description = "Read-only memory";
            m2.AlignmentStr = "8";
            m2.IsWritable = false;
            m2.IsReadable = true;

            var srcVm = new MemoriesWindowViewModel(src.Manager, src.Notifier, src.Dirty, src.MainVm)
            {
                RequestSaveCsvPath = () => Task.FromResult<string?>(tempPath)
            };

            await ExecuteAsync(srcVm.ExportCsvCommand);

            var dst = CreateContext();
            var dstVm = new MemoriesWindowViewModel(dst.Manager, dst.Notifier, dst.Dirty, dst.MainVm)
            {
                RequestOpenCsvPath = () => Task.FromResult<string?>(tempPath)
            };

            await ExecuteAsync(dstVm.ImportCsvCommand);

            var actual = dst.Notifier.Memories
                .Select(m => new
                {
                    m.Name,
                    m.NumericIdStr,
                    m.Mnemonic,
                    m.SizeStr,
                    m.Address,
                    m.Description,
                    m.AlignmentStr,
                    m.IsWritable,
                    m.IsReadable,
                })
                .OrderBy(m => m.Name)
                .ToList();

            var expected = new[] { m1, m2 }
                .Select(m => new
                {
                    m.Name,
                    m.NumericIdStr,
                    m.Mnemonic,
                    m.SizeStr,
                    m.Address,
                    m.Description,
                    m.AlignmentStr,
                    m.IsWritable,
                    m.IsReadable,
                })
                .OrderBy(m => m.Name)
                .ToList();

            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task MetadataCsv_ExportThenImport_RoundTripsBuiltInAndCustomFields()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"metadata_{Guid.NewGuid():N}.csv");

        try
        {
            var src = CreateContext();
            src.Manager.SetMetadataValue(MetadataBuiltInField.Name, "ICD-A");
            src.Manager.SetMetadataValue(MetadataBuiltInField.Version, "v1.2.3");
            src.Manager.SetMetadataValue(MetadataBuiltInField.Date, "2026-04-25");
            src.Manager.SetMetadataValue(MetadataBuiltInField.Status, "draft");
            src.Manager.SetMetadataValue(MetadataBuiltInField.Description, "Integration test metadata");

            var f1 = src.Manager.AddMetadataField("mission");
            src.Manager.UpdateMetadataFieldValue(f1, "LUNA");
            var f2 = src.Manager.AddMetadataField("owner");
            src.Manager.UpdateMetadataFieldValue(f2, "team-a");

            var srcVm = new MetadataWindowViewModel(src.Manager, src.Notifier, src.MainVm)
            {
                RequestSaveCsvPath = () => Task.FromResult<string?>(tempPath)
            };

            await ExecuteAsync(srcVm.ExportCsvCommand);

            var dst = CreateContext();
            var dstVm = new MetadataWindowViewModel(dst.Manager, dst.Notifier, dst.MainVm)
            {
                RequestOpenCsvPath = () => Task.FromResult<string?>(tempPath)
            };

            await ExecuteAsync(dstVm.ImportCsvCommand);

            dst.Manager.GetMetadataValue(MetadataBuiltInField.Name).Should().Be("ICD-A");
            dst.Manager.GetMetadataValue(MetadataBuiltInField.Version).Should().Be("v1.2.3");
            dst.Manager.GetMetadataValue(MetadataBuiltInField.Date).Should().Be("2026-04-25");
            dst.Manager.GetMetadataValue(MetadataBuiltInField.Status).Should().Be("draft");
            dst.Manager.GetMetadataValue(MetadataBuiltInField.Description).Should().Be("Integration test metadata");

            var actualCustom = dst.Notifier.MetadataFields
                .ToDictionary(f => f.Name, f => f.Value);

            actualCustom.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["mission"] = "LUNA",
                ["owner"] = "team-a",
            });
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static async Task ExecuteAsync(object command)
    {
        if (command is IAsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(null);
            return;
        }

        throw new InvalidOperationException("Expected an async relay command.");
    }

    private static (DataModelManager Manager, ChangeNotifier Notifier, DirtyTracker Dirty, MainWindowViewModel MainVm) CreateContext()
    {
        var notifier = new ChangeNotifier();
        var dirty = new DirtyTracker();
        var undo = new UndoRedoManager();
        var manager = new DataModelManager(notifier, dirty, undo);
        manager.New();
        var mainVm = new MainWindowViewModel(manager, notifier, dirty, new OptionsManager());
        return (manager, notifier, dirty, mainVm);
    }
}