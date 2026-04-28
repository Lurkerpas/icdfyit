using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace IcdFyIt.App.ViewModels;

public sealed class RecentFileItemViewModel
{
    private const int MaxDisplayLength = 32;

    public string Header { get; }
    public string FullPath { get; }
    public ICommand OpenCommand { get; }

    public RecentFileItemViewModel(string path, Func<string, Task> open)
    {
        FullPath = path;
        Header = path.Length <= MaxDisplayLength
            ? path
            : "\u2026" + path[^(MaxDisplayLength - 1)..];
        OpenCommand = new AsyncRelayCommand(() => open(path));
    }
}
