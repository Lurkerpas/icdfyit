using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Memories window — spreadsheet grid with filtering,
/// column visibility, and CSV import/export.
/// </summary>
public partial class MemoriesWindowViewModel : ObservableObject
{
    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier _changeNotifier;
    private readonly DirtyTracker _dirtyTracker;
    private readonly MainWindowViewModel _mainVm;

    private readonly ObservableCollection<MemoryRowViewModel> _rows = new();
    private readonly ObservableCollection<MemoryRowViewModel> _filteredRows = new();

    public MemoriesWindowViewModel(
        DataModelManager dataModelManager,
        ChangeNotifier changeNotifier,
        DirtyTracker dirtyTracker,
        MainWindowViewModel mainVm)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier   = changeNotifier;
        _dirtyTracker     = dirtyTracker;
        _mainVm           = mainVm;

        foreach (var m in _changeNotifier.Memories)
            _rows.Add(CreateRow(m));

        _changeNotifier.Memories.CollectionChanged += OnMemoriesCollectionChanged;
        RefreshFilteredRows();
    }

    // ── Collections ───────────────────────────────────────────────────────────

    public ObservableCollection<MemoryRowViewModel> FilteredRows => _filteredRows;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateCommand))]
    private MemoryRowViewModel? _selectedRow;

    // ── Filter ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _filterText = string.Empty;

    partial void OnFilterTextChanged(string value) => RefreshFilteredRows();

    // ── Delegates wired by the composition root / view ────────────────────────

    public Func<Task<string?>>? RequestAddMemory    { get; set; }
    public Func<Task<string?>>? RequestSaveCsvPath  { get; set; }
    public Func<Task<string?>>? RequestOpenCsvPath  { get; set; }

    // ── Add / Remove / Duplicate ──────────────────────────────────────────────

    [RelayCommand]
    private async Task Add()
    {
        if (RequestAddMemory is null) return;
        var name = await RequestAddMemory();
        if (name is null) return;
        var m = _dataModelManager.AddMemory(name);
        FilterText = string.Empty;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == m);
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove()
    {
        if (SelectedRow is null) return;
        _dataModelManager.RemoveMemory(SelectedRow.Model);
        SelectedRow = _filteredRows.Count > 0 ? _filteredRows[^1] : null;
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Duplicate()
    {
        if (SelectedRow is null) return;
        var copy = _dataModelManager.DuplicateMemory(SelectedRow.Model);
        FilterText = string.Empty;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == copy);
        _mainVm.NotifyModelEdited();
    }

    private bool HasSelection => SelectedRow is not null;

    // ── CSV export / import ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportCsv()
    {
        var path = await (RequestSaveCsvPath?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Name,NumericId,Mnemonic,Size,Address,Description,Alignment,IsWritable,IsReadable");
        foreach (var row in _filteredRows)
        {
            sb.AppendLine(
                $"\"{Esc(row.Name)}\",\"{row.NumericId}\"," +
                $"\"{Esc(row.Mnemonic)}\",\"{row.Size}\"," +
                $"\"{Esc(row.Address)}\",\"{Esc(row.Description)}\"," +
                $"\"{row.Alignment}\",\"{row.IsWritable}\",\"{row.IsReadable}\"");
        }
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

            var m   = _dataModelManager.AddMemory(f[0]);
            var row = _rows.FirstOrDefault(r => r.Model == m);
            if (row is null) continue;

            if (f.Length > 1 && !string.IsNullOrEmpty(f[1]))         row.NumericId   = f[1];
            if (f.Length > 2 && !string.IsNullOrEmpty(f[2]))         row.Mnemonic    = f[2];
            if (f.Length > 3 && !string.IsNullOrEmpty(f[3]))         row.Size        = f[3];
            if (f.Length > 4 && !string.IsNullOrEmpty(f[4]))         row.Address     = f[4];
            if (f.Length > 5 && !string.IsNullOrEmpty(f[5]))         row.Description = f[5];
            if (f.Length > 6 && !string.IsNullOrEmpty(f[6]))         row.Alignment   = f[6];
            if (f.Length > 7 && bool.TryParse(f[7], out var wr))     row.IsWritable  = wr;
            if (f.Length > 8 && bool.TryParse(f[8], out var rd))     row.IsReadable  = rd;
        }

        FilterText = string.Empty;
        _mainVm.NotifyModelEdited();
    }

    /// <summary>Reorders the memory list by moving <paramref name="draggedRow"/> to the position of <paramref name="targetRow"/>.</summary>
    public void MoveRow(MemoryRowViewModel draggedRow, MemoryRowViewModel targetRow)
    {
        var toIdx = _rows.IndexOf(targetRow);
        if (toIdx < 0 || !_rows.Contains(draggedRow)) return;
        _dataModelManager.MoveMemory(draggedRow.Model, toIdx);
        SelectedRow = draggedRow;
        _mainVm.NotifyModelEdited();
    }

    // ── Called by code-behind on inline cell edits / context menu open ────────

    public void MarkEdited()
    {
        _dirtyTracker.MarkDirty();
        _mainVm.NotifyModelEdited();
    }

    public void ForceCanEditNotify()
    {
        RemoveCommand.NotifyCanExecuteChanged();
        DuplicateCommand.NotifyCanExecuteChanged();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private MemoryRowViewModel CreateRow(Memory m)
    {
        var row = new MemoryRowViewModel(m);
        row.OnEdited = MarkEdited;
        return row;
    }

    private void OnMemoriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (Memory m in e.NewItems!)
                    _rows.Add(CreateRow(m));
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (Memory m in e.OldItems!)
                {
                    var row = _rows.FirstOrDefault(r => r.Model == m);
                    if (row is not null) _rows.Remove(row);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                if (e.OldItems?[0] is Memory movedM)
                {
                    var movedRow = _rows.FirstOrDefault(r => r.Model == movedM);
                    if (movedRow is not null)
                    {
                        _rows.Remove(movedRow);
                        _rows.Insert(Math.Clamp(e.NewStartingIndex, 0, _rows.Count), movedRow);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                _rows.Clear();
                foreach (var m in _changeNotifier.Memories)
                    _rows.Add(CreateRow(m));
                break;
        }
        RefreshFilteredRows();
    }

    private void RefreshFilteredRows()
    {
        var selected = SelectedRow;
        _filteredRows.Clear();
        IEnumerable<MemoryRowViewModel> q = _rows;
        if (!string.IsNullOrWhiteSpace(FilterText))
            q = q.Where(r =>
                r.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                (r.Mnemonic?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false));
        foreach (var row in q)
            _filteredRows.Add(row);
        SelectedRow = selected is not null && _filteredRows.Contains(selected) ? selected : null;
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
        return fields.ToArray();
    }
}
