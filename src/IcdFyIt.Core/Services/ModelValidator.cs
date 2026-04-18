using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Services;

/// <summary>
/// Validates a <see cref="DataModel"/> and returns a list of issues (ICD-FUN-60 to ICD-FUN-62).
/// </summary>
public class ModelValidator
{
    /// <summary>
    /// Validates the model and returns all detected issues.
    /// Returns an empty list when the model is valid.
    /// </summary>
    public IReadOnlyList<ValidationIssue> Validate(DataModel model) =>
        throw new NotImplementedException();
}
