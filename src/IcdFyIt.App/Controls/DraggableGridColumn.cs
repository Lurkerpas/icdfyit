using Avalonia.Controls;

namespace IcdFyIt.App.Controls;

public enum DraggableGridColumnType { Text, Checkbox, DragHandle }

/// <summary>Column descriptor for <see cref="DraggableGrid"/>.</summary>
public class DraggableGridColumn
{
    /// <summary>Text displayed in the column header. Empty string for the drag-handle column.</summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>Property name on the item used for data binding (edit source).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Optional separate property name used for the read-only display value.
    /// When null, <see cref="Path"/> is used for display as well.</summary>
    public string? DisplayPath { get; set; }

    /// <summary>Value shown when the bound display value is null.</summary>
    public string NullText { get; set; } = "";

    /// <summary>Boolean property name on the item that enables or disables the edit TextBox.
    /// When null, editability is always enabled (subject to <see cref="IsEditable"/>).</summary>
    public string? IsEnabledPath { get; set; }

    /// <summary>Boolean property name on the item that drives 1.0 / 0.4 opacity on the cell.
    /// When null, opacity is always 1.0.</summary>
    public string? OpacityPath { get; set; }

    public GridLength Width { get; set; } = new GridLength(1, GridUnitType.Star);

    public DraggableGridColumnType ColumnType { get; set; } = DraggableGridColumnType.Text;

    /// <summary>Whether the column exposes an editable TextBox.</summary>
    public bool IsEditable { get; set; } = false;

    /// <summary>Whether the column is currently visible. Toggled via <see cref="DraggableGrid.SetColumnVisible"/>.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Logical name used to identify the column in <see cref="DraggableGrid.SetColumnVisible"/>.</summary>
    public string? Name { get; set; }

    /// <summary>Custom factory that creates the cell control for a given item.
    /// Overrides all built-in rendering when set. The row's DataContext is already set
    /// to the item, so Avalonia bindings inside the returned control work normally.</summary>
    public Func<object, Control>? CellFactory { get; set; }
}
