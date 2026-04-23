using Avalonia.Controls;
using Avalonia.Interactivity;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

/// <summary>
/// Modal dialog for creating a new Memory.
/// Call <c>await dialog.ShowDialog&lt;string?&gt;(owner)</c>; returns the name or null if cancelled.
/// </summary>
public partial class AddMemoryDialog : Window
{
    public AddMemoryDialog()
    {
        InitializeComponent();
        DataContext = new AddMemoryViewModel();
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddMemoryViewModel vm)
            Close(vm.Name.Trim());
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);
}
