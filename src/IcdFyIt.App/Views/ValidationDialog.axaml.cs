using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using IcdFyIt.App.ViewModels;
using System.Linq;
using Serilog;

namespace IcdFyIt.App.Views;

/// <summary>
/// Modal dialog that displays validation issues (ICD-IF-191).
/// Open with <c>await new ValidationDialog().ShowDialog(owner)</c>.
/// </summary>
public partial class ValidationDialog : Window
{
    public ValidationDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private async void OnCopyToClipboardClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not ValidationDialogViewModel vm) return;
            var text = string.Join(System.Environment.NewLine,
                vm.Issues.Select(i => i.Message));
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to copy validation issues to clipboard");
        }
    }
}
