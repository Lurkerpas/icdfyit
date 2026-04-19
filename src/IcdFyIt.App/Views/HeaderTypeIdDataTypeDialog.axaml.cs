using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

public partial class HeaderTypeIdDataTypeDialog : Window
{
    public HeaderTypeIdDataTypeDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
