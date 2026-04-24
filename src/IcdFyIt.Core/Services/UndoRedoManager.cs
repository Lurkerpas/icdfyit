namespace IcdFyIt.Core.Services;

/// <summary>
/// Maintains undo/redo stacks bounded by <see cref="MaxDepth"/> (ICD-FUN-50 to ICD-FUN-53).
/// Uses <see cref="LinkedList{T}"/> for the undo stack so oldest entries can be dropped
/// from the front when the depth limit is reached.
/// </summary>
public class UndoRedoManager
{
    private readonly LinkedList<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private int _maxDepth = 64;

    /// <summary>Maximum number of undoable commands retained (ICD-FUN-51). Default: 64.</summary>
    public int MaxDepth
    {
        get => _maxDepth;
        set => _maxDepth = Math.Max(1, value);
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Executes a command and pushes it onto the undo stack.</summary>
    public void Push(IUndoableCommand command)
    {
        command.Execute();
        _undoStack.AddLast(command);
        _redoStack.Clear();
        while (_undoStack.Count > MaxDepth)
            _undoStack.RemoveFirst();
    }

    /// <summary>Undoes the most recent command.</summary>
    public void Undo()
    {
        if (!CanUndo) return;
        var cmd = _undoStack.Last!.Value;
        _undoStack.RemoveLast();
        cmd.Undo();
        _redoStack.Push(cmd);
    }

    /// <summary>Re-executes the most recently undone command.</summary>
    public void Redo()
    {
        if (!CanRedo) return;
        var cmd = _redoStack.Pop();
        cmd.Execute();
        _undoStack.AddLast(cmd);
    }

    /// <summary>Clears both stacks (called after New/Open).</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
