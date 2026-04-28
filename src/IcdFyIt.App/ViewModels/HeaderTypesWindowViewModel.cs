using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;
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
        _dataModelManager.HeaderTypeIdsChanged += OnHeaderTypeIdsChanged;
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

    /// <summary>Wired by the view to open a save-file dialog for CSV export.</summary>
    public Func<Task<string?>>? RequestSaveCsvPath { get; set; }

    /// <summary>Wired by the view to open an open-file dialog for CSV import.</summary>
    public Func<Task<string?>>? RequestOpenCsvPath { get; set; }

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

    // ── CSV import / export (ICD-IF-160) ──────────────────────────────────────

    [RelayCommand]
    private async Task ExportCsv()
    {
        var path = await (RequestSaveCsvPath?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Name,Mnemonic,Description");
        foreach (var row in FilteredRows)
            sb.AppendLine($"\"{Esc(row.Name)}\",\"{Esc(row.Mnemonic)}\",\"{Esc(row.Description)}\"");
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        var path = await (RequestOpenCsvPath?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;

        var lines = await File.ReadAllLinesAsync(path);
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = ParseCsvLine(line);
            if (f.Length < 1 || string.IsNullOrWhiteSpace(f[0])) continue;
            if (_rows.Any(r => r.Name == f[0])) continue;

            var ht  = _dataModelManager.AddHeaderType(f[0]);
            var row = _rows.FirstOrDefault(r => r.Model == ht);
            if (row is null) continue;
            if (f.Length > 1 && !string.IsNullOrEmpty(f[1]))
                row.Mnemonic = f[1];
            if (f.Length > 2 && !string.IsNullOrEmpty(f[2]))
                row.Description = f[2];
        }

        FilterText = string.Empty;
        _mainVm.NotifyModelEdited();
    }

    private static string Esc(string? s) => s?.Replace("\"", "\"\"") ?? string.Empty;

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQ    = false;
        var cur    = new StringBuilder();
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQ && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else inQ = !inQ;
            }
            else if (c == ',' && !inQ) { fields.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        fields.Add(cur.ToString());
        return [.. fields];
    }

    // ── ID entry CRUD (routed via DataModelManager for undo support, NC-07) ────

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void AddId()
    {
        if (SelectedRow is null) return;
        _dataModelManager.AddHeaderTypeId(SelectedRow.Model);
        // IdRows rebuilt via HeaderTypeIdsChanged event
        SelectedIdRow = IdRows.Count > 0 ? IdRows[^1] : null;
        SelectedRow.NotifyIdCountChanged();
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedIdRow))]
    private void RemoveId()
    {
        if (SelectedRow is null || SelectedIdRow is null) return;
        _dataModelManager.RemoveHeaderTypeId(SelectedRow.Model, SelectedIdRow.Model);
        // IdRows rebuilt via HeaderTypeIdsChanged event
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

    // ── Sub-entity sync ───────────────────────────────────────────────────────

    /// <summary>Syncs <see cref="IdRows"/> when undo/redo mutates the selected header type's Ids list.</summary>
    private void OnHeaderTypeIdsChanged(HeaderType ht)
    {
        if (SelectedRow?.Model != ht) return;
        RebuildIdRows(ht);
        SelectedRow.NotifyIdCountChanged();
    }

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
                || row.Description.Contains(filter, StringComparison.OrdinalIgnoreCase))
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
