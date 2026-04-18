using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Infrastructure;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Export window (ICD-DES §5.4).
/// </summary>
public partial class ExportWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly AppOptions _options;

    public ExportWindowViewModel(DataModelManager dataModelManager, AppOptions options)
    {
        _dataModelManager = dataModelManager;
        _options = options;
    }

    /// <summary>Template sets available for selection, sourced from AppOptions.</summary>
    public IReadOnlyList<TemplateSetConfig> TemplateSets => _options.TemplateSets;

    [ObservableProperty]
    private TemplateSetConfig? _selectedTemplateSet;

    [ObservableProperty]
    private string _outputFolder = string.Empty;

    [RelayCommand]
    private void BrowseOutputFolder() => throw new NotImplementedException();

    [RelayCommand]
    private void Export() => throw new NotImplementedException();
}
