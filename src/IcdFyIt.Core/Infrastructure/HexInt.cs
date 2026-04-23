using System.Globalization;

namespace IcdFyIt.Core.Infrastructure;

/// <summary>
/// Helper for parsing integers that may be expressed in hexadecimal ("0x…") or decimal notation.
/// Hex format is preserved round-trip through the dual-property pattern used in model classes:
/// the <c>int</c> property is used by internal logic (comparisons, arithmetic) and the companion
/// <c>string</c> property is used for XML serialization and UI binding, preserving the original
/// notation as entered (ICD-DAT-800).
/// </summary>
public static class HexInt
{
    /// <summary>
    /// Parses <paramref name="s"/> as a decimal or hexadecimal integer.
    /// Accepts "0x" or "0X" prefix for hexadecimal. Returns 0 on null, empty, or invalid input.
    /// </summary>
    public static int Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(s[2..], NumberStyles.HexNumber, null, out var hex) ? hex : 0;
        return int.TryParse(s, out var dec) ? dec : 0;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="s"/> can be parsed as a decimal or hexadecimal integer
    /// and sets <paramref name="value"/> to the parsed result.
    /// </summary>
    public static bool TryParse(string? s, out int value)
    {
        if (string.IsNullOrWhiteSpace(s)) { value = 0; return false; }
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(s[2..], NumberStyles.HexNumber, null, out value);
        return int.TryParse(s, out value);
    }
}
