using CommunityToolkit.Mvvm.ComponentModel;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// Flat row wrapper around a <see cref="Memory"/> for spreadsheet-style display (ICD-IF-92 pattern).
/// All properties write through to the underlying model.
/// </summary>
public partial class MemoryRowViewModel : ObservableObject
{
    public Memory Model { get; }

    /// <summary>Invoked after any inline property change so the window can mark the model dirty.</summary>
    public Action? OnEdited { get; set; }

    public MemoryRowViewModel(Memory model) => Model = model;

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); }
    }

    public string NumericId
    {
        get => Model.NumericIdStr;
        set { Model.NumericIdStr = value; OnPropertyChanged(); }
    }

    public string? Mnemonic
    {
        get => Model.Mnemonic;
        set { Model.Mnemonic = value; OnPropertyChanged(); }
    }

    public string Size
    {
        get => Model.SizeStr;
        set { Model.SizeStr = value; OnPropertyChanged(); }
    }

    public string? Address
    {
        get => Model.Address;
        set { Model.Address = value; OnPropertyChanged(); }
    }

    public string? Description
    {
        get => Model.Description;
        set { Model.Description = value; OnPropertyChanged(); }
    }

    public string Alignment
    {
        get => Model.AlignmentStr;
        set { Model.AlignmentStr = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public bool IsWritable
    {
        get => Model.IsWritable;
        set { Model.IsWritable = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }

    public bool IsReadable
    {
        get => Model.IsReadable;
        set { Model.IsReadable = value; OnPropertyChanged(); OnEdited?.Invoke(); }
    }
}
