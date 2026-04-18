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
    public void ReloadFrom(DataModel model) => throw new NotImplementedException();

    /// <summary>Appends a newly created DataType to the observable collection.</summary>
    public void NotifyAdded(DataType dataType) => throw new NotImplementedException();

    /// <summary>Removes a deleted DataType from the observable collection.</summary>
    public void NotifyRemoved(DataType dataType) => throw new NotImplementedException();

    /// <summary>Appends a newly created Parameter to the observable collection.</summary>
    public void NotifyAdded(Parameter parameter) => throw new NotImplementedException();

    /// <summary>Removes a deleted Parameter from the observable collection.</summary>
    public void NotifyRemoved(Parameter parameter) => throw new NotImplementedException();

    /// <summary>Appends a newly created PacketType to the observable collection.</summary>
    public void NotifyAdded(PacketType packetType) => throw new NotImplementedException();

    /// <summary>Removes a deleted PacketType from the observable collection.</summary>
    public void NotifyRemoved(PacketType packetType) => throw new NotImplementedException();
}
