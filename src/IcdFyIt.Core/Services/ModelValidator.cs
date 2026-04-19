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
        CheckNullParameterDataTypes(model, issues);
        CheckNullPacketFieldParameters(model, issues);
        CheckTypeIndicatorKinds(model, issues);
        CheckCircularDataTypeRefs(model, issues);

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
}
