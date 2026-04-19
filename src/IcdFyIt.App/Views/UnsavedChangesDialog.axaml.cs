using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

/// <summary>
/// Dialog shown when the user attempts to close or reset with unsaved changes (ICD-IF-180).
/// Call <c>await dialog.ShowDialog&lt;string?&gt;(owner)</c>.
/// Returns "save", "discard", or null (cancelled).
/// </summary>
public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)    => Close("save");
    private void OnDiscardClicked(object? sender, RoutedEventArgs e) => Close("discard");
    private void OnCancelClicked(object? sender, RoutedEventArgs e)  => Close(null);
}
