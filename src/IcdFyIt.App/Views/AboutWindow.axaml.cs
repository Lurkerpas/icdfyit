using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IcdFyIt.App.Views;

/// <summary>
/// Modal About window (ICD-IF-51).
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
