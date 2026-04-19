using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Row view-model for one Header Type ID entry in the Packet Type detail panel.
/// Exposes the ID name (read-only) and an editable fixed hex value.
/// </summary>
public partial class HeaderIdValueRowViewModel : ObservableObject
{
    private readonly PacketType   _packetType;
    private readonly HeaderTypeId _headerId;

    /// <summary>Invoked after the value is edited so the window can mark the model dirty.</summary>
    public Action? OnEdited { get; set; }

    public HeaderIdValueRowViewModel(PacketType packetType, HeaderTypeId headerId)
    {
        _packetType = packetType;
        _headerId   = headerId;
    }

    /// <summary>Display name of the Header Type ID (read-only).</summary>
    public string IdName => _headerId.Name;

    /// <summary>Fixed hex value stored in the Packet Type for this ID.</summary>
    public string Value
    {
        get => _packetType.HeaderIdValues.FirstOrDefault(v => v.IdRef == _headerId.Id)?.Value
               ?? string.Empty;
        set
        {
            var existing = _packetType.HeaderIdValues.FirstOrDefault(v => v.IdRef == _headerId.Id);
            if (existing is not null)
                existing.Value = value;
            else
                _packetType.HeaderIdValues.Add(new HeaderIdValue { IdRef = _headerId.Id, Value = value });
            OnPropertyChanged();
            OnEdited?.Invoke();
        }
    }
}
