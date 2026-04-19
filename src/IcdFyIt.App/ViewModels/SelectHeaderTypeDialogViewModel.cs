using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the "Select Header Type" dialog.
/// Directly updates <see cref="PacketType.HeaderType"/> when the user picks a value.
/// </summary>
public partial class SelectHeaderTypeDialogViewModel : ObservableObject
{
    private readonly PacketType _packetType;

    public SelectHeaderTypeDialogViewModel(PacketType packetType, IReadOnlyList<HeaderType> availableTypes)
    {
        _packetType    = packetType;
        AvailableTypes = availableTypes;
    }

    public string PacketTypeName => _packetType.Name;

    public IReadOnlyList<HeaderType> AvailableTypes { get; }

    public HeaderType? SelectedHeaderType
    {
        get => _packetType.HeaderType;
        set
        {
            if (value is null) return;
            _packetType.HeaderType = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Clears the Header Type association on the Packet Type.</summary>
    public void Clear() => _packetType.HeaderType = null;
}
