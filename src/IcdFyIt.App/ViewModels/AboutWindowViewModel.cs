using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the About modal window (ICD-IF-51).
/// </summary>
public partial class AboutWindowViewModel : ObservableObject
{
    public string ApplicationName => "icdfyit";

    public string Version => typeof(AboutWindowViewModel).Assembly
        .GetName().Version?.ToString() ?? "0.0.0";

    public string License => "AGPL-3.0";

    [RelayCommand]
    private void Close() => throw new NotImplementedException();
}
