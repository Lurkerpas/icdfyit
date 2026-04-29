using IcdFyIt.Core.Model;
using IcdFyIt.Core.Persistence.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IcdFyIt.Core.Persistence;

/// <summary>
/// Serialises and deserialises a <see cref="DataModel"/> to/from YAML (ICD-FUN-150, ICD-FUN-151).
///
/// YAML conventions:
/// - Entity names are YAML keys; references between entities use names (ICD-IF-441 to ICD-IF-445).
/// - Dual-representation numeric fields are written using their string form only (ICD-IF-450).
/// - GUIDs do not appear in YAML; fresh GUIDs are generated on import (ICD-IF-460).
/// - Multi-file composition is driven by <c>includes</c> at each file's root (ICD-IF-420, ICD-IF-421).
/// </summary>
public class YamlPersistence
{
    /// <summary>Highest YAML format version understood by this build.</summary>
    public const int CurrentVersion = 1;

    private readonly IDeserializer _deserializer;
    private readonly ISerializer   _serializer;

    public YamlPersistence()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Serialises <paramref name="model"/> to a single YAML file (ICD-FUN-150).</summary>
    public void Export(DataModel model, string filePath)
    {
        var doc  = ToDocument(model);
        var yaml = _serializer.Serialize(doc);
        File.WriteAllText(filePath, yaml);
    }

    /// <summary>
    /// Deserialises a DataModel from <paramref name="filePath"/>, resolving all
    /// <c>includes</c> directives (ICD-FUN-151, ICD-IF-420, ICD-IF-421).
    /// </summary>
    public DataModel Import(string filePath)
    {
        var doc = LoadWithIncludes(filePath, new HashSet<string>(), new HashSet<string>());
        if (doc.Version > CurrentVersion)
            throw new InvalidDataException(
                $"YAML file version {doc.Version} is newer than the supported version {CurrentVersion}. " +
                "Please upgrade the application.");
        return ToModel(doc);
    }

    // ── Export: DataModel → YamlDocument ─────────────────────────────────────

    private static YamlDocument ToDocument(DataModel model) => new()
    {
        Metadata    = ToMetadataDto(model.Metadata),
        DataTypes   = ToDataTypesDict(model.DataTypes),
        Parameters  = ToParametersDict(model.Parameters),
        HeaderTypes = ToHeaderTypesDict(model.HeaderTypes),
        PacketTypes = ToPacketTypesDict(model.PacketTypes),
        Memories    = ToMemoriesDict(model.Memories),
    };

    private static YamlMetadataDto ToMetadataDto(IcdMetadata meta) => new()
    {
        Name        = NullIfEmpty(meta.Name),
        Version     = NullIfEmpty(meta.Version),
        Date        = NullIfEmpty(meta.Date),
        Status      = NullIfEmpty(meta.Status),
        Description = NullIfEmpty(meta.Description),
        Fields      = meta.Fields.Count > 0
            ? meta.Fields.Select(f => new YamlMetadataFieldDto { Name = f.Name, Value = f.Value }).ToList()
            : null,
    };

    private static Dictionary<string, YamlDataTypeDto>? ToDataTypesDict(List<DataType> types)
    {
        if (types.Count == 0) return null;
        return types.ToDictionary(dt => dt.Name, ToDataTypeDto);
    }

    private static YamlDataTypeDto ToDataTypeDto(DataType dt) => dt switch
    {
        SignedIntegerType   si  => ScalarDto("sint",      si.Scalar,  si.Numeric),
        UnsignedIntegerType ui  => ScalarDto("uint",      ui.Scalar,  ui.Numeric),
        FloatType           ft  => ScalarDto("float",     ft.Scalar,  ft.Numeric),
        BooleanType         bt  => ScalarDto("bool",      bt.Scalar,  null),
        BitStringType       bst => ScalarDto("bitstring", bst.Scalar, null),
        EnumeratedType      et  => ToEnumDto(et),
        StructureType       st  => ToStructDto(st),
        ArrayType           at  => ToArrayDto(at),
        _                       => new YamlDataTypeDto { Type = "unknown" },
    };

    private static YamlDataTypeDto ScalarDto(string type, ScalarProperties scalar, NumericProperties? numeric) => new()
    {
        Type        = type,
        Endianness  = scalar.Endianness.ToString(),
        BitSize     = scalar.BitSizeStr,
        Range       = numeric is null ? null : RangeDto(numeric.Range),
        Unit        = NullIfEmpty(numeric?.Unit),
        Calibration = NullIfEmpty(numeric?.CalibrationFormula),
    };

    private static YamlDataTypeDto ToEnumDto(EnumeratedType et) => new()
    {
        Type       = "enum",
        Endianness = et.Endianness.ToString(),
        BitSize    = et.BitSizeStr,
        Values     = et.Values.Select(v => new YamlEnumValueDto
        {
            Name      = v.Name,
            RawValues = NullIfEmpty(v.RawValuesText),
        }).ToList(),
    };

    private static YamlDataTypeDto ToStructDto(StructureType st) => new()
    {
        Type   = "struct",
        Fields = st.Fields.Select(f => new YamlStructFieldDto
        {
            Name     = f.Name,
            DataType = f.DataType?.Name,
        }).ToList(),
    };

    private static YamlDataTypeDto ToArrayDto(ArrayType at) => new()
    {
        Type        = "array",
        ElementType = at.ElementType?.Name,
        ArraySize   = at.ArraySize is null ? null : new YamlArraySizeDto
        {
            Endianness = at.ArraySize.Endianness.ToString(),
            BitSize    = at.ArraySize.BitSizeStr,
            Range      = RangeDto(at.ArraySize.Range),
        },
    };

    private static Dictionary<string, YamlParameterDto>? ToParametersDict(List<Parameter> parameters)
    {
        if (parameters.Count == 0) return null;
        return parameters.ToDictionary(p => p.Name, ToParameterDto);
    }

    private static YamlParameterDto ToParameterDto(Parameter p) => new()
    {
        NumericId         = p.NumericIdStr,
        Mnemonic          = NullIfEmpty(p.Mnemonic),
        Kind              = p.Kind.ToString(),
        DataType          = p.DataType?.Name,
        ShortDescription  = NullIfEmpty(p.ShortDescription),
        LongDescription   = NullIfEmpty(p.LongDescription),
        Formula           = NullIfEmpty(p.Formula),
        HexValue          = NullIfEmpty(p.HexValue),
        Memory            = p.Memory?.Name,
        MemoryOffset      = p.Memory is not null ? p.MemoryOffsetStr : null,
        ValidityParameter = p.ValidityParameter?.Name,
        AlarmLow          = p.AlarmLow,
        AlarmHigh         = p.AlarmHigh,
    };

    private static Dictionary<string, YamlHeaderTypeDto>? ToHeaderTypesDict(List<HeaderType> headerTypes)
    {
        if (headerTypes.Count == 0) return null;
        return headerTypes.ToDictionary(ht => ht.Name, ht => new YamlHeaderTypeDto
        {
            Mnemonic    = NullIfEmpty(ht.Mnemonic),
            Description = NullIfEmpty(ht.Description),
            Ids         = ht.Ids.Select(i => new YamlHeaderTypeIdDto
            {
                Name        = i.Name,
                Description = NullIfEmpty(i.Description),
                DataType    = i.DataType?.Name,
            }).ToList(),
        });
    }

    private static Dictionary<string, YamlPacketTypeDto>? ToPacketTypesDict(List<PacketType> packetTypes)
    {
        if (packetTypes.Count == 0) return null;
        return packetTypes.ToDictionary(pt => pt.Name, ToPacketTypeDto);
    }

    private static YamlPacketTypeDto ToPacketTypeDto(PacketType pt)
    {
        Dictionary<string, string>? headerIdValues = null;
        if (pt.HeaderType is not null && pt.HeaderIdValues.Count > 0)
        {
            var idNames = pt.HeaderType.Ids.ToDictionary(i => i.Id, i => i.Name);
            headerIdValues = pt.HeaderIdValues
                .Where(hiv => idNames.ContainsKey(hiv.IdRef))
                .ToDictionary(hiv => idNames[hiv.IdRef], hiv => hiv.Value);
            if (headerIdValues.Count == 0) headerIdValues = null;
        }

        return new YamlPacketTypeDto
        {
            Kind           = pt.Kind.ToString(),
            NumericId      = pt.NumericIdStr,
            Mnemonic       = NullIfEmpty(pt.Mnemonic),
            Description    = NullIfEmpty(pt.Description),
            HeaderType     = pt.HeaderType?.Name,
            HeaderIdValues = headerIdValues,
            Fields         = pt.Fields.Count > 0
                ? pt.Fields.Select(f => new YamlPacketFieldDto
                {
                    Name            = f.Name,
                    Description     = NullIfEmpty(f.Description),
                    Parameter       = f.Parameter?.Name,
                    IsTypeIndicator = f.IsTypeIndicator ? true : null,
                    IndicatorValue  = NullIfEmpty(f.IndicatorValue),
                }).ToList()
                : null,
        };
    }

    private static Dictionary<string, YamlMemoryDto>? ToMemoriesDict(List<Memory> memories)
    {
        if (memories.Count == 0) return null;
        return memories.ToDictionary(m => m.Name, m => new YamlMemoryDto
        {
            NumericId   = m.NumericIdStr,
            Mnemonic    = NullIfEmpty(m.Mnemonic),
            Size        = m.SizeStr,
            Address     = NullIfEmpty(m.Address),
            Description = NullIfEmpty(m.Description),
            Alignment   = m.AlignmentStr != "1" ? m.AlignmentStr : null,
            IsWritable  = m.IsWritable,
            IsReadable  = m.IsReadable,
        });
    }

    private static YamlRangeDto RangeDto(NumericRange r) => new() { Min = r.Min, Max = r.Max };

    // ── Import: YAML file(s) → YamlDocument ──────────────────────────────────

    /// <summary>
    /// Recursively loads <paramref name="filePath"/> and all its transitive includes,
    /// merging them into a single <see cref="YamlDocument"/>.
    /// </summary>
    /// <param name="processing">Canonical paths currently on the call stack (circular include detection).</param>
    /// <param name="visited">Canonical paths already fully processed (de-duplication).</param>
    private YamlDocument LoadWithIncludes(
        string filePath, HashSet<string> processing, HashSet<string> visited)
    {
        var canonical = Path.GetFullPath(filePath);

        if (processing.Contains(canonical))
            throw new InvalidDataException(
                $"Circular include detected: '{canonical}' is already being processed. " +
                "Check your include directives.");

        if (visited.Contains(canonical))
            return EmptyDocument();

        processing.Add(canonical);

        var text = File.ReadAllText(canonical);
        var doc  = _deserializer.Deserialize<YamlDocument>(text) ?? EmptyDocument();
        var dir  = Path.GetDirectoryName(canonical)!;

        var merged = EmptyDocument();
        foreach (var include in doc.Includes ?? [])
        {
            var includePath  = Path.GetFullPath(Path.Combine(dir, include));
            var includedDoc  = LoadWithIncludes(includePath, processing, visited);
            Merge(merged, includedDoc);
        }

        Merge(merged, doc);

        processing.Remove(canonical);
        visited.Add(canonical);

        return merged;
    }

    private static void Merge(YamlDocument target, YamlDocument source)
    {
        // Propagate the highest version seen across all files
        if (source.Version > target.Version)
            target.Version = source.Version;

        if (source.Metadata is not null)
            target.Metadata = source.Metadata;

        target.DataTypes   = MergeDict(target.DataTypes,   source.DataTypes);
        target.Parameters  = MergeDict(target.Parameters,  source.Parameters);
        target.HeaderTypes = MergeDict(target.HeaderTypes, source.HeaderTypes);
        target.PacketTypes = MergeDict(target.PacketTypes, source.PacketTypes);
        target.Memories    = MergeDict(target.Memories,    source.Memories);
    }

    private static Dictionary<string, T>? MergeDict<T>(Dictionary<string, T>? target, Dictionary<string, T>? source)
    {
        if (source is null) return target;
        target ??= new Dictionary<string, T>();
        foreach (var (k, v) in source)
            target[k] = v;
        return target;
    }

    private static YamlDocument EmptyDocument() => new() { Version = CurrentVersion };

    // ── Import: YamlDocument → DataModel ─────────────────────────────────────

    private static DataModel ToModel(YamlDocument doc)
    {
        var model = new DataModel();

        // Metadata
        if (doc.Metadata is { } meta)
        {
            model.Metadata.Name        = meta.Name        ?? string.Empty;
            model.Metadata.Version     = meta.Version     ?? string.Empty;
            model.Metadata.Date        = meta.Date        ?? string.Empty;
            model.Metadata.Status      = meta.Status      ?? string.Empty;
            model.Metadata.Description = meta.Description ?? string.Empty;
            model.Metadata.Fields      = (meta.Fields ?? [])
                .Select(f => new MetadataField { Name = f.Name, Value = f.Value })
                .ToList();
        }

        // Data Types — pass 1: create entities (no cross-references yet)
        var typeByName = new Dictionary<string, DataType>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, dto) in doc.DataTypes ?? [])
        {
            var dt = CreateDataType(name, dto);
            typeByName[name] = dt;
            model.DataTypes.Add(dt);
        }

        // Data Types — pass 2: resolve struct fields and array element references
        foreach (var (name, dto) in doc.DataTypes ?? [])
        {
            var dt = typeByName[name];
            if (dt is StructureType st)
                foreach (var fd in dto.Fields ?? [])
                    st.Fields.Add(new StructureField
                    {
                        Name     = fd.Name,
                        DataType = Resolve(fd.DataType, typeByName),
                    });
            if (dt is ArrayType at)
                at.ElementType = Resolve(dto.ElementType, typeByName);
        }

        // Memories
        var memByName = new Dictionary<string, Memory>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, dto) in doc.Memories ?? [])
        {
            var m = CreateMemory(name, dto);
            memByName[name] = m;
            model.Memories.Add(m);
        }

        // Parameters — pass 1: create entities
        var paramByName = new Dictionary<string, Parameter>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, dto) in doc.Parameters ?? [])
        {
            var p = CreateParameter(name, dto, typeByName, memByName);
            paramByName[name] = p;
            model.Parameters.Add(p);
        }

        // Parameters — pass 2: validity parameter (self-referential)
        foreach (var (name, dto) in doc.Parameters ?? [])
            if (dto.ValidityParameter is not null)
                paramByName[name].ValidityParameter = Resolve(dto.ValidityParameter, paramByName);

        // Header Types
        var htByName = new Dictionary<string, HeaderType>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, dto) in doc.HeaderTypes ?? [])
        {
            var ht = CreateHeaderType(name, dto, typeByName);
            htByName[name] = ht;
            model.HeaderTypes.Add(ht);
        }

        // Packet Types
        foreach (var (name, dto) in doc.PacketTypes ?? [])
            model.PacketTypes.Add(CreatePacketType(name, dto, paramByName, htByName));

        return model;
    }

    // ── Entity factories ──────────────────────────────────────────────────────

    private static DataType CreateDataType(string name, YamlDataTypeDto dto) =>
        dto.Type.ToLowerInvariant() switch
        {
            "sint"      => new SignedIntegerType   { Name = name, Scalar = ParseScalar(dto), Numeric = ParseNumeric(dto) },
            "uint"      => new UnsignedIntegerType { Name = name, Scalar = ParseScalar(dto), Numeric = ParseNumeric(dto) },
            "float"     => new FloatType           { Name = name, Scalar = ParseScalar(dto), Numeric = ParseNumeric(dto) },
            "bool"      => new BooleanType         { Name = name, Scalar = ParseScalar(dto) },
            "bitstring" => new BitStringType       { Name = name, Scalar = ParseScalar(dto) },
            "enum"      => CreateEnumType(name, dto),
            "struct"    => new StructureType { Name = name },  // fields filled in pass 2
            "array"     => CreateArrayType(name, dto),
            var unknown => throw new InvalidDataException(
                $"Unknown data type discriminator '{unknown}' for type '{name}'."),
        };

    private static ScalarProperties ParseScalar(YamlDataTypeDto dto) => new()
    {
        Endianness = ParseEndianness(dto.Endianness),
        BitSizeStr = dto.BitSize ?? "32",
    };

    private static NumericProperties? ParseNumeric(YamlDataTypeDto dto)
    {
        if (dto.Range is null && dto.Unit is null && dto.Calibration is null) return null;
        return new NumericProperties
        {
            Range              = dto.Range is null ? new NumericRange() : new NumericRange { Min = dto.Range.Min, Max = dto.Range.Max },
            Unit               = dto.Unit,
            CalibrationFormula = dto.Calibration,
        };
    }

    private static EnumeratedType CreateEnumType(string name, YamlDataTypeDto dto)
    {
        var et = new EnumeratedType
        {
            Name       = name,
            Endianness = ParseEndianness(dto.Endianness),
            BitSizeStr = dto.BitSize ?? "32",
        };
        foreach (var v in dto.Values ?? [])
        {
            var ev = new EnumeratedValue { Name = v.Name };
            if (!string.IsNullOrEmpty(v.RawValues))
                ev.RawValuesText = v.RawValues;
            et.Values.Add(ev);
        }
        return et;
    }

    private static ArrayType CreateArrayType(string name, YamlDataTypeDto dto)
    {
        var at = new ArrayType { Name = name };
        if (dto.ArraySize is { } sz)
            at.ArraySize = new ArraySizeDescriptor
            {
                Endianness = ParseEndianness(sz.Endianness),
                BitSizeStr = sz.BitSize ?? "32",
                Range      = sz.Range is null ? new NumericRange() : new NumericRange { Min = sz.Range.Min, Max = sz.Range.Max },
            };
        return at;
    }

    private static Memory CreateMemory(string name, YamlMemoryDto dto) => new()
    {
        Name         = name,
        NumericIdStr = dto.NumericId,
        Mnemonic     = dto.Mnemonic,
        SizeStr      = dto.Size,
        Address      = dto.Address,
        Description  = dto.Description,
        AlignmentStr = dto.Alignment ?? "1",
        IsWritable   = dto.IsWritable,
        IsReadable   = dto.IsReadable,
    };

    private static Parameter CreateParameter(
        string name, YamlParameterDto dto,
        Dictionary<string, DataType> typeByName,
        Dictionary<string, Memory> memByName) => new()
    {
        Name             = name,
        NumericIdStr     = dto.NumericId,
        Mnemonic         = dto.Mnemonic,
        Kind             = Enum.Parse<ParameterKind>(dto.Kind, ignoreCase: true),
        DataType         = Resolve(dto.DataType, typeByName),
        ShortDescription = dto.ShortDescription,
        LongDescription  = dto.LongDescription,
        Formula          = dto.Formula,
        HexValue         = dto.HexValue,
        Memory           = Resolve(dto.Memory, memByName),
        MemoryOffsetStr  = dto.MemoryOffset ?? "0",
        AlarmLow         = dto.AlarmLow,
        AlarmHigh        = dto.AlarmHigh,
    };

    private static HeaderType CreateHeaderType(
        string name, YamlHeaderTypeDto dto, Dictionary<string, DataType> typeByName)
    {
        var ht = new HeaderType
        {
            Name        = name,
            Mnemonic    = dto.Mnemonic,
            Description = dto.Description ?? string.Empty,
        };
        foreach (var id in dto.Ids ?? [])
            ht.Ids.Add(new HeaderTypeId
            {
                Name        = id.Name,
                Description = id.Description,
                DataType    = Resolve(id.DataType, typeByName),
            });
        return ht;
    }

    private static PacketType CreatePacketType(
        string name, YamlPacketTypeDto dto,
        Dictionary<string, Parameter> paramByName,
        Dictionary<string, HeaderType> htByName)
    {
        var ht = Resolve(dto.HeaderType, htByName);
        var pt = new PacketType
        {
            Name        = name,
            Kind        = Enum.Parse<PacketTypeKind>(dto.Kind, ignoreCase: true),
            NumericIdStr = dto.NumericId,
            Mnemonic    = dto.Mnemonic,
            Description = dto.Description,
            HeaderType  = ht,
        };

        if (ht is not null && dto.HeaderIdValues is not null)
        {
            var idsByName = ht.Ids.ToDictionary(i => i.Name, i => i.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var (idName, hexVal) in dto.HeaderIdValues)
                if (idsByName.TryGetValue(idName, out var id))
                    pt.HeaderIdValues.Add(new HeaderIdValue { IdRef = id, Value = hexVal });
        }

        foreach (var fd in dto.Fields ?? [])
            pt.Fields.Add(new PacketField
            {
                Name            = fd.Name,
                Description     = fd.Description,
                Parameter       = Resolve(fd.Parameter, paramByName),
                IsTypeIndicator = fd.IsTypeIndicator ?? false,
                IndicatorValue  = fd.IndicatorValue,
            });

        return pt;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static T? Resolve<T>(string? name, Dictionary<string, T> map) where T : class
        => name is not null && map.TryGetValue(name, out var v) ? v : null;

    private static Endianness ParseEndianness(string? s) =>
        s is null ? Endianness.LittleEndian : Enum.Parse<Endianness>(s, ignoreCase: true);

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrEmpty(s) ? null : s;
}
