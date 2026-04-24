using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Row wrapper for ICD metadata in the Metadata window DraggableGrid.
/// </summary>
public partial class MetadataRowViewModel : ObservableObject
{
    public MetadataField? Model { get; }

    public MetadataBuiltInField? BuiltInField { get; }

    public bool IsBuiltIn => BuiltInField is not null;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _value;

    public string KindDisplay => IsBuiltIn ? "Built-in" : "Custom";

    public MetadataRowViewModel(MetadataBuiltInField builtInField, string value)
    {
        BuiltInField = builtInField;
        _name = BuiltInName(builtInField);
        _value = value;
    }

    public MetadataRowViewModel(MetadataField model)
    {
        Model = model;
        _name = model.Name;
        _value = model.Value;
    }

    public void SyncBuiltInValue(string value)
    {
        if (!IsBuiltIn) return;
        Name = BuiltInName(BuiltInField!.Value);
        Value = value;
    }

    public static string BuiltInName(MetadataBuiltInField field) => field switch
    {
        MetadataBuiltInField.Name => "name",
        MetadataBuiltInField.Version => "version",
        MetadataBuiltInField.Date => "date",
        MetadataBuiltInField.Status => "status",
        MetadataBuiltInField.Description => "description",
        _ => string.Empty,
    };

    public static bool TryParseBuiltInField(string name, out MetadataBuiltInField field)
    {
        if (string.Equals(name, "name", StringComparison.OrdinalIgnoreCase))
        {
            field = MetadataBuiltInField.Name;
            return true;
        }
        if (string.Equals(name, "version", StringComparison.OrdinalIgnoreCase))
        {
            field = MetadataBuiltInField.Version;
            return true;
        }
        if (string.Equals(name, "date", StringComparison.OrdinalIgnoreCase))
        {
            field = MetadataBuiltInField.Date;
            return true;
        }
        if (string.Equals(name, "status", StringComparison.OrdinalIgnoreCase))
        {
            field = MetadataBuiltInField.Status;
            return true;
        }
        if (string.Equals(name, "description", StringComparison.OrdinalIgnoreCase))
        {
            field = MetadataBuiltInField.Description;
            return true;
        }

        field = default;
        return false;
    }
}