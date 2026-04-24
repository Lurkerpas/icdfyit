using FluentAssertions;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Persistence;
using Xunit;

namespace IcdFyIt.Core.Tests.Persistence;

public class XmlPersistenceTests
{
    private readonly XmlPersistence _sut = new();

    [Fact]
    public void SaveThenLoad_ProducesEquivalentModel()
    {
        var model = new DataModel();
        model.DataTypes.Add(new SignedIntegerType { Name = "MyType" });
        var path = Path.Combine(Path.GetTempPath(), $"icdfyit_test_{Guid.NewGuid():N}.xml");

        try
        {
            _sut.Save(model, path);
            var loaded = _sut.Load(path);

            loaded.DataTypes.Should().HaveCount(1);
            loaded.DataTypes[0].Name.Should().Be("MyType");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_NewerSchemaVersion_Throws()
    {
        // Arrange: write a file with a schema version higher than CurrentSchemaVersion
        var path = Path.Combine(Path.GetTempPath(), $"icdfyit_test_{Guid.NewGuid():N}.xml");
        var xml = $"<DataModel SchemaVersion=\"{XmlPersistence.CurrentSchemaVersion + 1}\" />";
        File.WriteAllText(path, xml);

        try
        {
            _sut.Invoking(s => s.Load(path))
                .Should().Throw<NotSupportedException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveThenLoad_RestoresReferencesAndMetadata()
    {
        var path = Path.Combine(Path.GetTempPath(), $"icdfyit_test_{Guid.NewGuid():N}.xml");

        var dt = new SignedIntegerType { Name = "U16" };
        var memory = new Memory { Name = "EEPROM", NumericIdStr = "0x1", SizeStr = "0x100" };
        var parameter = new Parameter
        {
            Name = "ParamA",
            NumericIdStr = "0x10",
            DataType = dt,
            Memory = memory,
            MemoryOffsetStr = "0x4",
        };

        var headerType = new HeaderType
        {
            Name = "PrimaryHeader",
            Description = "Main header",
            Ids =
            [
                new HeaderTypeId
                {
                    Name = "TypeId",
                    Description = "Type id",
                    DataType = dt,
                }
            ]
        };

        var packetType = new PacketType
        {
            Name = "TC_A",
            Kind = PacketTypeKind.Telecommand,
            NumericIdStr = "0x20",
            HeaderType = headerType,
            Fields =
            [
                new PacketField
                {
                    Name = "FieldA",
                    Parameter = parameter,
                }
            ]
        };

        var model = new DataModel
        {
            DataTypes = [dt],
            Parameters = [parameter],
            PacketTypes = [packetType],
            HeaderTypes = [headerType],
            Memories = [memory],
            Metadata = new IcdMetadata
            {
                Name = "MYICD",
                Version = "v1",
                Date = "2026-04-25",
                Status = "draft",
                Description = "Round-trip test",
                Fields =
                [
                    new MetadataField { Name = "mission", Value = "LUNA" },
                    new MetadataField { Name = "owner", Value = "team-a" },
                ]
            }
        };

        try
        {
            _sut.Save(model, path);
            var loaded = _sut.Load(path);

            loaded.DataTypes.Should().ContainSingle();
            loaded.Parameters.Should().ContainSingle();
            loaded.PacketTypes.Should().ContainSingle();
            loaded.HeaderTypes.Should().ContainSingle();
            loaded.Memories.Should().ContainSingle();

            loaded.Metadata.Name.Should().Be("MYICD");
            loaded.Metadata.Version.Should().Be("v1");
            loaded.Metadata.Date.Should().Be("2026-04-25");
            loaded.Metadata.Status.Should().Be("draft");
            loaded.Metadata.Description.Should().Be("Round-trip test");
            loaded.Metadata.Fields.Should().HaveCount(2);
            loaded.Metadata.Fields.Should().Contain(f => f.Name == "mission" && f.Value == "LUNA");
            loaded.Metadata.Fields.Should().Contain(f => f.Name == "owner" && f.Value == "team-a");

            var loadedDt = loaded.DataTypes.Single();
            var loadedMemory = loaded.Memories.Single();
            var loadedParam = loaded.Parameters.Single();
            var loadedHeaderType = loaded.HeaderTypes.Single();
            var loadedPacket = loaded.PacketTypes.Single();

            loadedParam.DataType.Should().NotBeNull();
            loadedParam.DataType!.Id.Should().Be(loadedDt.Id);
            loadedParam.Memory.Should().NotBeNull();
            loadedParam.Memory!.Id.Should().Be(loadedMemory.Id);

            loadedHeaderType.Ids.Should().ContainSingle();
            loadedHeaderType.Ids[0].DataType.Should().NotBeNull();
            loadedHeaderType.Ids[0].DataType!.Id.Should().Be(loadedDt.Id);

            loadedPacket.HeaderType.Should().NotBeNull();
            loadedPacket.HeaderType!.Id.Should().Be(loadedHeaderType.Id);
            loadedPacket.Fields.Should().ContainSingle();
            loadedPacket.Fields[0].Parameter.Should().NotBeNull();
            loadedPacket.Fields[0].Parameter!.Id.Should().Be(loadedParam.Id);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
