using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

/// <summary>
/// Modal dialog that displays validation issues (ICD-IF-191).
/// Open with <c>await new ValidationDialog().ShowDialogAsync(owner)</c>.
/// </summary>
public partial class ValidationDialog : Window
{
    public ValidationDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
