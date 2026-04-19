using Avalonia.Controls;
using Avalonia.Interactivity;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

/// <summary>
/// Modal dialog for creating a new Parameter.
/// Call <c>await dialog.ShowDialog&lt;string?&gt;(owner)</c>; returns the name or null if cancelled.
/// </summary>
public partial class AddParameterDialog : Window
{
    public AddParameterDialog()
    {
        InitializeComponent();
        DataContext = new AddParameterViewModel();
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddParameterViewModel vm)
            Close(vm.Name.Trim());
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);
}
