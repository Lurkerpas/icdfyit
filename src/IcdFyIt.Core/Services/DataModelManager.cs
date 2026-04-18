using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Services;

/// <summary>
/// Orchestrates the data model lifecycle: New, Open, Save, and all entity CRUD operations.
/// Internally coordinates <see cref="ChangeNotifier"/>, <see cref="DirtyTracker"/>,
/// and <see cref="UndoRedoManager"/> (ICD-DES §4.1).
/// </summary>
public class DataModelManager
{
    private readonly ChangeNotifier _changeNotifier;
    private readonly DirtyTracker _dirtyTracker;
    private readonly UndoRedoManager _undoRedoManager;

    public DataModelManager(
        ChangeNotifier changeNotifier,
        DirtyTracker dirtyTracker,
        UndoRedoManager undoRedoManager)
    {
        _changeNotifier = changeNotifier;
        _dirtyTracker = dirtyTracker;
        _undoRedoManager = undoRedoManager;
    }

    /// <summary>The current file path, or null when the document has never been saved.</summary>
    public string? CurrentFilePath { get; private set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <summary>Discards the current model and starts a new empty one (ICD-FUN-10).</summary>
    public void New() => throw new NotImplementedException();

    /// <summary>Loads a model from the specified file path (ICD-FUN-11).</summary>
    public void Open(string filePath) => throw new NotImplementedException();

    /// <summary>Saves the model to the specified file path (ICD-FUN-12).</summary>
    public void Save(string filePath) => throw new NotImplementedException();

    // ── DataType CRUD ──────────────────────────────────────────────────────────

    /// <summary>Creates a new DataType and adds it to the model (ICD-FUN-20).</summary>
    public DataType AddDataType(string name) => throw new NotImplementedException();

    /// <summary>Removes the specified DataType; nullifies references in Parameters and PacketFields (ICD-FUN-40).</summary>
    public void RemoveDataType(DataType dataType) => throw new NotImplementedException();

    // ── Parameter CRUD ─────────────────────────────────────────────────────────

    /// <summary>Creates a new Parameter and adds it to the model (ICD-FUN-30).</summary>
    public Parameter AddParameter(string name) => throw new NotImplementedException();

    /// <summary>Removes the specified Parameter; nullifies references in PacketFields (ICD-FUN-40).</summary>
    public void RemoveParameter(Parameter parameter) => throw new NotImplementedException();

    // ── PacketType CRUD ────────────────────────────────────────────────────────

    /// <summary>Creates a new PacketType and adds it to the model (ICD-IF-61).</summary>
    public PacketType AddPacketType(string name) => throw new NotImplementedException();

    /// <summary>Removes the specified PacketType from the model.</summary>
    public void RemovePacketType(PacketType packetType) => throw new NotImplementedException();

    // ── Undo / Redo ────────────────────────────────────────────────────────────

    public bool CanUndo => _undoRedoManager.CanUndo;
    public bool CanRedo => _undoRedoManager.CanRedo;

    public void Undo() => _undoRedoManager.Undo();
    public void Redo() => _undoRedoManager.Redo();
}
