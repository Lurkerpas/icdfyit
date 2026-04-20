using Avalonia.Controls;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class OptionsWindow : Window
{
    public OptionsWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is OptionsWindowViewModel vm)
            vm.RequestClose = Close;
    }
}
