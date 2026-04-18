using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Validation dialog (ICD-IF-191).
/// Exposes issues as a copyable list.
/// </summary>
public partial class ValidationDialogViewModel : ObservableObject
{
    public ValidationDialogViewModel(IReadOnlyList<ValidationIssue> issues)
    {
        Issues = new ObservableCollection<ValidationIssue>(issues);
    }

    public ObservableCollection<ValidationIssue> Issues { get; }

    /// <summary>Copies all issue messages to the clipboard, one per line.</summary>
    [RelayCommand]
    private void CopyToClipboard() => throw new NotImplementedException();
}
