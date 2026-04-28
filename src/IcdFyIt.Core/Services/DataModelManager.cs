using IcdFyIt.Core.Model;
using IcdFyIt.Core.Persistence;

namespace IcdFyIt.Core.Services;

public enum MetadataBuiltInField
{
    Name,
    Version,
    Date,
    Status,
    Description,
}

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

    /// <summary>Discards the current model and starts a new empty one (ICD-FUN-50).</summary>
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

        _model.Memories.Clear();
        _model.Memories.AddRange(_changeNotifier.Memories);

        _model.Metadata.Fields.Clear();
        _model.Metadata.Fields.AddRange(_changeNotifier.MetadataFields);

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
        var nextId = _model.Parameters.Count > 0
            ? _model.Parameters.Max(p => p.NumericId) + 1
            : 0;
        var p = new Parameter { Name = name, NumericId = nextId };
        _undoRedoManager.Push(new AddEntityCommand<Parameter>(
            p, _model.Parameters, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return p;
    }

    public void RemoveParameter(Parameter parameter)
        => _undoRedoManager.Push(new RemoveParameterCommand(parameter, _model, _changeNotifier, _dirtyTracker));

    public void MoveParameter(Parameter parameter, int newIndex)
    {
        var fromIndex = _changeNotifier.Parameters.IndexOf(parameter);
        if (fromIndex < 0) return;

        var toIndex = Math.Clamp(newIndex, 0, _changeNotifier.Parameters.Count - 1);
        if (fromIndex == toIndex) return;

        _undoRedoManager.Push(new MoveParameterCommand(
            parameter,
            fromIndex,
            toIndex,
            _changeNotifier,
            _dirtyTracker));
    }

    public Parameter DuplicateParameter(Parameter source)
    {
        var nextId = _model.Parameters.Count > 0
            ? _model.Parameters.Max(p => p.NumericId) + 1
            : 0;
        var copy = new Parameter
        {
            Name                = $"Copy of {source.Name}",
            Kind                = source.Kind,
            DataType            = source.DataType,
            NumericId           = nextId,
            Mnemonic            = source.Mnemonic,
            ShortDescription    = source.ShortDescription,
            LongDescription     = source.LongDescription,
            Formula             = source.Formula,
            HexValue            = source.HexValue,
            Memory              = source.Memory,
            MemoryOffsetStr     = source.MemoryOffsetStr,
            ValidityParameter   = source.ValidityParameter,
            AlarmLow            = source.AlarmLow,
            AlarmHigh           = source.AlarmHigh,
        };
        _undoRedoManager.Push(new AddEntityCommand<Parameter>(
            copy, _model.Parameters, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return copy;
    }

    // ── PacketType CRUD ────────────────────────────────────────────────────────

    public PacketType AddPacketType(string name, PacketTypeKind kind = PacketTypeKind.Telecommand)
    {
        var nextId = _model.PacketTypes.Count > 0
            ? _model.PacketTypes.Max(p => p.NumericId) + 1
            : 0;
        var pt = new PacketType { Name = name, Kind = kind, NumericId = nextId };
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
        var nextId = _model.PacketTypes.Count > 0
            ? _model.PacketTypes.Max(p => p.NumericId) + 1
            : 0;
        var copy = new PacketType
        {
            Name        = $"Copy of {source.Name}",
            Kind        = source.Kind,
            Description = source.Description,
            NumericId   = nextId,
            Mnemonic    = source.Mnemonic,
            HeaderType  = source.HeaderType,
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
        foreach (var v in source.HeaderIdValues)
            copy.HeaderIdValues.Add(new HeaderIdValue { IdRef = v.IdRef, Value = v.Value });
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
        => _undoRedoManager.Push(new RemoveHeaderTypeCommand(headerType, _model, _changeNotifier, _dirtyTracker));

    public HeaderType DuplicateHeaderType(HeaderType source)
    {
        var copy = new HeaderType
        {
            Name        = $"Copy of {source.Name}",
            Mnemonic    = source.Mnemonic,
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

    // ── Memory CRUD ────────────────────────────────────────────────────────────

    public Memory AddMemory(string name)
    {
        var nextId = _model.Memories.Count > 0
            ? _model.Memories.Max(m => m.NumericId) + 1
            : 0;
        var m = new Memory { Name = name, NumericId = nextId };
        _undoRedoManager.Push(new AddEntityCommand<Memory>(
            m, _model.Memories, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return m;
    }

    public void RemoveMemory(Memory memory)
        => _undoRedoManager.Push(new RemoveMemoryCommand(memory, _model, _changeNotifier, _dirtyTracker));

    public void MoveMemory(Memory memory, int newIndex)
    {
        var fromIndex = _changeNotifier.Memories.IndexOf(memory);
        if (fromIndex < 0) return;

        var toIndex = Math.Clamp(newIndex, 0, _changeNotifier.Memories.Count - 1);
        if (fromIndex == toIndex) return;

        _undoRedoManager.Push(new MoveMemoryCommand(
            memory,
            fromIndex,
            toIndex,
            _changeNotifier,
            _dirtyTracker));
    }

    public Memory DuplicateMemory(Memory source)
    {
        var nextId = _model.Memories.Count > 0
            ? _model.Memories.Max(m => m.NumericId) + 1
            : 0;
        var copy = new Memory
        {
            Name        = $"Copy of {source.Name}",
            NumericId   = nextId,
            Mnemonic    = source.Mnemonic,
            SizeStr     = source.SizeStr,
            Address     = source.Address,
            Description = source.Description,
            AlignmentStr = source.AlignmentStr,
            IsWritable  = source.IsWritable,
            IsReadable  = source.IsReadable,
        };
        _undoRedoManager.Push(new AddEntityCommand<Memory>(
            copy, _model.Memories, _changeNotifier.NotifyAdded, _changeNotifier.NotifyRemoved,
            _dirtyTracker));
        return copy;
    }

    // ── ICD Metadata CRUD ─────────────────────────────────────────────────────

    public MetadataField AddMetadataField(string name)
    {
        var field = new MetadataField { Name = name, Value = string.Empty };
        _undoRedoManager.Push(new AddMetadataFieldCommand(
            field,
            _model.Metadata.Fields,
            _changeNotifier,
            () => MetadataChanged?.Invoke(),
            _dirtyTracker));
        return field;
    }

    public void RemoveMetadataField(MetadataField field)
        => _undoRedoManager.Push(new AddMetadataFieldCommand(
            field,
            _model.Metadata.Fields,
            _changeNotifier,
            () => MetadataChanged?.Invoke(),
            _dirtyTracker)
        {
            IsRemove = true
        });

    public MetadataField DuplicateMetadataField(MetadataField source)
    {
        var copy = new MetadataField
        {
            Name = $"Copy of {source.Name}",
            Value = source.Value,
        };
        _undoRedoManager.Push(new AddMetadataFieldCommand(
            copy,
            _model.Metadata.Fields,
            _changeNotifier,
            () => MetadataChanged?.Invoke(),
            _dirtyTracker));
        return copy;
    }

    public void MoveMetadataField(MetadataField field, int newIndex)
    {
        var fromIndex = _changeNotifier.MetadataFields.IndexOf(field);
        if (fromIndex < 0) return;

        var toIndex = Math.Clamp(newIndex, 0, _changeNotifier.MetadataFields.Count - 1);
        if (fromIndex == toIndex) return;

        _undoRedoManager.Push(new MoveMetadataFieldCommand(
            field,
            fromIndex,
            toIndex,
            _changeNotifier,
            () => MetadataChanged?.Invoke(),
            _dirtyTracker));
    }

    public void UpdateMetadataFieldName(MetadataField field, string newName)
    {
        if (field.Name == newName) return;
        _undoRedoManager.Push(new SetMetadataFieldPropertyCommand(
            field,
            isName: true,
            newValue: newName,
            () => MetadataChanged?.Invoke(),
            _dirtyTracker));
    }

    public void UpdateMetadataFieldValue(MetadataField field, string newValue)
    {
        if (field.Value == newValue) return;
        _undoRedoManager.Push(new SetMetadataFieldPropertyCommand(
            field,
            isName: false,
            newValue: newValue,
            () => MetadataChanged?.Invoke(),
            _dirtyTracker));
    }

    public string GetMetadataValue(MetadataBuiltInField field) => field switch
    {
        MetadataBuiltInField.Name => _model.Metadata.Name,
        MetadataBuiltInField.Version => _model.Metadata.Version,
        MetadataBuiltInField.Date => _model.Metadata.Date,
        MetadataBuiltInField.Status => _model.Metadata.Status,
        MetadataBuiltInField.Description => _model.Metadata.Description,
        _ => string.Empty,
    };

    public void SetMetadataValue(MetadataBuiltInField field, string value)
    {
        if (GetMetadataValue(field) == value) return;
        _undoRedoManager.Push(new SetMetadataValueCommand(
            _model.Metadata,
            field,
            value,
            () => MetadataChanged?.Invoke(),
            _dirtyTracker));
    }

    // ── Undo / Redo ────────────────────────────────────────────────────────────

    public bool CanUndo => _undoRedoManager.CanUndo;
    public bool CanRedo => _undoRedoManager.CanRedo;

    public void Undo() => _undoRedoManager.Undo();
    public void Redo() => _undoRedoManager.Redo();

    // ── Change events for sub-entity mutations (NC-07, NC-08) ─────────────────

    /// <summary>Fired when a HeaderType's Ids list changes (add/remove). Used by VMs to sync IdRows.</summary>
    public event Action<HeaderType>? HeaderTypeIdsChanged;

    /// <summary>Fired when a PacketType's Fields list changes (add/remove/move). Used by VMs to sync Fields.</summary>
    public event Action<PacketType>? PacketFieldsChanged;

    /// <summary>Fired when document-level metadata changes.</summary>
    public event Action? MetadataChanged;

    // ── HeaderTypeId CRUD (ICD-IF-170, NC-07) ─────────────────────────────────

    public HeaderTypeId AddHeaderTypeId(HeaderType headerType)
    {
        var id = new HeaderTypeId { Name = "NewId" };
        _undoRedoManager.Push(new AddHeaderTypeIdCommand(id, headerType,
            () => HeaderTypeIdsChanged?.Invoke(headerType), _dirtyTracker));
        return id;
    }

    public void RemoveHeaderTypeId(HeaderType headerType, HeaderTypeId id)
        => _undoRedoManager.Push(new AddHeaderTypeIdCommand(id, headerType,
            () => HeaderTypeIdsChanged?.Invoke(headerType), _dirtyTracker) { IsRemove = true });

    // ── PacketField CRUD (ICD-IF-170, NC-08) ──────────────────────────────────

    public PacketField AddPacketField(PacketType packetType)
    {
        var field = new PacketField { Name = "NewField" };
        _undoRedoManager.Push(new AddPacketFieldCommand(field, packetType,
            () => PacketFieldsChanged?.Invoke(packetType), _dirtyTracker));
        return field;
    }

    public void RemovePacketField(PacketType packetType, PacketField field)
        => _undoRedoManager.Push(new AddPacketFieldCommand(field, packetType,
            () => PacketFieldsChanged?.Invoke(packetType), _dirtyTracker) { IsRemove = true });

    public void MovePacketField(PacketType packetType, int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        _undoRedoManager.Push(new MovePacketFieldCommand(packetType, fromIndex, toIndex,
            () => PacketFieldsChanged?.Invoke(packetType), _dirtyTracker));
    }

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

    /// <summary>
    /// Reversible removal of a Parameter: captures all packet-field cross-references
    /// so Undo can restore them (ICD-FUN-53, NC-05).
    /// </summary>
    private sealed class RemoveParameterCommand : IUndoableCommand
    {
        private readonly Parameter _parameter;
        private readonly DataModel _model;
        private readonly ChangeNotifier _notifier;
        private readonly DirtyTracker _dirty;
        private readonly List<PacketField> _fieldRefs = new();
        private readonly List<Parameter> _validityRefs = new();

        public RemoveParameterCommand(Parameter parameter, DataModel model,
            ChangeNotifier notifier, DirtyTracker dirty)
        {
            _parameter = parameter; _model = model; _notifier = notifier; _dirty = dirty;
        }

        public void Execute()
        {
            _fieldRefs.Clear();
            foreach (var pt in _model.PacketTypes)
                foreach (var f in pt.Fields.Where(f => f.Parameter == _parameter))
                {
                    _fieldRefs.Add(f);
                    f.Parameter = null;
                }
            // Null out ValidityParameter references on sibling parameters.
            _validityRefs.Clear();
            foreach (var p in _model.Parameters.Where(p => p.ValidityParameter == _parameter))
            {
                _validityRefs.Add(p);
                p.ValidityParameter = null;
            }
            _model.Parameters.Remove(_parameter);
            _notifier.NotifyRemoved(_parameter);
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            _model.Parameters.Add(_parameter);
            _notifier.NotifyAdded(_parameter);
            foreach (var f in _fieldRefs) f.Parameter = _parameter;
            foreach (var p in _validityRefs) p.ValidityParameter = _parameter;
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible removal of a Memory: nullifies all Parameter.Memory references
    /// so Undo can restore them (ICD-FUN-53).
    /// </summary>
    private sealed class RemoveMemoryCommand : IUndoableCommand
    {
        private readonly Memory _memory;
        private readonly DataModel _model;
        private readonly ChangeNotifier _notifier;
        private readonly DirtyTracker _dirty;
        private readonly List<(Parameter Param, int Offset)> _paramRefs = new();

        public RemoveMemoryCommand(Memory memory, DataModel model,
            ChangeNotifier notifier, DirtyTracker dirty)
        {
            _memory = memory; _model = model; _notifier = notifier; _dirty = dirty;
        }

        public void Execute()
        {
            _paramRefs.Clear();
            foreach (var p in _model.Parameters.Where(p => p.Memory == _memory))
            {
                _paramRefs.Add((p, p.MemoryOffset));
                p.Memory = null;
            }
            _model.Memories.Remove(_memory);
            _notifier.NotifyRemoved(_memory);
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            _model.Memories.Add(_memory);
            _notifier.NotifyAdded(_memory);
            foreach (var (p, offset) in _paramRefs) { p.Memory = _memory; p.MemoryOffset = offset; }
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible removal of a HeaderType: captures all packet-type header-type references
    /// so Undo can restore them (ICD-FUN-53, NC-06).
    /// </summary>
    private sealed class RemoveHeaderTypeCommand : IUndoableCommand
    {
        private readonly HeaderType _headerType;
        private readonly DataModel _model;
        private readonly ChangeNotifier _notifier;
        private readonly DirtyTracker _dirty;
        private readonly List<PacketType> _affectedPacketTypes = new();

        public RemoveHeaderTypeCommand(HeaderType headerType, DataModel model,
            ChangeNotifier notifier, DirtyTracker dirty)
        {
            _headerType = headerType; _model = model; _notifier = notifier; _dirty = dirty;
        }

        public void Execute()
        {
            _affectedPacketTypes.Clear();
            foreach (var pt in _model.PacketTypes.Where(pt => pt.HeaderType == _headerType))
            {
                _affectedPacketTypes.Add(pt);
                pt.HeaderType = null;
            }
            _model.HeaderTypes.Remove(_headerType);
            _notifier.NotifyRemoved(_headerType);
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            _model.HeaderTypes.Add(_headerType);
            _notifier.NotifyAdded(_headerType);
            foreach (var pt in _affectedPacketTypes) pt.HeaderType = _headerType;
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible add/remove of a HeaderTypeId entry (ICD-IF-170, NC-07).
    /// Set <see cref="IsRemove"/> = true to make Execute a removal.
    /// </summary>
    private sealed class AddHeaderTypeIdCommand : IUndoableCommand
    {
        private readonly HeaderTypeId _id;
        private readonly HeaderType _headerType;
        private readonly Action _notify;
        private readonly DirtyTracker _dirty;
        private int _savedIndex = -1;

        public bool IsRemove { get; init; }

        public AddHeaderTypeIdCommand(HeaderTypeId id, HeaderType headerType,
            Action notify, DirtyTracker dirty)
        {
            _id = id; _headerType = headerType; _notify = notify; _dirty = dirty;
        }

        public void Execute()
        {
            if (IsRemove) { _savedIndex = _headerType.Ids.IndexOf(_id); _headerType.Ids.Remove(_id); }
            else { _headerType.Ids.Add(_id); }
            _notify();
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            if (IsRemove)
            {
                if (_savedIndex >= 0 && _savedIndex <= _headerType.Ids.Count)
                    _headerType.Ids.Insert(_savedIndex, _id);
                else
                    _headerType.Ids.Add(_id);
            }
            else { _headerType.Ids.Remove(_id); }
            _notify();
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible add/remove of a PacketField (ICD-IF-170, NC-08).
    /// Set <see cref="IsRemove"/> = true to make Execute a removal.
    /// </summary>
    private sealed class AddPacketFieldCommand : IUndoableCommand
    {
        private readonly PacketField _field;
        private readonly PacketType _packetType;
        private readonly Action _notify;
        private readonly DirtyTracker _dirty;
        private int _savedIndex = -1;

        public bool IsRemove { get; init; }

        public AddPacketFieldCommand(PacketField field, PacketType packetType,
            Action notify, DirtyTracker dirty)
        {
            _field = field; _packetType = packetType; _notify = notify; _dirty = dirty;
        }

        public void Execute()
        {
            if (IsRemove) { _savedIndex = _packetType.Fields.IndexOf(_field); _packetType.Fields.Remove(_field); }
            else { _packetType.Fields.Add(_field); }
            _notify();
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            if (IsRemove)
            {
                if (_savedIndex >= 0 && _savedIndex <= _packetType.Fields.Count)
                    _packetType.Fields.Insert(_savedIndex, _field);
                else
                    _packetType.Fields.Add(_field);
            }
            else { _packetType.Fields.Remove(_field); }
            _notify();
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible reorder of a PacketField within its PacketType (ICD-IF-170, NC-08).
    /// </summary>
    private sealed class MovePacketFieldCommand : IUndoableCommand
    {
        private readonly PacketType _packetType;
        private readonly int _from;
        private readonly int _to;
        private readonly Action _notify;
        private readonly DirtyTracker _dirty;

        public MovePacketFieldCommand(PacketType packetType, int from, int to,
            Action notify, DirtyTracker dirty)
        {
            _packetType = packetType; _from = from; _to = to; _notify = notify; _dirty = dirty;
        }

        public void Execute()
        {
            var field = _packetType.Fields[_from];
            _packetType.Fields.RemoveAt(_from);
            _packetType.Fields.Insert(_to, field);
            _notify();
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            var field = _packetType.Fields[_to];
            _packetType.Fields.RemoveAt(_to);
            _packetType.Fields.Insert(_from, field);
            _notify();
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible reorder of a Parameter within the observable collection.
    /// </summary>
    private sealed class MoveParameterCommand : IUndoableCommand
    {
        private readonly Parameter _parameter;
        private readonly int _from;
        private readonly int _to;
        private readonly ChangeNotifier _notifier;
        private readonly DirtyTracker _dirty;

        public MoveParameterCommand(
            Parameter parameter,
            int from,
            int to,
            ChangeNotifier notifier,
            DirtyTracker dirty)
        {
            _parameter = parameter;
            _from = from;
            _to = to;
            _notifier = notifier;
            _dirty = dirty;
        }

        public void Execute()
        {
            _notifier.MoveParameter(_parameter, _to);
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            _notifier.MoveParameter(_parameter, _from);
            _dirty.MarkDirty();
        }
    }

    /// <summary>
    /// Reversible reorder of a Memory within the observable collection.
    /// </summary>
    private sealed class MoveMemoryCommand : IUndoableCommand
    {
        private readonly Memory _memory;
        private readonly int _from;
        private readonly int _to;
        private readonly ChangeNotifier _notifier;
        private readonly DirtyTracker _dirty;

        public MoveMemoryCommand(
            Memory memory,
            int from,
            int to,
            ChangeNotifier notifier,
            DirtyTracker dirty)
        {
            _memory = memory;
            _from = from;
            _to = to;
            _notifier = notifier;
            _dirty = dirty;
        }

        public void Execute()
        {
            _notifier.MoveMemory(_memory, _to);
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            _notifier.MoveMemory(_memory, _from);
            _dirty.MarkDirty();
        }
    }

    private sealed class SetMetadataValueCommand : IUndoableCommand
    {
        private readonly IcdMetadata _metadata;
        private readonly MetadataBuiltInField _field;
        private readonly string _newValue;
        private readonly string _oldValue;
        private readonly Action _notify;
        private readonly DirtyTracker _dirty;

        public SetMetadataValueCommand(
            IcdMetadata metadata,
            MetadataBuiltInField field,
            string newValue,
            Action notify,
            DirtyTracker dirty)
        {
            _metadata = metadata;
            _field = field;
            _newValue = newValue;
            _notify = notify;
            _dirty = dirty;
            _oldValue = GetCurrent();
        }

        public void Execute()
        {
            SetCurrent(_newValue);
            _notify();
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            SetCurrent(_oldValue);
            _notify();
            _dirty.MarkDirty();
        }

        private string GetCurrent() => _field switch
        {
            MetadataBuiltInField.Name => _metadata.Name,
            MetadataBuiltInField.Version => _metadata.Version,
            MetadataBuiltInField.Date => _metadata.Date,
            MetadataBuiltInField.Status => _metadata.Status,
            MetadataBuiltInField.Description => _metadata.Description,
            _ => string.Empty,
        };

        private void SetCurrent(string value)
        {
            switch (_field)
            {
                case MetadataBuiltInField.Name:
                    _metadata.Name = value;
                    break;
                case MetadataBuiltInField.Version:
                    _metadata.Version = value;
                    break;
                case MetadataBuiltInField.Date:
                    _metadata.Date = value;
                    break;
                case MetadataBuiltInField.Status:
                    _metadata.Status = value;
                    break;
                case MetadataBuiltInField.Description:
                    _metadata.Description = value;
                    break;
            }
        }
    }

    private sealed class AddMetadataFieldCommand : IUndoableCommand
    {
        private readonly MetadataField _field;
        private readonly List<MetadataField> _modelList;
        private readonly ChangeNotifier _notifier;
        private readonly Action _notify;
        private readonly DirtyTracker _dirty;
        private int _savedIndex = -1;

        public bool IsRemove { get; init; }

        public AddMetadataFieldCommand(
            MetadataField field,
            List<MetadataField> modelList,
            ChangeNotifier notifier,
            Action notify,
            DirtyTracker dirty)
        {
            _field = field;
            _modelList = modelList;
            _notifier = notifier;
            _notify = notify;
            _dirty = dirty;
        }

        public void Execute()
        {
            if (IsRemove)
            {
                _savedIndex = _modelList.IndexOf(_field);
                _modelList.Remove(_field);
                _notifier.NotifyRemoved(_field);
            }
            else
            {
                _modelList.Add(_field);
                _notifier.NotifyAdded(_field);
            }

            _notify();
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            if (IsRemove)
            {
                if (_savedIndex >= 0 && _savedIndex <= _modelList.Count)
                    _modelList.Insert(_savedIndex, _field);
                else
                    _modelList.Add(_field);
                _notifier.NotifyAdded(_field);
            }
            else
            {
                _modelList.Remove(_field);
                _notifier.NotifyRemoved(_field);
            }

            _notify();
            _dirty.MarkDirty();
        }
    }

    private sealed class MoveMetadataFieldCommand : IUndoableCommand
    {
        private readonly MetadataField _field;
        private readonly int _from;
        private readonly int _to;
        private readonly ChangeNotifier _notifier;
        private readonly Action _notify;
        private readonly DirtyTracker _dirty;

        public MoveMetadataFieldCommand(
            MetadataField field,
            int from,
            int to,
            ChangeNotifier notifier,
            Action notify,
            DirtyTracker dirty)
        {
            _field = field;
            _from = from;
            _to = to;
            _notifier = notifier;
            _notify = notify;
            _dirty = dirty;
        }

        public void Execute()
        {
            _notifier.MoveMetadataField(_field, _to);
            _notify();
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            _notifier.MoveMetadataField(_field, _from);
            _notify();
            _dirty.MarkDirty();
        }
    }

    private sealed class SetMetadataFieldPropertyCommand : IUndoableCommand
    {
        private readonly MetadataField _field;
        private readonly bool _isName;
        private readonly string _newValue;
        private readonly string _oldValue;
        private readonly Action _notify;
        private readonly DirtyTracker _dirty;

        public SetMetadataFieldPropertyCommand(
            MetadataField field,
            bool isName,
            string newValue,
            Action notify,
            DirtyTracker dirty)
        {
            _field = field;
            _isName = isName;
            _newValue = newValue;
            _notify = notify;
            _dirty = dirty;
            _oldValue = isName ? field.Name : field.Value;
        }

        public void Execute()
        {
            if (_isName) _field.Name = _newValue;
            else _field.Value = _newValue;
            _notify();
            _dirty.MarkDirty();
        }

        public void Undo()
        {
            if (_isName) _field.Name = _oldValue;
            else _field.Value = _oldValue;
            _notify();
            _dirty.MarkDirty();
        }
    }
}
