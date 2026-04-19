using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace IcdFyIt.App.ViewModels;

public sealed class RecentFileItemViewModel
{
    public string Header { get; }
    public ICommand OpenCommand { get; }

    public RecentFileItemViewModel(string path, Func<string, Task> open)
    {
        Header = path;
        OpenCommand = new AsyncRelayCommand(() => open(path));
    }
}
