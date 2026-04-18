namespace IcdFyIt.Core.Services;

/// <summary>
/// Maintains undo/redo stacks bounded by <see cref="MaxDepth"/> (ICD-FUN-50 to ICD-FUN-53).
/// </summary>
public class UndoRedoManager
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    /// <summary>Maximum number of undoable commands retained (ICD-FUN-51). Default: 64.</summary>
    public int MaxDepth { get; set; } = 64;

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Executes a command and pushes it onto the undo stack.</summary>
    public void Push(IUndoableCommand command) => throw new NotImplementedException();

    /// <summary>Undoes the most recent command.</summary>
    public void Undo() => throw new NotImplementedException();

    /// <summary>Re-executes the most recently undone command.</summary>
    public void Redo() => throw new NotImplementedException();

    /// <summary>Clears both stacks (called after New/Open).</summary>
    public void Clear() => throw new NotImplementedException();
}
