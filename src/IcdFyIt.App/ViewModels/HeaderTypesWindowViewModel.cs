using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Header Types window (ICD-IF-210, ICD-IF-220).
/// Provides a filterable grid of Header Types and a detail panel for their ID entries.
/// </summary>
public partial class HeaderTypesWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier   _changeNotifier;
    private readonly MainWindowViewModel _mainVm;

    private readonly ObservableCollection<HeaderTypeRowViewModel> _rows        = new();
    private readonly ObservableCollection<HeaderTypeRowViewModel> _filteredRows = new();

    public HeaderTypesWindowViewModel(
        DataModelManager     dataModelManager,
        ChangeNotifier       changeNotifier,
        MainWindowViewModel  mainVm)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier   = changeNotifier;
        _mainVm           = mainVm;

        foreach (var ht in _changeNotifier.HeaderTypes)
            _rows.Add(CreateRow(ht));

        _changeNotifier.HeaderTypes.CollectionChanged += OnHeaderTypesCollectionChanged;
        RefreshFilteredRows();
    }

    // ── Collections ───────────────────────────────────────────────────────────

    public ObservableCollection<HeaderTypeRowViewModel>    FilteredRows { get; } = new();
    public ObservableCollection<HeaderTypeIdRowViewModel>  IdRows       { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdSectionTitle))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddIdCommand))]
    private HeaderTypeRowViewModel? _selectedRow;

    partial void OnSelectedRowChanged(HeaderTypeRowViewModel? value) => RebuildIdRows(value?.Model);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveIdCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditIdDataTypeCommand))]
    private HeaderTypeIdRowViewModel? _selectedIdRow;

    // ── Filter ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _filterText = string.Empty;

    partial void OnFilterTextChanged(string value) => RefreshFilteredRows();

    // ── ID section title ──────────────────────────────────────────────────────

    public string IdSectionTitle => SelectedRow is not null
        ? $"IDs \u2014 {SelectedRow.Name}"
        : "IDs";

    // ── Delegates wired by the view ───────────────────────────────────────────

    /// <summary>Opens the data-type picker dialog for a given ID entry.</summary>
    public Func<HeaderTypeId, Task>? RequestEditIdDataType { get; set; }

    // ── Header Type CRUD ──────────────────────────────────────────────────────

    [RelayCommand]
    private void Add()
    {
        var ht = _dataModelManager.AddHeaderType("NewHeaderType");
        FilterText = string.Empty;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == ht);
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void Remove()
    {
        if (SelectedRow is null) return;
        _dataModelManager.RemoveHeaderType(SelectedRow.Model);
        SelectedRow = _filteredRows.Count > 0 ? _filteredRows[0] : null;
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void Duplicate()
    {
        if (SelectedRow is null) return;
        var copy = _dataModelManager.DuplicateHeaderType(SelectedRow.Model);
        FilterText = string.Empty;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == copy);
        _mainVm.NotifyModelEdited();
    }

    private bool HasSelectedRow => SelectedRow is not null;

    // ── ID entry CRUD (direct model mutation, no undo) ────────────────────────

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void AddId()
    {
        if (SelectedRow is null) return;
        var entry = new HeaderTypeId { Name = "NewId" };
        SelectedRow.Model.Ids.Add(entry);
        var row = new HeaderTypeIdRowViewModel(entry) { OnEdited = _mainVm.NotifyModelEdited };
        IdRows.Add(row);
        SelectedIdRow = row;
        SelectedRow.NotifyIdCountChanged();
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedIdRow))]
    private void RemoveId()
    {
        if (SelectedRow is null || SelectedIdRow is null) return;
        SelectedRow.Model.Ids.Remove(SelectedIdRow.Model);
        IdRows.Remove(SelectedIdRow);
        SelectedIdRow = IdRows.Count > 0 ? IdRows[^1] : null;
        SelectedRow.NotifyIdCountChanged();
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedIdRow))]
    private async Task EditIdDataType()
    {
        if (SelectedIdRow is null || RequestEditIdDataType is null) return;
        await RequestEditIdDataType(SelectedIdRow.Model);
        SelectedIdRow.RefreshDataType();
        _mainVm.NotifyModelEdited();
    }

    private bool HasSelectedIdRow => SelectedIdRow is not null;

    // ── Dirty notification ────────────────────────────────────────────────────

    public void MarkEdited() => _mainVm.NotifyModelEdited();

    // ── Collection sync ───────────────────────────────────────────────────────

    private void OnHeaderTypesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (HeaderType ht in e.NewItems!)
                    _rows.Add(CreateRow(ht));
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (HeaderType ht in e.OldItems!)
                {
                    var row = _rows.FirstOrDefault(r => r.Model == ht);
                    if (row is not null) _rows.Remove(row);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                _rows.Clear();
                foreach (var ht in _changeNotifier.HeaderTypes)
                    _rows.Add(CreateRow(ht));
                break;
        }
        RefreshFilteredRows();
    }

    private void RefreshFilteredRows()
    {
        var filter = FilterText.Trim();
        FilteredRows.Clear();
        foreach (var row in _rows)
        {
            if (filter.Length == 0
                || row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (row.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true))
                FilteredRows.Add(row);
        }
        if (SelectedRow is null || !FilteredRows.Contains(SelectedRow))
            SelectedRow = FilteredRows.Count > 0 ? FilteredRows[0] : null;
    }

    private void RebuildIdRows(HeaderType? ht)
    {
        IdRows.Clear();
        SelectedIdRow = null;
        if (ht is null) return;
        foreach (var id in ht.Ids)
            IdRows.Add(new HeaderTypeIdRowViewModel(id) { OnEdited = _mainVm.NotifyModelEdited });
    }

    private static HeaderTypeRowViewModel CreateRow(HeaderType ht)
        => new(ht);
}
