using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using IcdFyIt.App.Controls;
using IcdFyIt.App.Services;
using IcdFyIt.App.ViewModels;
using Serilog;

namespace IcdFyIt.App.Views;

public partial class MainWindow : Window
{
    private bool _allowClose = false;

    public MainWindow()
    {
        InitializeComponent();
        LayoutPersistenceManager.Register(this);

        // Wire TreeView selection so only packet-type leaf nodes update SelectedPacketType.
        PacketTypeTree.SelectionChanged += (_, _) =>
        {
            if (DataContext is not MainWindowViewModel vm) return;
            vm.SelectedPacketType = PacketTypeTree.SelectedItem as PacketTypeNodeViewModel;
        };

        // CellFactory for Parameter column (AutoCompleteBox — safe in DraggableGrid on X11)
        FieldsGrid.Columns.First(c => c.Name == "Parameter").CellFactory = _ =>
        {
            var acb = new AutoCompleteBox
            {
                FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
            };
            acb.Bind(AutoCompleteBox.ItemsSourceProperty,
                new Binding(nameof(PacketFieldRowViewModel.AvailableParameters)));
            acb.Bind(AutoCompleteBox.SelectedItemProperty,
                new Binding(nameof(PacketFieldRowViewModel.Parameter)) { Mode = BindingMode.TwoWay });
            return acb;
        };

        // CellFactory for IsTypeIndicator column (editable CheckBox)
        FieldsGrid.Columns.First(c => c.Name == "IsTypeIndicator").CellFactory = _ =>
        {
            var cb = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(4, 0),
            };
            cb.Bind(CheckBox.IsCheckedProperty,
                new Binding(nameof(PacketFieldRowViewModel.IsTypeIndicator)) { Mode = BindingMode.TwoWay });
            return cb;
        };

        // CellFactory for IndicatorValue column (TextBox, hidden when not a type-indicator field)
        FieldsGrid.Columns.First(c => c.Name == "IndicatorValue").CellFactory = _ =>
        {
            var tb = new TextBox
            {
                Background      = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Foreground      = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Padding         = new Avalonia.Thickness(6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth        = 0,
            };
            tb.Bind(TextBox.TextProperty,
                new Binding(nameof(PacketFieldRowViewModel.IndicatorValue)) { Mode = BindingMode.TwoWay });
            tb.Bind(IsVisibleProperty,
                new Binding(nameof(PacketFieldRowViewModel.ShowIndicatorValue)));
            return tb;
        };

        // Notify the ViewModel whenever an inline cell edit is committed.
        FieldsGrid.EditEnded += (_, _) =>
            (DataContext as MainWindowViewModel)?.NotifyModelEdited();

        // Delegate drag-to-reorder to the SelectedPacketType so the underlying model is kept in sync.
        FieldsGrid.ItemMoved += OnFieldMoved;
    }

    private void OnFieldMoved(object? sender, ItemMovedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (vm.SelectedPacketType is null) return;
        e.Handled = true;
        vm.SelectedPacketType.MoveField(
            (PacketFieldRowViewModel)e.DraggedItem,
            (PacketFieldRowViewModel)e.TargetItem,
            e.Above);
    }

    // ── Close guard (ICD-IF-180) ──────────────────────────────────────────────

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_allowClose) return;
        if (DataContext is not MainWindowViewModel vm) return;
        if (!vm.IsDirty) return;

        // Cancel synchronously, then handle asynchronously.
        e.Cancel = true;
        _ = HandleClosingAsync(vm);
    }

    private async Task HandleClosingAsync(MainWindowViewModel vm)
    {
        try
        {
            var dialog = new UnsavedChangesDialog();
            var result = await dialog.ShowDialog<string?>(this);

            if (result == "save")
            {
                var saved = await vm.TrySaveForCloseAsync();
                if (!saved || vm.IsDirty)
                    return;

                _allowClose = true;
                Close();
            }
            else if (result == "discard")
            {
                _allowClose = true;
                Close();
            }
            // else: cancelled — stay open.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed while processing main-window close guard");
        }
    }

    private void OnResetSizesToDefaultClicked(object? sender, RoutedEventArgs e)
    {
        LayoutPersistenceManager.ResetToDefaultsForOpenWindows();
    }
}
