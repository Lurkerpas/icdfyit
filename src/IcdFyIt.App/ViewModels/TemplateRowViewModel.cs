using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Row view-model for one <see cref="TemplateConfig"/> in the Options → Template Sets detail panel.
/// All edits go directly to the underlying config object so no explicit ToModel() is needed.
/// </summary>
public partial class TemplateRowViewModel : ObservableObject
{
    private readonly TemplateConfig _model;

    /// <summary>Optional delegate for browsing the file system to pick a template file.</summary>
    public Func<string?, Task<string?>>? RequestBrowseFile { get; set; }

    public TemplateRowViewModel(TemplateConfig model)
    {
        _model = model;
    }

    public TemplateConfig Model => _model;

    public string Name
    {
        get => _model.Name;
        set { _model.Name = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _model.Description;
        set { _model.Description = value; OnPropertyChanged(); }
    }

    public string FilePath
    {
        get => _model.FilePath;
        set { _model.FilePath = value; OnPropertyChanged(); }
    }

    public string OutputNamePattern
    {
        get => _model.OutputNamePattern;
        set { _model.OutputNamePattern = value; OnPropertyChanged(); }
    }

    /// <summary>Opens the file browser and sets <see cref="FilePath"/> if a file is chosen.</summary>
    [RelayCommand]
    private async Task Browse()
    {
        if (RequestBrowseFile is null) return;
        var path = await RequestBrowseFile(FilePath);
        if (path is not null)
            FilePath = path;
    }
}
