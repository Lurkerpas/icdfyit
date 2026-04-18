namespace IcdFyIt.Core.Services;

/// <summary>
/// Describes a single validation finding returned by <see cref="ModelValidator"/>.
/// </summary>
public sealed class ValidationIssue
{
    public ValidationIssue(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
