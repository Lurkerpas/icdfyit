using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

/// <summary>
/// Pop-up dialog for editing an <see cref="IcdFyIt.Core.Model.ArrayType"/>:
/// element type, size-field endianness/bit-size, and min/max array length.
/// Open with <c>await dialog.ShowDialog(owner)</c>.
/// </summary>
public partial class ArrayTypeDialog : Window
{
    public ArrayTypeDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
