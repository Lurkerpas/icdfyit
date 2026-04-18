namespace IcdFyIt.Core.Services;

/// <summary>
/// Represents a reversible operation on the data model (ICD-FUN-50).
/// </summary>
public interface IUndoableCommand
{
    /// <summary>Applies the operation.</summary>
    void Execute();

    /// <summary>Reverses the operation, restoring any affected references (ICD-FUN-53).</summary>
    void Undo();
}
