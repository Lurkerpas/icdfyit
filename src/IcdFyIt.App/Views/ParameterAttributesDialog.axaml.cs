using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

/// <summary>
/// Pop-up dialog for editing a Parameter's Kind and DataType reference — controls that
/// are unsafe inside a DataGrid cell template on Linux/X11.
/// Open with <c>await dialog.ShowDialog(owner)</c>.
/// </summary>
public partial class ParameterAttributesDialog : Window
{
    public ParameterAttributesDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
