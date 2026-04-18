using Avalonia.Controls;
using Avalonia.Interactivity;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

/// <summary>
/// Modal dialog for creating a new Data Type.
/// Call <c>await dialog.ShowDialogAsync&lt;DataTypeCreationArgs?&gt;(owner)</c>.
/// </summary>
public partial class AddDataTypeDialog : Window
{
    public AddDataTypeDialog()
    {
        InitializeComponent();
        DataContext = new AddDataTypeViewModel();
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddDataTypeViewModel vm)
            Close(new DataTypeCreationArgs(vm.Name.Trim(), vm.SelectedKind));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);
}
