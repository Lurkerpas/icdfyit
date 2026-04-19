using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Wraps a <see cref="PacketType"/> for display in the Main Window tree and detail panel (ICD-IF-61).
/// Exposes an observable <see cref="Fields"/> collection so the UI reacts to Add/Remove.
/// </summary>
public partial class PacketTypeNodeViewModel : ObservableObject
{
    private readonly PacketType               _packetType;
    private readonly IReadOnlyList<Parameter> _availableParameters;

    /// <summary>Invoked after any property change so the window can mark the model dirty.</summary>
    public Action? OnEdited { get; set; }

    public PacketTypeNodeViewModel(PacketType packetType, IReadOnlyList<Parameter> availableParameters)
    {
        _packetType          = packetType;
        _availableParameters = availableParameters;

        Fields = new ObservableCollection<PacketFieldRowViewModel>(
            packetType.Fields.Select(f => MakeRow(f)));
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

    public string Kind => _packetType.Kind.ToString();

    // ── Fields collection (detail panel) ─────────────────────────────────────

    public ObservableCollection<PacketFieldRowViewModel> Fields { get; }

    public void AddField()
    {
        var field = new PacketField { Name = "NewField" };
        _packetType.Fields.Add(field);
        Fields.Add(MakeRow(field));
        OnEdited?.Invoke();
    }

    public void RemoveField(PacketFieldRowViewModel row)
    {
        _packetType.Fields.Remove(row.Model);
        Fields.Remove(row);
        OnEdited?.Invoke();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private PacketFieldRowViewModel MakeRow(PacketField field)
        => new(field, _availableParameters, AddField, RemoveField);
}
