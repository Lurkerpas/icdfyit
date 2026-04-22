using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using IcdFyIt.App.Controls;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class OptionsWindow : Window
{
    public OptionsWindow()
    {
        InitializeComponent();

        // CellFactory for the Browse button column
        TemplatesGrid.Columns.First(c => c.Name == "Browse").CellFactory = _ =>
        {
            var btn = new Button
            {
                Content             = "…",
                Padding             = new Thickness(4, 2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Center,
            };
            ToolTip.SetTip(btn, "Browse for template file");
            btn.Bind(Button.CommandProperty,
                new Binding(nameof(TemplateRowViewModel.BrowseCommand)));
            return btn;
        };

        // Delegate drag-to-reorder to the ViewModel so the underlying model list stays in sync.
        TemplatesGrid.ItemMoved += OnTemplateMoved;
    }

    private void OnTemplateMoved(object? sender, ItemMovedEventArgs e)
    {
        if (DataContext is not OptionsWindowViewModel vm) return;
        e.Handled = true;
        vm.MoveTemplate(
            (TemplateRowViewModel)e.DraggedItem,
            (TemplateRowViewModel)e.TargetItem,
            e.Above);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is OptionsWindowViewModel vm)
            vm.RequestClose = Close;
    }
}
