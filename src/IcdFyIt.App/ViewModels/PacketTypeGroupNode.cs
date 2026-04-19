using System.Collections.ObjectModel;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// A top-level group node in the main window TreeView
/// (represents "Telecommands" or "Telemetries").
/// </summary>
public class PacketTypeGroupNode
{
    public string Name { get; }
    public ObservableCollection<PacketTypeNodeViewModel> Children { get; }

    public PacketTypeGroupNode(string name, ObservableCollection<PacketTypeNodeViewModel> children)
    {
        Name     = name;
        Children = children;
    }
}
