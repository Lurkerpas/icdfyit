using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using IcdFyIt.App.Controls;
using IcdFyIt.App.Services;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

/// <summary>
/// Pop-up dialog for editing the fields of a <see cref="IcdFyIt.Core.Model.StructureType"/>.
/// Open with <c>await dialog.ShowDialog(owner)</c>.
/// </summary>
public partial class StructureFieldsDialog : Window
{
    public StructureFieldsDialog()
    {
        InitializeComponent();
        LayoutPersistenceManager.Register(this);

        // CellFactory for the Data Type column (AutoCompleteBox — safe in DraggableGrid on X11)
        FieldsGrid.Columns.First(c => c.Name == "DataType").CellFactory = _ =>
        {
            var acb = new AutoCompleteBox
            {
                FilterMode           = AutoCompleteFilterMode.Contains,
                MinimumPrefixLength  = 0,
            };
            acb.Bind(AutoCompleteBox.ItemsSourceProperty,
                new Binding(nameof(StructureFieldRowViewModel.AvailableTypes)));
            acb.Bind(AutoCompleteBox.SelectedItemProperty,
                new Binding(nameof(StructureFieldRowViewModel.DataType)) { Mode = BindingMode.TwoWay });
            return acb;
        };

        // Delegate drag-to-reorder to the ViewModel so the underlying model list is kept in sync.
        FieldsGrid.ItemMoved += OnItemMoved;
    }

    private void OnItemMoved(object? sender, ItemMovedEventArgs e)
    {
        if (DataContext is not StructureFieldsDialogViewModel vm) return;
        e.Handled = true;
        vm.MoveField(
            (StructureFieldRowViewModel)e.DraggedItem,
            (StructureFieldRowViewModel)e.TargetItem,
            e.Above);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
