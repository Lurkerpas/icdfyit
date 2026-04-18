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

    /// <summary>Replaces all observable collections from the supplied model (called after New/Open).</summary>
    public void ReloadFrom(DataModel model)
    {
        DataTypes.Clear();
        foreach (var dt in model.DataTypes) DataTypes.Add(dt);

        Parameters.Clear();
        foreach (var p in model.Parameters) Parameters.Add(p);

        PacketTypes.Clear();
        foreach (var pt in model.PacketTypes) PacketTypes.Add(pt);
    }

    public void NotifyAdded(DataType dataType) => DataTypes.Add(dataType);
    public void NotifyRemoved(DataType dataType) => DataTypes.Remove(dataType);

    public void NotifyAdded(Parameter parameter) => Parameters.Add(parameter);
    public void NotifyRemoved(Parameter parameter) => Parameters.Remove(parameter);

    public void NotifyAdded(PacketType packetType) => PacketTypes.Add(packetType);
    public void NotifyRemoved(PacketType packetType) => PacketTypes.Remove(packetType);
}
