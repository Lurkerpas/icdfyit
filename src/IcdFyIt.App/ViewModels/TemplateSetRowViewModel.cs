using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Row view-model for one <see cref="TemplateSetConfig"/> in the Options → Template Sets list.
/// Edits go directly to the underlying config object.
/// </summary>
public partial class TemplateSetRowViewModel : ObservableObject
{
    private readonly TemplateSetConfig _model;

    public TemplateSetRowViewModel(TemplateSetConfig model)
    {
        _model = model;
    }

    public TemplateSetConfig Model => _model;

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
}
