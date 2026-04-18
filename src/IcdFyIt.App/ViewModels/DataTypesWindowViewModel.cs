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
/// ViewModel for the Data Types window — spreadsheet grid with filtering,
/// column visibility, CSV import/export, and a detail panel for complex types
/// (ICD-IF-100, ICD-IF-110, ICD-IF-120, ICD-IF-130, ICD-IF-150, ICD-IF-160).
/// </summary>
public partial class DataTypesWindowViewModel : ObservableObject
{
    private const string AllKindLabel = "All";

    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier _changeNotifier;
    private readonly DirtyTracker _dirtyTracker;
    private readonly MainWindowViewModel _mainVm;

    private readonly ObservableCollection<DataTypeRowViewModel> _rows = new();
    private readonly ObservableCollection<DataTypeRowViewModel> _filteredRows = new();

    public DataTypesWindowViewModel(
        DataModelManager dataModelManager,
        ChangeNotifier changeNotifier,
        DirtyTracker dirtyTracker,
        MainWindowViewModel mainVm)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier   = changeNotifier;
        _dirtyTracker     = dirtyTracker;
        _mainVm           = mainVm;

        foreach (var dt in _changeNotifier.DataTypes)
            _rows.Add(CreateRow(dt));

        _changeNotifier.DataTypes.CollectionChanged += OnDataTypesCollectionChanged;
        RefreshFilteredRows();
    }

    // ── Collections ───────────────────────────────────────────────────────────

    /// <summary>Filtered + sorted rows; bound to the main DataGrid.</summary>
    public ObservableCollection<DataTypeRowViewModel> FilteredRows => _filteredRows;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditValues))]
    [NotifyPropertyChangedFor(nameof(CanEditFields))]
    [NotifyCanExecuteChangedFor(nameof(EditStructureFieldsCommand))]
    private DataTypeRowViewModel? _selectedRow;

    partial void OnSelectedRowChanged(DataTypeRowViewModel? value) { }

    // ── Filter (ICD-IF-110) ───────────────────────────────────────────────────

    [ObservableProperty]
    private string _filterText = string.Empty;

    partial void OnFilterTextChanged(string value) => RefreshFilteredRows();

    public IReadOnlyList<string> AllKindFilters { get; } =
        new[] { AllKindLabel }.Concat(Enum.GetNames<BaseType>()).ToArray();

    [ObservableProperty]
    private string _selectedKindFilter = AllKindLabel;

    partial void OnSelectedKindFilterChanged(string value) => RefreshFilteredRows();

    // ── Delegates wired by the view ───────────────────────────────────────────

    public Func<Task<DataTypeCreationArgs?>>? RequestAddDataType { get; set; }
    public Func<Task<string?>>? RequestSaveCsvPath { get; set; }
    public Func<Task<string?>>? RequestOpenCsvPath { get; set; }

    // ── Add / Remove / Duplicate ──────────────────────────────────────────────

    [RelayCommand]
    private async Task Add()
    {
        var args = await (RequestAddDataType?.Invoke() ?? Task.FromResult<DataTypeCreationArgs?>(null));
        if (args is null) return;
        var dt = _dataModelManager.AddDataType(args.Name, args.Kind);
        FilterText = string.Empty;
        SelectedKindFilter = AllKindLabel;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == dt);
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand]
    private void Remove()
    {
        if (SelectedRow is null) return;
        _dataModelManager.RemoveDataType(SelectedRow.Model);
        SelectedRow = _filteredRows.Count > 0 ? _filteredRows[^1] : null;
        _mainVm.NotifyModelEdited();
    }

    [RelayCommand]
    private void Duplicate()
    {
        if (SelectedRow is null) return;
        var copy = _dataModelManager.DuplicateDataType(SelectedRow.Model);
        FilterText = string.Empty;
        SelectedKindFilter = AllKindLabel;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == copy);
        _mainVm.NotifyModelEdited();
    }

    // ── CSV export / import (ICD-IF-160) ─────────────────────────────────────

    [RelayCommand]
    private async Task ExportCsv()
    {
        var path = await (RequestSaveCsvPath?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Name,Kind,Endianness,BitSize,RangeMin,RangeMax,Unit,Calibration,Summary");
        foreach (var row in _filteredRows)
        {
            sb.AppendLine(
                $"\"{Esc(row.Name)}\",\"{row.Kind}\"," +
                $"\"{row.Endianness}\",\"{row.BitSize}\"," +
                $"\"{row.RangeMin?.ToString(CultureInfo.InvariantCulture)}\"," +
                $"\"{row.RangeMax?.ToString(CultureInfo.InvariantCulture)}\"," +
                $"\"{Esc(row.Unit)}\",\"{Esc(row.CalibrationFormula)}\"," +
                $"\"{Esc(row.Summary)}\"");
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
            if (f.Length < 2 || string.IsNullOrWhiteSpace(f[0])) continue;
            if (!Enum.TryParse<BaseType>(f[1], out var kind)) continue;
            if (_rows.Any(r => r.Name == f[0])) continue;   // skip duplicates

            var dt  = _dataModelManager.AddDataType(f[0], kind);
            var row = _rows.FirstOrDefault(r => r.Model == dt);
            if (row is null) continue;

            if (f.Length > 2 && Enum.TryParse<Endianness>(f[2], out var end)) row.Endianness = end;
            if (f.Length > 3 && int.TryParse(f[3], out var bs))              row.BitSize     = bs;
            if (f.Length > 4 && double.TryParse(f[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var rmin)) row.RangeMin = rmin;
            if (f.Length > 5 && double.TryParse(f[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var rmax)) row.RangeMax = rmax;
            if (f.Length > 6 && !string.IsNullOrEmpty(f[6])) row.Unit                = f[6];
            if (f.Length > 7 && !string.IsNullOrEmpty(f[7])) row.CalibrationFormula  = f[7];
        }

        FilterText = string.Empty;
        SelectedKindFilter = AllKindLabel;
        _mainVm.NotifyModelEdited();
    }

    // ── Edit enumerated values (pop-up dialog) ───────────────────────────────

    /// <summary>Wired by App.axaml.cs to open the <see cref="Views.EnumeratedValuesDialog"/>.</summary>
    public Func<EnumeratedType, Task>? RequestEditEnumeratedValues { get; set; }

    public bool CanEditValues => SelectedRow?.Model is EnumeratedType;

    [RelayCommand(CanExecute = nameof(CanEditValues))]
    private async Task EditValues()
    {
        if (SelectedRow?.Model is not EnumeratedType et) return;
        await (RequestEditEnumeratedValues?.Invoke(et) ?? Task.CompletedTask);
        SelectedRow.RefreshSummary();
        MarkEdited();
    }

    // ── Edit structure fields (pop-up dialog) ──────────────────────────────────

    /// <summary>Wired by App.axaml.cs to open the <see cref="Views.StructureFieldsDialog"/>.</summary>
    public Func<StructureType, Task>? RequestEditStructureFields { get; set; }

    public bool CanEditFields => SelectedRow?.Model is StructureType;

    [RelayCommand(CanExecute = nameof(CanEditFields))]
    private async Task EditStructureFields()
    {
        if (SelectedRow?.Model is not StructureType st) return;
        await (RequestEditStructureFields?.Invoke(st) ?? Task.CompletedTask);
        SelectedRow.RefreshSummary();
        MarkEdited();
    }

    // ── Called by code-behind on inline cell edits ────────────────────────────

    public void MarkEdited()
    {
        _dirtyTracker.MarkDirty();
        _mainVm.NotifyModelEdited();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private DataTypeRowViewModel CreateRow(DataType dt)
    {
        var row = new DataTypeRowViewModel(dt);
        row.OnEdited = MarkEdited;
        return row;
    }

    private void OnDataTypesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (DataType dt in e.NewItems!)
                    _rows.Add(CreateRow(dt));
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (DataType dt in e.OldItems!)
                {
                    var row = _rows.FirstOrDefault(r => r.Model == dt);
                    if (row is not null) _rows.Remove(row);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                _rows.Clear();
                foreach (var dt in _changeNotifier.DataTypes)
                    _rows.Add(CreateRow(dt));
                break;
        }
        RefreshFilteredRows();
    }

    private void RefreshFilteredRows()
    {
        var selected = SelectedRow;
        _filteredRows.Clear();
        IEnumerable<DataTypeRowViewModel> q = _rows;
        if (!string.IsNullOrWhiteSpace(FilterText))
            q = q.Where(r => r.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
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

