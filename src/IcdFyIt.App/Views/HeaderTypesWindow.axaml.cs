using Avalonia.Controls;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class HeaderTypesWindow : Window
{
    public HeaderTypesWindow()
    {
        InitializeComponent();
    }

    private void OnHeaderTypeCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is HeaderTypesWindowViewModel vm) vm.MarkEdited();
    }

    private void OnIdCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is HeaderTypesWindowViewModel vm) vm.MarkEdited();
    }
}
