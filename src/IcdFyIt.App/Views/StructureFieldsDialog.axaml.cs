using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

/// <summary>
/// Pop-up dialog for editing the fields of a <see cref="IcdFyIt.Core.Model.StructureType"/>.
/// Open with <c>await dialog.ShowDialog(owner)</c>.
/// </summary>
public partial class StructureFieldsDialog : Window
{
    public StructureFieldsDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e) { }
}
