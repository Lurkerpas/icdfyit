using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Structure Fields pop-up dialog.
/// Manages Add/Remove operations on a single <see cref="StructureType"/>.
/// </summary>
public partial class StructureFieldsDialogViewModel : ObservableObject
{
    private readonly StructureType           _type;
    private readonly IReadOnlyList<DataType> _availableTypes;

    public StructureFieldsDialogViewModel(StructureType type, IReadOnlyList<DataType> availableTypes)
    {
        _type           = type;
        _availableTypes = availableTypes;
        Fields = new ObservableCollection<StructureFieldRowViewModel>(
            type.Fields.Select(f => new StructureFieldRowViewModel(f, availableTypes, AddField, RemoveField)));
    }

    public string TypeName => _type.Name;

    public ObservableCollection<StructureFieldRowViewModel> Fields { get; }

    [ObservableProperty]
    private StructureFieldRowViewModel? _selectedField;

    [RelayCommand]
    private void Add() => AddField();

    [RelayCommand]
    private void Remove()
    {
        if (SelectedField is null) return;
        RemoveField(SelectedField);
        SelectedField = null;
    }

    public void MoveField(StructureFieldRowViewModel dragged, StructureFieldRowViewModel target, bool above)
    {
        var fromIdx = Fields.IndexOf(dragged);
        var toIdx   = Fields.IndexOf(target);
        if (fromIdx < 0 || toIdx < 0 || fromIdx == toIdx) return;

        var insertAt = above ? toIdx : toIdx + 1;
        if (insertAt > fromIdx) insertAt--;

        Fields.Move(fromIdx, insertAt);
        _type.Fields.RemoveAt(fromIdx);
        _type.Fields.Insert(insertAt, dragged.Model);
    }

    private void AddField()
    {
        var f   = new StructureField { Name = "NewField" };
        _type.Fields.Add(f);
        Fields.Add(new StructureFieldRowViewModel(f, _availableTypes, AddField, RemoveField));
    }

    private void RemoveField(StructureFieldRowViewModel row)
    {
        _type.Fields.Remove(row.Model);
        Fields.Remove(row);
    }
}
