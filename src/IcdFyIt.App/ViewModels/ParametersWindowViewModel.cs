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
/// ViewModel for the Parameters window — spreadsheet grid with filtering,
/// column visibility, CSV import/export, and an attributes pop-up for Kind and DataType
/// (ICD-IF-90 to ICD-IF-93, ICD-IF-150, ICD-IF-160).
/// </summary>
public partial class ParametersWindowViewModel : ObservableObject
{
    private const string AllKindLabel = "All";

    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier _changeNotifier;
    private readonly DirtyTracker _dirtyTracker;
    private readonly MainWindowViewModel _mainVm;

    private readonly ObservableCollection<ParameterRowViewModel> _rows = new();
    private readonly ObservableCollection<ParameterRowViewModel> _filteredRows = new();

    public ParametersWindowViewModel(
        DataModelManager dataModelManager,
        ChangeNotifier changeNotifier,
        DirtyTracker dirtyTracker,
        MainWindowViewModel mainVm)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier   = changeNotifier;
        _dirtyTracker     = dirtyTracker;
        _mainVm           = mainVm;

        foreach (var p in _changeNotifier.Parameters)
            _rows.Add(CreateRow(p));

        _changeNotifier.Parameters.CollectionChanged += OnParametersCollectionChanged;
        RefreshFilteredRows();
    }

    // ── Collections ───────────────────────────────────────────────────────────

    /// <summary>Filtered + sorted rows; bound to the main DataGrid.</summary>
    public ObservableCollection<ParameterRowViewModel> FilteredRows => _filteredRows;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditAttributesCommand))]
    private ParameterRowViewModel? _selectedRow;

    // ── Filter (ICD-IF-91) ────────────────────────────────────────────────────

    [ObservableProperty]
    private string _filterText = string.Empty;

    partial void OnFilterTextChanged(string value) => RefreshFilteredRows();

    public IReadOnlyList<string> AllKindFilters { get; } =
        new[] { AllKindLabel }.Concat(Enum.GetNames<ParameterKind>()).ToArray();

    [ObservableProperty]
    private string _selectedKindFilter = AllKindLabel;

    partial void OnSelectedKindFilterChanged(string value) => RefreshFilteredRows();

    // ── Delegates wired by the composition root / view ────────────────────────

    public Func<Task<string?>>? RequestAddParameter  { get; set; }
    public Func<Task<string?>>? RequestSaveCsvPath   { get; set; }
    public Func<Task<string?>>? RequestOpenCsvPath   { get; set; }

    // ── Add / Remove / Duplicate ──────────────────────────────────────────────

    [RelayCommand]
    private void Add()
    {
        var p = _dataModelManager.AddParameter("NewParameter");
        FilterText = string.Empty;
        SelectedKindFilter = AllKindLabel;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == p);
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand]
    private void Remove()
    {
        if (SelectedRow is null) return;
        _dataModelManager.RemoveParameter(SelectedRow.Model);
        SelectedRow = _filteredRows.Count > 0 ? _filteredRows[^1] : null;
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand]
    private void Duplicate()
    {
        if (SelectedRow is null) return;
        var copy = _dataModelManager.DuplicateParameter(SelectedRow.Model);
        FilterText = string.Empty;
        SelectedKindFilter = AllKindLabel;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == copy);
        _mainVm.NotifyModelEdited();
    }

    // ── Edit attributes (pop-up dialog) ──────────────────────────────────────

    /// <summary>Wired by App.axaml.cs to open the <see cref="Views.ParameterAttributesDialog"/>.</summary>
    public Func<Parameter, Task>? RequestEditAttributes { get; set; }

    public bool CanEditAttributes => SelectedRow is not null;

    [RelayCommand(CanExecute = nameof(CanEditAttributes))]
    private async Task EditAttributes()
    {
        if (SelectedRow is null) return;
        await (RequestEditAttributes?.Invoke(SelectedRow.Model) ?? Task.CompletedTask);
        SelectedRow.RefreshAfterAttributesEdit();
        MarkEdited();
    }

    // ── CSV export / import (ICD-IF-160) ─────────────────────────────────────

    [RelayCommand]
    private async Task ExportCsv()
    {
        var path = await (RequestSaveCsvPath?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Name,Mnemonic,Kind,NumericId,ShortDescription,DataType,Formula,HexValue");
        foreach (var row in _filteredRows)
        {
            sb.AppendLine(
                $"\"{Esc(row.Name)}\",\"{Esc(row.Mnemonic)}\"," +
                $"\"{row.Kind}\",\"{row.NumericId}\"," +
                $"\"{Esc(row.ShortDescription)}\",\"{Esc(row.DataTypeName)}\"," +
                $"\"{Esc(row.Formula)}\",\"{Esc(row.HexValue)}\"");
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
            if (_rows.Any(r => r.Name == f[0])) continue;   // skip duplicates

            var p   = _dataModelManager.AddParameter(f[0]);
            var row = _rows.FirstOrDefault(r => r.Model == p);
            if (row is null) continue;

            if (f.Length > 1 && !string.IsNullOrEmpty(f[1]))  row.Mnemonic         = f[1];
            if (f.Length > 2 && Enum.TryParse<ParameterKind>(f[2], out var kind))
            {
                row.Model.Kind = kind;
                row.RefreshAfterAttributesEdit();
            }
            if (f.Length > 3 && int.TryParse(f[3], out var id))   row.NumericId        = id;
            if (f.Length > 4 && !string.IsNullOrEmpty(f[4]))  row.ShortDescription = f[4];
            if (f.Length > 6 && !string.IsNullOrEmpty(f[6]))  row.Formula          = f[6];
            if (f.Length > 7 && !string.IsNullOrEmpty(f[7]))  row.HexValue         = f[7];
        }

        FilterText = string.Empty;
        SelectedKindFilter = AllKindLabel;
        _mainVm.NotifyModelEdited();
    }

    /// <summary>Reorders the parameter list by moving <paramref name="draggedRow"/> to the position of <paramref name="targetRow"/>.</summary>
    public void MoveRow(ParameterRowViewModel draggedRow, ParameterRowViewModel targetRow)
    {
        var toIdx = _rows.IndexOf(targetRow);
        if (toIdx < 0 || !_rows.Contains(draggedRow)) return;
        _dataModelManager.MoveParameter(draggedRow.Model, toIdx);
        // _rows is updated via OnParametersCollectionChanged(Move)
        SelectedRow = draggedRow;
        _mainVm.NotifyModelEdited();
    }

    // ── Called by code-behind on inline cell edits / context menu open ────────

    public void MarkEdited()
    {
        _dirtyTracker.MarkDirty();
        _mainVm.NotifyModelEdited();
    }

    /// <summary>Forces re-evaluation of context-menu visibility properties.
    /// Called by the view when the context menu opens.</summary>
    public void ForceCanEditNotify()
    {
        EditAttributesCommand.NotifyCanExecuteChanged();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private ParameterRowViewModel CreateRow(Parameter p)
    {
        var row = new ParameterRowViewModel(p);
        row.OnEdited = MarkEdited;
        return row;
    }

    private void OnParametersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (Parameter p in e.NewItems!)
                    _rows.Add(CreateRow(p));
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (Parameter p in e.OldItems!)
                {
                    var row = _rows.FirstOrDefault(r => r.Model == p);
                    if (row is not null) _rows.Remove(row);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                if (e.OldItems?[0] is Parameter movedP)
                {
                    var movedRow = _rows.FirstOrDefault(r => r.Model == movedP);
                    if (movedRow is not null)
                    {
                        _rows.Remove(movedRow);
                        _rows.Insert(Math.Clamp(e.NewStartingIndex, 0, _rows.Count), movedRow);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                _rows.Clear();
                foreach (var p in _changeNotifier.Parameters)
                    _rows.Add(CreateRow(p));
                break;
        }
        RefreshFilteredRows();
    }

    private void RefreshFilteredRows()
    {
        var selected = SelectedRow;
        _filteredRows.Clear();
        IEnumerable<ParameterRowViewModel> q = _rows;
        if (!string.IsNullOrWhiteSpace(FilterText))
            q = q.Where(r =>
                r.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                (r.Mnemonic?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false));
        if (SelectedKindFilter != AllKindLabel)
            q = q.Where(r => r.Kind == SelectedKindFilter);
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
        return [.. fields];
    }
}

