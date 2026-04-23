using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Wraps a <see cref="PacketType"/> for display in the Main Window tree and detail panel (ICD-IF-61).
/// Exposes an observable <see cref="Fields"/> collection so the UI reacts to Add/Remove.
/// </summary>
public partial class PacketTypeNodeViewModel : ObservableObject
{
    private readonly PacketType               _packetType;
    private readonly IReadOnlyList<Parameter> _availableParameters;
    private readonly DataModelManager         _dataModelManager;

    /// <summary>Invoked after any property change so the window can mark the model dirty.</summary>
    public Action? OnEdited { get; set; }

    public PacketTypeNodeViewModel(PacketType packetType, IReadOnlyList<Parameter> availableParameters,
        DataModelManager dataModelManager)
    {
        _packetType          = packetType;
        _availableParameters = availableParameters;
        _dataModelManager    = dataModelManager;

        Fields = new ObservableCollection<PacketFieldRowViewModel>(
            packetType.Fields.Select(f => MakeRow(f)));

        // Rebuild Fields when undo/redo mutates this packet type's field list (NC-08).
        _dataModelManager.PacketFieldsChanged += OnPacketFieldsChanged;

        // Populate Header ID value rows (HeaderType is already resolved when reloading).
        RefreshHeaderType();
    }

    public PacketType Model => _packetType;

    // ── Header properties (tree item display) ────────────────────────────────

    public string Name
    {
        get => _packetType.Name;
        set { _packetType.Name = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public string? Description
    {
        get => _packetType.Description;
        set { _packetType.Description = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public string NumericId
    {
        get => _packetType.NumericIdStr;
        set { _packetType.NumericIdStr = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public string? Mnemonic
    {
        get => _packetType.Mnemonic;
        set { _packetType.Mnemonic = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public string Kind => _packetType.Kind.ToString();

    // ── Header Type association ───────────────────────────────────────────────

    /// <summary>Display name of the associated Header Type, or "—" when none.</summary>
    public string HeaderTypeName => _packetType.HeaderType?.Name ?? "—";

    /// <summary>True when a Header Type is currently associated.</summary>
    public bool HasHeaderType => _packetType.HeaderType is not null;

    /// <summary>Editable fixed values for each Header Type ID.</summary>
    public ObservableCollection<HeaderIdValueRowViewModel> HeaderIdValueRows { get; } = new();

    /// <summary>
    /// Rebuilds <see cref="HeaderIdValueRows"/> from the currently associated Header Type.
    /// Must be called after <see cref="PacketType.HeaderType"/> is assigned or changed.
    /// </summary>
    public void RefreshHeaderType()
    {
        OnPropertyChanged(nameof(HeaderTypeName));
        OnPropertyChanged(nameof(HasHeaderType));
        HeaderIdValueRows.Clear();
        if (_packetType.HeaderType is null) return;
        foreach (var hid in _packetType.HeaderType.Ids)
            HeaderIdValueRows.Add(new HeaderIdValueRowViewModel(_packetType, hid) { OnEdited = OnEdited });
    }

    // ── Fields collection (detail panel) ─────────────────────────────────────

    public ObservableCollection<PacketFieldRowViewModel> Fields { get; }

    public void AddField()
    {
        _dataModelManager.AddPacketField(_packetType);
        // Fields rebuilt via PacketFieldsChanged event
        OnEdited?.Invoke();
    }

    public void RemoveField(PacketFieldRowViewModel row)
    {
        _dataModelManager.RemovePacketField(_packetType, row.Model);
        // Fields rebuilt via PacketFieldsChanged event
        OnEdited?.Invoke();
    }

    public void MoveField(PacketFieldRowViewModel dragged, PacketFieldRowViewModel target, bool above)
    {
        var fromIdx = Fields.IndexOf(dragged);
        var toIdx   = Fields.IndexOf(target);
        if (fromIdx < 0 || toIdx < 0 || fromIdx == toIdx) return;

        var insertAt = above ? toIdx : toIdx + 1;
        if (insertAt > fromIdx) insertAt--;

        _dataModelManager.MovePacketField(_packetType, fromIdx, insertAt);
        // Fields rebuilt via PacketFieldsChanged event
        OnEdited?.Invoke();
    }

    private void OnPacketFieldsChanged(PacketType pt)
    {
        if (pt != _packetType) return;
        RebuildFields();
    }

    private void RebuildFields()
    {
        Fields.Clear();
        foreach (var f in _packetType.Fields)
            Fields.Add(MakeRow(f));
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private PacketFieldRowViewModel MakeRow(PacketField field)
        => new(field, _availableParameters, AddField, RemoveField);
}
