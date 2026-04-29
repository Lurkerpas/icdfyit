namespace IcdFyIt.Core.Persistence.Yaml;

// ── Root document (ICD-IF-410 to ICD-IF-460) ─────────────────────────────────

/// <summary>
/// Root YAML document.  Top-level sections map directly to Data Model entity groups.
/// The <c>includes</c> list drives multi-file composition (ICD-IF-420).
/// </summary>
public class YamlDocument
{
    public int Version { get; set; } = YamlPersistence.CurrentVersion;
    public List<string>? Includes { get; set; }
    public YamlMetadataDto? Metadata { get; set; }
    public Dictionary<string, YamlDataTypeDto>? DataTypes { get; set; }
    public Dictionary<string, YamlParameterDto>? Parameters { get; set; }
    public Dictionary<string, YamlHeaderTypeDto>? HeaderTypes { get; set; }
    public Dictionary<string, YamlPacketTypeDto>? PacketTypes { get; set; }
    public Dictionary<string, YamlMemoryDto>? Memories { get; set; }
}

// ── Metadata ──────────────────────────────────────────────────────────────────

public class YamlMetadataDto
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Date { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public List<YamlMetadataFieldDto>? Fields { get; set; }
}

public class YamlMetadataFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

// ── Data Types ────────────────────────────────────────────────────────────────

/// <summary>
/// Flat DTO for all Data Type kinds.  The <c>type</c> discriminator determines
/// which properties are meaningful (ICD-IF-440).
/// Values: "sint" | "uint" | "float" | "bool" | "bitstring" | "enum" | "struct" | "array".
/// </summary>
public class YamlDataTypeDto
{
    public string Type { get; set; } = string.Empty;

    // Scalar / enum
    public string? Endianness { get; set; }
    public string? BitSize { get; set; }   // string-only (ICD-IF-450)

    // Numeric
    public YamlRangeDto? Range { get; set; }
    public string? Unit { get; set; }
    public string? Calibration { get; set; }

    // Enum
    public List<YamlEnumValueDto>? Values { get; set; }

    // Struct
    public List<YamlStructFieldDto>? Fields { get; set; }

    // Array
    public string? ElementType { get; set; }  // name ref (ICD-IF-441)
    public YamlArraySizeDto? ArraySize { get; set; }
}

public class YamlRangeDto
{
    public double Min { get; set; }
    public double Max { get; set; }
}

public class YamlEnumValueDto
{
    public string Name { get; set; } = string.Empty;
    public string? RawValues { get; set; }  // comma-separated; hex preserved (ICD-IF-450)
}

public class YamlStructFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string? DataType { get; set; }  // name ref (ICD-IF-441)
}

public class YamlArraySizeDto
{
    public string? Endianness { get; set; }
    public string? BitSize { get; set; }   // string-only (ICD-IF-450)
    public YamlRangeDto? Range { get; set; }
}

// ── Parameters ────────────────────────────────────────────────────────────────

public class YamlParameterDto
{
    public string NumericId { get; set; } = "0";   // string-only (ICD-IF-450)
    public string? Mnemonic { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? DataType { get; set; }           // name ref (ICD-IF-441)
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public string? Formula { get; set; }
    public string? HexValue { get; set; }
    public string? Memory { get; set; }             // name ref (ICD-IF-442)
    public string? MemoryOffset { get; set; }       // string-only (ICD-IF-450)
    public string? ValidityParameter { get; set; }  // name ref (ICD-IF-443)
    public double? AlarmLow { get; set; }
    public double? AlarmHigh { get; set; }
}

// ── Header Types ──────────────────────────────────────────────────────────────

public class YamlHeaderTypeDto
{
    public string? Mnemonic { get; set; }
    public string? Description { get; set; }
    public List<YamlHeaderTypeIdDto>? Ids { get; set; }
}

public class YamlHeaderTypeIdDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DataType { get; set; }  // name ref (ICD-IF-445)
}

// ── Packet Types ──────────────────────────────────────────────────────────────

public class YamlPacketTypeDto
{
    public string Kind { get; set; } = string.Empty;  // "Telecommand" | "Telemetry"
    public string NumericId { get; set; } = "0";      // string-only (ICD-IF-450)
    public string? Mnemonic { get; set; }
    public string? Description { get; set; }
    public string? HeaderType { get; set; }                          // name ref (ICD-IF-444)
    public Dictionary<string, string>? HeaderIdValues { get; set; } // HeaderTypeId.Name → hex
    public List<YamlPacketFieldDto>? Fields { get; set; }
}

public class YamlPacketFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Parameter { get; set; }   // name ref (ICD-IF-441)
    public bool? IsTypeIndicator { get; set; }
    public string? IndicatorValue { get; set; }
}

// ── Memories ──────────────────────────────────────────────────────────────────

public class YamlMemoryDto
{
    public string NumericId { get; set; } = "0";  // string-only (ICD-IF-450)
    public string? Mnemonic { get; set; }
    public string Size { get; set; } = "0";       // string-only (ICD-IF-450)
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string? Alignment { get; set; }         // omitted when "1"
    public bool IsWritable { get; set; }
    public bool IsReadable { get; set; }
}
