using IcdFyIt.Core.Model;
using IcdFyIt.Core.Persistence;

namespace IcdFyIt.Core.Services;

/// <summary>
/// Orchestrates the data model lifecycle: New, Open, Save, and all entity CRUD operations.
/// Internally coordinates <see cref="ChangeNotifier"/>, <see cref="DirtyTracker"/>,
/// and <see cref="UndoRedoManager"/> (ICD-DES §4.1).
/// Every mutating operation is wrapped in an <see cref="IUndoableCommand"/> pushed via
/// <see cref="UndoRedoManager.Push"/> so undo/redo works out of the box.
/// </summary>
public class DataModelManager
{
    private readonly ChangeNotifier _changeNotifier;
    private readonly DirtyTracker _dirtyTracker;
    private readonly UndoRedoManager _undoRedoManager;
    private readonly XmlPersistence _persistence = new();
    private DataModel _model = new();

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

    /// <summary>Read-only view of the live data model (used by validation).</summary>
    public DataModel CurrentModel => _model;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <summary>Discards the current model and starts a new empty one (ICD-FUN-10).</summary>
    public void New()
    {
        _model = new DataModel();
        CurrentFilePath = null;
        _changeNotifier.ReloadFrom(_model);
        _dirtyTracker.MarkClean();
        _undoRedoManager.Clear();
    }

    /// <summary>Loads a model from the specified file path (ICD-FUN-11).</summary>
    public void Open(string filePath)
    {
        _model = _persistence.Load(filePath);
        CurrentFilePath = filePath;
        _changeNotifier.ReloadFrom(_model);
        _dirtyTracker.MarkClean();
        _undoRedoManager.Clear();
    }

    /// <summary>Saves the model to the current or a new file path (ICD-FUN-12).</summary>
    public void Save(string filePath)
    {
        // ChangeNotifier collections are the source of truth for ordering/membership.
        _model.DataTypes.Clear();
        _model.DataTypes.AddRange(_changeNotifier.DataTypes);
        _model.Parameters.Clear();
        _model.Parameters.AddRange(_changeNotifier.Parameters);
        _model.PacketTypes.Clear();
        _model.PacketTypes.AddRange(_changeNotifier.PacketTypes);

        _model.HeaderTypes.Clear();
        _model.HeaderTypes.AddRange(_changeNotifier.HeaderTypes);

        _persistence.Save(_model, filePath);
        CurrentFilePath = filePath;
        _dirtyTracker.MarkClean();
    }

    // ── DataType CRUD ──────────────────────────────────────────────────────────

    /// <summary>Creates a new DataType of the given kind and adds it to the model (ICD-FUN-20).</summary>
    public DataType AddDataType(string name, BaseType kind)
    {
        var dt = CreateDataType(name, kind);
        _undoRedoManager.Push(new AddEntityCommand<DataType>(
            dt, _model.DataTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return dt;
    }

    /// <summary>Removes a DataType; nullifies all references to it (ICD-FUN-40).</summary>
    public void RemoveDataType(DataType dataType)
        => _undoRedoManager.Push(new RemoveDataTypeCommand(dataType, _model, _changeNotifier, _dirtyTracker));

    /// <summary>Creates a shallow copy of a DataType with a new GUID and modified name.</summary>
    public DataType DuplicateDataType(DataType source)
    {
        var copy = CreateDataType($"Copy of {source.Name}", source.Kind);
        _undoRedoManager.Push(new AddEntityCommand<DataType>(
            copy, _model.DataTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return copy;
    }

    // ── Parameter CRUD ─────────────────────────────────────────────────────────

    public Parameter AddParameter(string name)
    {
        var p = new Parameter { Name = name };
        _undoRedoManager.Push(new AddEntityCommand<Parameter>(
            p, _model.Parameters, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return p;
    }

    public void RemoveParameter(Parameter parameter)
    {
        foreach (var pt in _model.PacketTypes)
            foreach (var f in pt.Fields.Where(f => f.Parameter == parameter))
                f.Parameter = null;
        _undoRedoManager.Push(new AddEntityCommand<Parameter>(
            parameter, _model.Parameters, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker) { IsRemove = true });
    }

    public Parameter DuplicateParameter(Parameter source)
    {
        var copy = new Parameter
        {
            Name             = $"Copy of {source.Name}",
            Kind             = source.Kind,
            DataType         = source.DataType,
            NumericId        = source.NumericId,
            Mnemonic         = source.Mnemonic,
            ShortDescription = source.ShortDescription,
            LongDescription  = source.LongDescription,
            Formula          = source.Formula,
            HexValue         = source.HexValue,
        };
        _undoRedoManager.Push(new AddEntityCommand<Parameter>(
            copy, _model.Parameters, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return copy;
    }

    // ── PacketType CRUD ────────────────────────────────────────────────────────

    public PacketType AddPacketType(string name, PacketTypeKind kind = PacketTypeKind.Telecommand)
    {
        var pt = new PacketType { Name = name, Kind = kind };
        _undoRedoManager.Push(new AddEntityCommand<PacketType>(
            pt, _model.PacketTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return pt;
    }

    public void RemovePacketType(PacketType packetType)
        => _undoRedoManager.Push(new AddEntityCommand<PacketType>(
            packetType, _model.PacketTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker) { IsRemove = true });

    public PacketType DuplicatePacketType(PacketType source)
    {
        var copy = new PacketType
        {
            Name        = $"Copy of {source.Name}",
            Kind        = source.Kind,
            Description = source.Description,
        };
        foreach (var f in source.Fields)
            copy.Fields.Add(new PacketField
            {
                Name            = f.Name,
                Description     = f.Description,
                Parameter       = f.Parameter,
                IsTypeIndicator = f.IsTypeIndicator,
                IndicatorValue  = f.IndicatorValue,
            });
        _undoRedoManager.Push(new AddEntityCommand<PacketType>(
            copy, _model.PacketTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return copy;
    }

    // ── HeaderType CRUD ────────────────────────────────────────────────────────

    public HeaderType AddHeaderType(string name)
    {
        var ht = new HeaderType { Name = name };
        _undoRedoManager.Push(new AddEntityCommand<HeaderType>(
            ht, _model.HeaderTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return ht;
    }

    public void RemoveHeaderType(HeaderType headerType)
    {
        foreach (var pt in _model.PacketTypes.Where(pt => pt.HeaderType == headerType))
            pt.HeaderType = null;
        _undoRedoManager.Push(new AddEntityCommand<HeaderType>(
            headerType, _model.HeaderTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker) { IsRemove = true });
    }

    public HeaderType DuplicateHeaderType(HeaderType source)
    {
        var copy = new HeaderType
        {
            Name        = $"Copy of {source.Name}",
            Description = source.Description,
        };
        foreach (var id in source.Ids)
            copy.Ids.Add(new HeaderTypeId
            {
                Name        = id.Name,
                Description = id.Description,
                DataType    = id.DataType,
            });
        _undoRedoManager.Push(new AddEntityCommand<HeaderType>(
            copy, _model.HeaderTypes, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return copy;
    }

    // ── Undo / Redo ────────────────────────────────────────────────────────────

    public bool CanUndo => _undoRedoManager.CanUndo;
    public bool CanRedo => _undoRedoManager.CanRedo;

    public void Undo() => _undoRedoManager.Undo();
    public void Redo() => _undoRedoManager.Redo();

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static DataType CreateDataType(string name, BaseType kind) => kind switch
    {
        BaseType.SignedInteger   => new SignedIntegerType   { Name = name },
        BaseType.UnsignedInteger => new UnsignedIntegerType { Name = name },
        BaseType.Float           => new FloatType           { Name = name },
        BaseType.Boolean         => new BooleanType         { Name = name },
        BaseType.BitString       => new BitStringType       { Name = name },
        BaseType.Enumerated      => new EnumeratedType      { Name = name },
        BaseType.Structure       => new StructureType       { Name = name },
        BaseType.Array           => new ArrayType           { Name = name },
        _                        => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    // ── Inner command types ────────────────────────────────────────────────────

    /// <summary>
    /// Generic reversible add/remove for entities that have no cross-references to clear.
    /// Set <see cref="IsRemove"/> = true to make Execute a removal.
    /// </summary>
    private sealed class AddEntityCommand<T> : IUndoableCommand
    {
        private readonly T _entity;
        private readonly List<T> _modelList;
        private readonly Action<T> _notifyAdded;
        private readonly Action<T> _notifyRemoved;
        private readonly DirtyTracker _dirty;

        public bool IsRemove { get; init; }

        public AddEntityCommand(T entity, List<T> modelList,
            Action<T> notifyAdded, Action<T> notifyRemoved, DirtyTracker dirty)
        {
            _entity = entity; _modelList = modelList;
            _notifyAdded = notifyAdded; _notifyRemoved = notifyRemoved;
            _dirty = dirty;
        }

        public void Execute()
        {
            if (IsRemove) { _modelList.Remove(_entity); _notifyRemoved(_entity); }
            else          { _modelList.Add(_entity);    _notifyAdded(_entity);   }
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            if (IsRemove) { _modelList.Add(_entity);    _notifyAdded(_entity);   }
            else          { _modelList.Remove(_entity); _notifyRemoved(_entity); }
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible removal of a DataType: captures all nullified cross-references
    /// so Undo can restore them (ICD-FUN-53).
    /// </summary>
    private sealed class RemoveDataTypeCommand : IUndoableCommand
    {
        private readonly DataType _dataType;
        private readonly DataModel _model;
        private readonly ChangeNotifier _notifier;
        private readonly DirtyTracker _dirty;

        // Captured references cleared during Execute; restored during Undo.
        private readonly List<(StructureField Field, DataType OldRef)> _structRefs = new();
        private readonly List<(ArrayType Array, DataType OldRef)> _arrayRefs = new();
        private readonly List<(Parameter Param, DataType OldRef)> _paramRefs = new();

        public RemoveDataTypeCommand(DataType dataType, DataModel model,
            ChangeNotifier notifier, DirtyTracker dirty)
        {
            _dataType = dataType; _model = model; _notifier = notifier; _dirty = dirty;
        }

        public void Execute()
        {
            _structRefs.Clear();
            _arrayRefs.Clear();
            _paramRefs.Clear();

            foreach (var dt in _model.DataTypes)
            {
                if (dt is StructureType st)
                    foreach (var f in st.Fields.Where(f => f.DataType == _dataType))
                    {
                        _structRefs.Add((f, _dataType));
                        f.DataType = null;
                    }
                if (dt is ArrayType at && at.ElementType == _dataType)
                {
                    _arrayRefs.Add((at, _dataType));
                    at.ElementType = null;
                }
            }
            foreach (var p in _model.Parameters.Where(p => p.DataType == _dataType))
            {
                _paramRefs.Add((p, _dataType));
                p.DataType = null;
            }

            _model.DataTypes.Remove(_dataType);
            _notifier.NotifyRemoved(_dataType);
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            _model.DataTypes.Add(_dataType);
            _notifier.NotifyAdded(_dataType);

            foreach (var (f, old) in _structRefs) f.DataType = old;
            foreach (var (at, old) in _arrayRefs) at.ElementType = old;
            foreach (var (p, old) in _paramRefs)  p.DataType = old;

            _dirty.MarkDirty();
        }
    }
}
