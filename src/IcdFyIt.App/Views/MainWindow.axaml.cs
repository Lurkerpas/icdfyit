using Avalonia.Controls;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Nothing to wire here — delegates are set in App.axaml.cs composition root.
    }
}
