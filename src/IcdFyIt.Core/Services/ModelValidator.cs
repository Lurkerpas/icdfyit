using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Services;

/// <summary>
/// Validates a <see cref="DataModel"/> and returns a list of issues (ICD-FUN-52).
/// </summary>
public class ModelValidator
{
    /// <summary>
    /// Validates the model and returns all detected issues.
    /// Returns an empty list when the model is valid.
    /// </summary>
    public IReadOnlyList<ValidationIssue> Validate(DataModel model)
    {
        var issues = new List<ValidationIssue>();

        CheckDuplicateDataTypeNames(model, issues);
        CheckDuplicateParameterNames(model, issues);
        CheckDuplicateParameterIds(model, issues);
        CheckDuplicatePacketTypeNames(model, issues);
        CheckDuplicatePacketTypeIds(model, issues);
        CheckNullParameterDataTypes(model, issues);
        CheckNullPacketFieldParameters(model, issues);
        CheckTypeIndicatorKinds(model, issues);
        CheckCircularDataTypeRefs(model, issues);
        CheckDuplicateHeaderTypeNames(model, issues);
        CheckHeaderTypeDescriptions(model, issues);
        CheckHeaderTypeIdNullDataTypes(model, issues);
        CheckHeaderTypeIdDataTypeKind(model, issues);
        CheckMissingHeaderIdValues(model, issues);
        CheckTypeIndicatorValues(model, issues);

        return issues;
    }

    private static void CheckDuplicateDataTypeNames(DataModel model, List<ValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dt in model.DataTypes)
            if (!seen.Add(dt.Name))
                issues.Add(new ValidationIssue($"Duplicate data type name: \"{dt.Name}\""));
    }

    private static void CheckDuplicateParameterNames(DataModel model, List<ValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in model.Parameters)
            if (!seen.Add(p.Name))
                issues.Add(new ValidationIssue($"Duplicate parameter name: \"{p.Name}\""));
    }

    private static void CheckDuplicateParameterIds(DataModel model, List<ValidationIssue> issues)
    {
        var seen = new Dictionary<int, string>();
        foreach (var p in model.Parameters)
        {
            if (seen.TryGetValue(p.NumericId, out var other))
                issues.Add(new ValidationIssue(
                    $"Duplicate parameter numeric ID {p.NumericId}: \"{p.Name}\" and \"{other}\""));
            else
                seen[p.NumericId] = p.Name;
        }
    }

    private static void CheckDuplicatePacketTypeNames(DataModel model, List<ValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pt in model.PacketTypes)
            if (!seen.Add(pt.Name))
                issues.Add(new ValidationIssue($"Duplicate packet type name: \"{pt.Name}\""));
    }

    private static void CheckDuplicatePacketTypeIds(DataModel model, List<ValidationIssue> issues)
    {
        var seen = new Dictionary<int, string>();
        foreach (var pt in model.PacketTypes)
        {
            if (seen.TryGetValue(pt.NumericId, out var other))
                issues.Add(new ValidationIssue(
                    $"Duplicate packet type numeric ID {pt.NumericId}: \"{pt.Name}\" and \"{other}\""));
            else
                seen[pt.NumericId] = pt.Name;
        }
    }

    private static void CheckNullParameterDataTypes(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var p in model.Parameters.Where(p => p.DataType is null))
            issues.Add(new ValidationIssue($"Parameter \"{p.Name}\" has no data type assigned"));
    }

    private static void CheckNullPacketFieldParameters(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var pt in model.PacketTypes)
            foreach (var f in pt.Fields.Where(f => f.Parameter is null))
                issues.Add(new ValidationIssue(
                    $"Packet field \"{f.Name}\" in \"{pt.Name}\" has no parameter assigned"));
    }

    private static void CheckTypeIndicatorKinds(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var pt in model.PacketTypes)
            foreach (var f in pt.Fields.Where(f => f.IsTypeIndicator && f.Parameter?.Kind != ParameterKind.Id))
                issues.Add(new ValidationIssue(
                    $"Type indicator field \"{f.Name}\" in \"{pt.Name}\" must reference a parameter of Kind ID (ICD-DAT-461)"));
    }

    private static void CheckTypeIndicatorValues(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var pt in model.PacketTypes)
            foreach (var f in pt.Fields.Where(f => f.IsTypeIndicator && string.IsNullOrEmpty(f.IndicatorValue)))
                issues.Add(new ValidationIssue(
                    $"Type indicator field \"{f.Name}\" in \"{pt.Name}\" has no indicator value defined (ICD-DAT-462)"));
    }

    private static void CheckCircularDataTypeRefs(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var dt in model.DataTypes)
            if (HasCircularRef(dt, dt, new HashSet<Guid>()))
                issues.Add(new ValidationIssue(
                    $"Circular data type reference detected involving \"{dt.Name}\" (ICD-FUN-42)"));
    }

    private static bool HasCircularRef(DataType origin, DataType current, HashSet<Guid> visited)
    {
        if (!visited.Add(current.Id)) return false;

        IEnumerable<DataType?> children = current switch
        {
            StructureType st => st.Fields.Select(f => f.DataType),
            ArrayType     at => [at.ElementType],
            _               => []
        };

        foreach (var child in children)
        {
            if (child is null) continue;
            if (child.Id == origin.Id) return true;
            if (HasCircularRef(origin, child, visited)) return true;
        }
        return false;
    }

    private static void CheckDuplicateHeaderTypeNames(DataModel model, List<ValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ht in model.HeaderTypes)
            if (!seen.Add(ht.Name))
                issues.Add(new ValidationIssue($"Duplicate header type name: \"{ht.Name}\""));
    }

    private static void CheckHeaderTypeDescriptions(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var ht in model.HeaderTypes.Where(ht => string.IsNullOrEmpty(ht.Description)))
            issues.Add(new ValidationIssue($"Header type \"{ht.Name}\" has no description (ICD-DAT-720)"));
    }

    private static void CheckHeaderTypeIdNullDataTypes(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var ht in model.HeaderTypes)
            foreach (var id in ht.Ids.Where(id => id.DataType is null))
                issues.Add(new ValidationIssue(
                    $"Header type \"{ht.Name}\" ID entry \"{id.Name}\" has no data type assigned"));
    }

    private static void CheckHeaderTypeIdDataTypeKind(DataModel model, List<ValidationIssue> issues)
    {
        // A data type referenced by a Header Type ID entry must be used by at least one
        // Parameter of Kind ID — parameters of Kind ID represent identification fields
        // whose data type is appropriate for use as a header discriminator (ICD-DAT-730).
        var idKindDataTypeIds = model.Parameters
            .Where(p => p.Kind == ParameterKind.Id && p.DataType is not null)
            .Select(p => p.DataType!.Id)
            .ToHashSet();

        foreach (var ht in model.HeaderTypes)
            foreach (var htId in ht.Ids.Where(id => id.DataType is not null))
                if (!idKindDataTypeIds.Contains(htId.DataType!.Id))
                    issues.Add(new ValidationIssue(
                        $"Header type \"{ht.Name}\" ID entry \"{htId.Name}\" references data type \"{htId.DataType!.Name}\" " +
                        $"which is not used by any parameter of Kind ID (ICD-DAT-730)"));
    }

    private static void CheckMissingHeaderIdValues(DataModel model, List<ValidationIssue> issues)
    {
        foreach (var pt in model.PacketTypes)
        {
            if (pt.HeaderType is null) continue;
            var definedIdRefs = pt.HeaderIdValues.Select(v => v.IdRef).ToHashSet();
            foreach (var htId in pt.HeaderType.Ids)
                if (!definedIdRefs.Contains(htId.Id) ||
                    string.IsNullOrEmpty(pt.HeaderIdValues.First(v => v.IdRef == htId.Id).Value))
                    issues.Add(new ValidationIssue(
                        $"Packet type \"{pt.Name}\" has no fixed value defined for header ID \"{htId.Name}\" (ICD-DAT-414)"));
        }
    }
}
