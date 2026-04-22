using Avalonia.Controls;
using IcdFyIt.App.Controls;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class HeaderTypesWindow : Window
{
    public HeaderTypesWindow()
    {
        InitializeComponent();

        HeaderTypesGrid.EditEnded += (_, _) =>
            (DataContext as HeaderTypesWindowViewModel)?.MarkEdited();
        IdsGrid.EditEnded += (_, _) =>
            (DataContext as HeaderTypesWindowViewModel)?.MarkEdited();
    }
}
