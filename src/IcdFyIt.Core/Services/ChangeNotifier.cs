using System.Collections.ObjectModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Services;

/// <summary>
/// Exposes the live, observable collections that reflect the current data model state.
/// ViewModels bind to these collections for cross-window reactivity (ICD-DES §5.7).
/// DataModelManager updates these collections after every mutation.
/// </summary>
public class ChangeNotifier
{
    public ObservableCollection<DataType> DataTypes { get; } = new();
    public ObservableCollection<Parameter> Parameters { get; } = new();
    public ObservableCollection<PacketType> PacketTypes { get; } = new();
    public ObservableCollection<HeaderType> HeaderTypes { get; } = new();
    public ObservableCollection<Memory> Memories { get; } = new();
    public ObservableCollection<MetadataField> MetadataFields { get; } = new();

    /// <summary>Replaces all observable collections from the supplied model (called after New/Open).</summary>
    public void ReloadFrom(DataModel model)
    {
        DataTypes.Clear();
        foreach (var dt in model.DataTypes) DataTypes.Add(dt);

        Parameters.Clear();
        foreach (var p in model.Parameters) Parameters.Add(p);

        PacketTypes.Clear();
        foreach (var pt in model.PacketTypes) PacketTypes.Add(pt);

        HeaderTypes.Clear();
        foreach (var ht in model.HeaderTypes) HeaderTypes.Add(ht);

        Memories.Clear();
        foreach (var m in model.Memories) Memories.Add(m);

        MetadataFields.Clear();
        foreach (var f in model.Metadata.Fields) MetadataFields.Add(f);
    }

    public void NotifyAdded(DataType dataType) => DataTypes.Add(dataType);
    public void NotifyRemoved(DataType dataType) => DataTypes.Remove(dataType);

    public void NotifyAdded(Parameter parameter) => Parameters.Add(parameter);
    public void NotifyRemoved(Parameter parameter) => Parameters.Remove(parameter);
    public void MoveParameter(Parameter parameter, int newIndex)
    {
        var oldIndex = Parameters.IndexOf(parameter);
        if (oldIndex >= 0 && oldIndex != newIndex)
            Parameters.Move(oldIndex, Math.Clamp(newIndex, 0, Parameters.Count - 1));
    }

    public void NotifyAdded(PacketType packetType) => PacketTypes.Add(packetType);
    public void NotifyRemoved(PacketType packetType) => PacketTypes.Remove(packetType);

    public void NotifyAdded(HeaderType headerType) => HeaderTypes.Add(headerType);
    public void NotifyRemoved(HeaderType headerType) => HeaderTypes.Remove(headerType);

    public void NotifyAdded(Memory memory) => Memories.Add(memory);
    public void NotifyRemoved(Memory memory) => Memories.Remove(memory);
    public void MoveMemory(Memory memory, int newIndex)
    {
        var oldIndex = Memories.IndexOf(memory);
        if (oldIndex >= 0 && oldIndex != newIndex)
            Memories.Move(oldIndex, Math.Clamp(newIndex, 0, Memories.Count - 1));
    }

    public void NotifyAdded(MetadataField field) => MetadataFields.Add(field);
    public void NotifyRemoved(MetadataField field) => MetadataFields.Remove(field);
    public void MoveMetadataField(MetadataField field, int newIndex)
    {
        var oldIndex = MetadataFields.IndexOf(field);
        if (oldIndex >= 0 && oldIndex != newIndex)
            MetadataFields.Move(oldIndex, Math.Clamp(newIndex, 0, MetadataFields.Count - 1));
    }
}
