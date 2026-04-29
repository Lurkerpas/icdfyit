using FluentAssertions;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Persistence;
using Xunit;

namespace IcdFyIt.Core.Tests.Persistence;

public class YamlPersistenceTests
{
    private readonly YamlPersistence _sut = new();
    private readonly XmlPersistence  _xml = new();

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string TempFile(string ext = ".yaml")
        => Path.Combine(Path.GetTempPath(), $"icdfyit_test_{Guid.NewGuid():N}{ext}");

    /// <summary>Builds a DataModel that exercises every entity type and cross-reference.</summary>
    private static DataModel BuildRichModel()
    {
        var model = new DataModel();

        // Metadata
        model.Metadata.Name        = "TestIcd";
        model.Metadata.Version     = "1.0";
        model.Metadata.Date        = "2025-01-01";
        model.Metadata.Status      = "Draft";
        model.Metadata.Description = "Test description";
        model.Metadata.Fields.Add(new MetadataField { Name = "Author", Value = "Alice" });

        // Memories
        var mem = new Memory
        {
            Name         = "Main",
            NumericIdStr = "0x0001",
            SizeStr      = "0x1000",
            Address      = "0xFFFF0000",
            IsWritable   = true,
            IsReadable   = true,
            AlignmentStr = "4",
            Description  = "Main memory",
        };
        model.Memories.Add(mem);

        // Data types — scalar
        var sint = new SignedIntegerType
        {
            Name   = "Int16",
            Scalar = new ScalarProperties { BitSizeStr = "16", Endianness = Endianness.LittleEndian },
            Numeric = new NumericProperties
            {
                Range              = new NumericRange { Min = -100, Max = 100 },
                Unit               = "deg",
                CalibrationFormula = "x*0.1",
            },
        };
        var uint8 = new UnsignedIntegerType
        {
            Name   = "UInt8",
            Scalar = new ScalarProperties { BitSizeStr = "8", Endianness = Endianness.BigEndian },
        };
        var enumT = new EnumeratedType
        {
            Name       = "Mode",
            BitSizeStr = "8",
            Endianness = Endianness.LittleEndian,
        };
        enumT.Values.Add(new EnumeratedValue { Name = "Off",  RawValuesText = "0"    });
        enumT.Values.Add(new EnumeratedValue { Name = "On",   RawValuesText = "1"    });
        enumT.Values.Add(new EnumeratedValue { Name = "Idle", RawValuesText = "0xFF" });

        var structT = new StructureType { Name = "Frame" };
        structT.Fields.Add(new StructureField { Name = "Id",   DataType = uint8 });
        structT.Fields.Add(new StructureField { Name = "Mode", DataType = enumT });

        var arrayT = new ArrayType
        {
            Name        = "Buffer",
            ElementType = uint8,
            ArraySize   = new ArraySizeDescriptor
            {
                BitSizeStr = "16",
                Endianness = Endianness.LittleEndian,
                Range      = new NumericRange { Min = 0, Max = 255 },
            },
        };

        model.DataTypes.Add(sint);
        model.DataTypes.Add(uint8);
        model.DataTypes.Add(enumT);
        model.DataTypes.Add(structT);
        model.DataTypes.Add(arrayT);

        // Parameters
        var p1 = new Parameter
        {
            Name             = "Temperature",
            NumericIdStr     = "0x0010",
            Mnemonic         = "TEMP",
            Kind             = ParameterKind.HardwareAcquisition,
            DataType         = sint,
            ShortDescription = "Engine temp",
            LongDescription  = "Temperature of main engine",
            Formula          = "x*0.5",
            Memory           = mem,
            MemoryOffsetStr  = "0x0100",
            AlarmLow         = -50.0,
            AlarmHigh        = 200.0,
        };
        var p2 = new Parameter
        {
            Name             = "Status",
            NumericIdStr     = "0x0011",
            Kind             = ParameterKind.SoftwareAcquisition,
            DataType         = enumT,
            ValidityParameter = p1,
        };
        model.Parameters.Add(p1);
        model.Parameters.Add(p2);

        // Header types
        var ht = new HeaderType { Name = "StdHeader", Description = "Standard header" };
        ht.Ids.Add(new HeaderTypeId { Name = "Apid", DataType = uint8 });
        ht.Ids.Add(new HeaderTypeId { Name = "SeqNum", DataType = sint });
        model.HeaderTypes.Add(ht);

        // Packet types
        var pt = new PacketType
        {
            Name        = "TM_ENG",
            NumericIdStr = "0x0100",
            Kind        = PacketTypeKind.Telemetry,
            HeaderType  = ht,
            Description = "Engineering TM",
        };
        pt.HeaderIdValues.Add(new HeaderIdValue { IdRef = ht.Ids[0].Id, Value = "0x01" });
        pt.Fields.Add(new PacketField { Name = "TempField",   Parameter = p1, IsTypeIndicator = false });
        pt.Fields.Add(new PacketField { Name = "TypeFlag", IsTypeIndicator = true, IndicatorValue = "0xAA" });
        model.PacketTypes.Add(pt);

        return model;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Round-trip tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ExportThenImport_PreservesAllEntities()
    {
        var model  = BuildRichModel();
        var path   = TempFile();
        try
        {
            _sut.Export(model, path);
            var loaded = _sut.Import(path);

            // Metadata
            loaded.Metadata.Name.Should().Be("TestIcd");
            loaded.Metadata.Version.Should().Be("1.0");
            loaded.Metadata.Fields.Should().HaveCount(1);
            loaded.Metadata.Fields[0].Name.Should().Be("Author");
            loaded.Metadata.Fields[0].Value.Should().Be("Alice");

            // Memories
            loaded.Memories.Should().HaveCount(1);
            var m = loaded.Memories[0];
            m.Name.Should().Be("Main");
            m.NumericIdStr.Should().Be("0x0001");
            m.SizeStr.Should().Be("0x1000");
            m.AlignmentStr.Should().Be("4");
            m.IsWritable.Should().BeTrue();
            m.IsReadable.Should().BeTrue();

            // Data types
            loaded.DataTypes.Should().HaveCount(5);
            var sint = loaded.DataTypes.OfType<SignedIntegerType>().Single();
            sint.Name.Should().Be("Int16");
            sint.Scalar.BitSizeStr.Should().Be("16");
            sint.Numeric!.Unit.Should().Be("deg");
            sint.Numeric.Range.Min.Should().BeApproximately(-100, 1e-9);

            var enumDt = loaded.DataTypes.OfType<EnumeratedType>().Single();
            enumDt.Values.Should().HaveCount(3);
            enumDt.Values[2].Name.Should().Be("Idle");
            enumDt.Values[2].RawValuesText.Should().Be("0xFF");

            var structDt = loaded.DataTypes.OfType<StructureType>().Single();
            structDt.Fields.Should().HaveCount(2);
            structDt.Fields[0].DataType.Should().NotBeNull();
            structDt.Fields[0].DataType!.Name.Should().Be("UInt8");

            var arrayDt = loaded.DataTypes.OfType<ArrayType>().Single();
            arrayDt.ElementType.Should().NotBeNull();
            arrayDt.ElementType!.Name.Should().Be("UInt8");
            arrayDt.ArraySize.Should().NotBeNull();
            arrayDt.ArraySize!.BitSizeStr.Should().Be("16");

            // Parameters
            loaded.Parameters.Should().HaveCount(2);
            var p1 = loaded.Parameters[0];
            p1.Name.Should().Be("Temperature");
            p1.NumericIdStr.Should().Be("0x0010");
            p1.Mnemonic.Should().Be("TEMP");
            p1.Kind.Should().Be(ParameterKind.HardwareAcquisition);
            p1.DataType.Should().NotBeNull();
            p1.DataType!.Name.Should().Be("Int16");
            p1.Memory.Should().NotBeNull();
            p1.Memory!.Name.Should().Be("Main");
            p1.MemoryOffsetStr.Should().Be("0x0100");
            p1.AlarmLow.Should().Be(-50.0);
            p1.AlarmHigh.Should().Be(200.0);

            var p2 = loaded.Parameters[1];
            p2.ValidityParameter.Should().NotBeNull();
            p2.ValidityParameter!.Name.Should().Be("Temperature");

            // Header types
            loaded.HeaderTypes.Should().HaveCount(1);
            var ht = loaded.HeaderTypes[0];
            ht.Ids.Should().HaveCount(2);
            ht.Ids[0].Name.Should().Be("Apid");

            // Packet types
            loaded.PacketTypes.Should().HaveCount(1);
            var pt = loaded.PacketTypes[0];
            pt.Name.Should().Be("TM_ENG");
            pt.HeaderType.Should().NotBeNull();
            pt.HeaderIdValues.Should().HaveCount(1);
            pt.Fields.Should().HaveCount(2);
            pt.Fields[1].IsTypeIndicator.Should().BeTrue();
            pt.Fields[1].IndicatorValue.Should().Be("0xAA");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void YamlToXmlToYaml_RoundTrip_ProducesEquivalentOutput()
    {
        // Arrange
        var model    = BuildRichModel();
        var yamlPath = TempFile(".yaml");
        var xmlPath  = TempFile(".xml");
        var yaml2Path = TempFile(".yaml");

        try
        {
            // Act: model → YAML → XML → YAML
            _sut.Export(model, yamlPath);
            var fromYaml = _sut.Import(yamlPath);
            _xml.Save(fromYaml, xmlPath);
            var fromXml  = _xml.Load(xmlPath);
            _sut.Export(fromXml, yaml2Path);

            // Assert: parse both YAML outputs and compare key structures
            var r1 = _sut.Import(yamlPath);
            var r2 = _sut.Import(yaml2Path);

            r2.DataTypes.Select(dt => dt.Name).Should()
                .BeEquivalentTo(r1.DataTypes.Select(dt => dt.Name));
            r2.Parameters.Select(p => p.Name).Should()
                .BeEquivalentTo(r1.Parameters.Select(p => p.Name));
            r2.Memories.Select(m => m.Name).Should()
                .BeEquivalentTo(r1.Memories.Select(m => m.Name));
            r2.HeaderTypes.Select(ht => ht.Name).Should()
                .BeEquivalentTo(r1.HeaderTypes.Select(ht => ht.Name));
            r2.PacketTypes.Select(pt => pt.Name).Should()
                .BeEquivalentTo(r1.PacketTypes.Select(pt => pt.Name));
            r2.Metadata.Name.Should().Be(r1.Metadata.Name);
        }
        finally
        {
            File.Delete(yamlPath);
            File.Delete(xmlPath);
            File.Delete(yaml2Path);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Include directive tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Import_WithIncludes_MergesEntities()
    {
        var dir  = Path.GetTempPath();
        var sub  = Path.Combine(dir, $"sub_{Guid.NewGuid():N}.yaml");
        var root = Path.Combine(dir, $"root_{Guid.NewGuid():N}.yaml");

        try
        {
            File.WriteAllText(sub,
                "version: 1\n" +
                "memories:\n" +
                "  Flash:\n" +
                "    numeric_id: '1'\n" +
                "    size: '0x4000'\n" +
                "    is_writable: false\n" +
                "    is_readable: true\n");

            File.WriteAllText(root,
                $"version: 1\n" +
                $"includes:\n" +
                $"  - {Path.GetFileName(sub)}\n" +
                "memories:\n" +
                "  Ram:\n" +
                "    numeric_id: '2'\n" +
                "    size: '0x1000'\n" +
                "    is_writable: true\n" +
                "    is_readable: true\n");

            var model = _sut.Import(root);

            model.Memories.Should().HaveCount(2);
            model.Memories.Select(m => m.Name).Should().Contain("Flash").And.Contain("Ram");
        }
        finally
        {
            File.Delete(sub);
            File.Delete(root);
        }
    }

    [Fact]
    public void Import_WithCircularIncludes_ThrowsInvalidDataException()
    {
        var dir = Path.GetTempPath();
        var a   = Path.Combine(dir, $"circ_a_{Guid.NewGuid():N}.yaml");
        var b   = Path.Combine(dir, $"circ_b_{Guid.NewGuid():N}.yaml");

        try
        {
            File.WriteAllText(a,
                $"version: 1\n" +
                $"includes:\n" +
                $"  - {Path.GetFileName(b)}\n");

            File.WriteAllText(b,
                $"version: 1\n" +
                $"includes:\n" +
                $"  - {Path.GetFileName(a)}\n");

            var act = () => _sut.Import(a);
            act.Should().Throw<InvalidDataException>()
               .WithMessage("*Circular include*");
        }
        finally
        {
            File.Delete(a);
            File.Delete(b);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Version guard
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Import_TooNewVersion_ThrowsInvalidDataException()
    {
        var path = TempFile();
        try
        {
            File.WriteAllText(path,
                $"version: {YamlPersistence.CurrentVersion + 1}\n");

            var act = () => _sut.Import(path);
            act.Should().Throw<InvalidDataException>()
               .WithMessage("*version*");
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Name-based reference resolution
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Import_UnresolvedDataTypeReference_YieldsNullDataType()
    {
        var path = TempFile();
        try
        {
            File.WriteAllText(path,
                "version: 1\n" +
                "parameters:\n" +
                "  P1:\n" +
                "    numeric_id: '1'\n" +
                "    kind: SoftwareSetting\n" +
                "    data_type: NonExistentType\n");

            var model = _sut.Import(path);

            model.Parameters.Should().HaveCount(1);
            model.Parameters[0].DataType.Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // String-only fields round-trip
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Export_HexNumericId_RoundTripsAsHexString()
    {
        var model = new DataModel();
        model.Parameters.Add(new Parameter
        {
            Name         = "P",
            NumericIdStr = "0xABCD",
            Kind         = ParameterKind.FixedValue,
        });

        var path = TempFile();
        try
        {
            _sut.Export(model, path);
            var loaded = _sut.Import(path);
            loaded.Parameters[0].NumericIdStr.Should().Be("0xABCD");
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Include de-duplication
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Import_SameFileIncludedTwice_LoadsOnlyOnce()
    {
        var dir  = Path.GetTempPath();
        var sub  = Path.Combine(dir, $"dedup_sub_{Guid.NewGuid():N}.yaml");
        var mid1 = Path.Combine(dir, $"dedup_mid1_{Guid.NewGuid():N}.yaml");
        var mid2 = Path.Combine(dir, $"dedup_mid2_{Guid.NewGuid():N}.yaml");
        var root = Path.Combine(dir, $"dedup_root_{Guid.NewGuid():N}.yaml");

        try
        {
            // sub defines one memory
            File.WriteAllText(sub,
                "version: 1\n" +
                "memories:\n" +
                "  Shared:\n" +
                "    numeric_id: '1'\n" +
                "    size: '256'\n" +
                "    is_writable: false\n" +
                "    is_readable: true\n");

            // mid1 includes sub
            File.WriteAllText(mid1,
                $"version: 1\n" +
                $"includes:\n" +
                $"  - {Path.GetFileName(sub)}\n");

            // mid2 also includes sub
            File.WriteAllText(mid2,
                $"version: 1\n" +
                $"includes:\n" +
                $"  - {Path.GetFileName(sub)}\n");

            // root includes both mids
            File.WriteAllText(root,
                $"version: 1\n" +
                $"includes:\n" +
                $"  - {Path.GetFileName(mid1)}\n" +
                $"  - {Path.GetFileName(mid2)}\n");

            var model = _sut.Import(root);
            // "Shared" should appear exactly once
            model.Memories.Where(m => m.Name == "Shared").Should().HaveCount(1);
        }
        finally
        {
            File.Delete(sub);
            File.Delete(mid1);
            File.Delete(mid2);
            File.Delete(root);
        }
    }
}
