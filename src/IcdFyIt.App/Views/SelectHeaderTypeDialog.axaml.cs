using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

public partial class SelectHeaderTypeDialog : Window
{
    public SelectHeaderTypeDialog()
    {
        InitializeComponent();
    }

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SelectHeaderTypeDialogViewModel vm)
            vm.Clear();
        Close();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
