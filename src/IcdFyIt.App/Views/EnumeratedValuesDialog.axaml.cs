using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

/// <summary>
/// Pop-up dialog for editing enumerated values of an <see cref="IcdFyIt.Core.Model.EnumeratedType"/>.
/// Open with <c>await dialog.ShowDialog(owner)</c>.
/// </summary>
public partial class EnumeratedValuesDialog : Window
{
    public EnumeratedValuesDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e) { }
}
