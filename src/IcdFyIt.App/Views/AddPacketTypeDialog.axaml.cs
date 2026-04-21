using Avalonia.Controls;
using Avalonia.Interactivity;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

/// <summary>
/// Modal dialog for creating a new Packet Type — asks for a name only.
/// Call <c>await dialog.ShowDialog&lt;string?&gt;(owner)</c>; returns trimmed name or null.
/// </summary>
public partial class AddPacketTypeDialog : Window
{
    public AddPacketTypeDialog()
    {
        InitializeComponent();
        DataContext = new AddParameterViewModel("Packet");
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddParameterViewModel vm)
            Close(vm.Name.Trim());
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);
}
