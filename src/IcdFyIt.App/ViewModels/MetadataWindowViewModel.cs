using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;

namespace IcdFyIt.App.ViewModels;

/// <summary>
/// ViewModel for the Metadata window.
/// Displays built-in metadata and user-defined name:value metadata fields.
/// </summary>
public partial class MetadataWindowViewModel : ObservableObject
{
    private static readonly MetadataBuiltInField[] BuiltInOrder =
    [
        MetadataBuiltInField.Name,
        MetadataBuiltInField.Version,
        MetadataBuiltInField.Date,
        MetadataBuiltInField.Status,
        MetadataBuiltInField.Description,
    ];

    private readonly DataModelManager _dataModelManager;
    private readonly ChangeNotifier _changeNotifier;
    private readonly MainWindowViewModel _mainVm;

    private readonly ObservableCollection<MetadataRowViewModel> _rows = new();
    private readonly ObservableCollection<MetadataRowViewModel> _filteredRows = new();
    private bool _isCommittingEdits;

    public MetadataWindowViewModel(
        DataModelManager dataModelManager,
        ChangeNotifier changeNotifier,
        MainWindowViewModel mainVm)
    {
        _dataModelManager = dataModelManager;
        _changeNotifier = changeNotifier;
        _mainVm = mainVm;

        _changeNotifier.MetadataFields.CollectionChanged += OnMetadataFieldsChanged;
        _dataModelManager.MetadataChanged += OnMetadataChanged;

        RebuildRows();
        RefreshFilteredRows();
    }

    public ObservableCollection<MetadataRowViewModel> FilteredRows => _filteredRows;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveFieldCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateFieldCommand))]
    private MetadataRowViewModel? _selectedRow;

    [ObservableProperty]
    private string _filterText = string.Empty;

    partial void OnFilterTextChanged(string value) => RefreshFilteredRows();

    public Func<Task<string?>>? RequestSaveCsvPath { get; set; }
    public Func<Task<string?>>? RequestOpenCsvPath { get; set; }

    [RelayCommand]
    private void AddField()
    {
        var field = _dataModelManager.AddMetadataField("NewField");
        _mainVm.NotifyModelEdited();
        FilterText = string.Empty;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == field);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCustomRow))]
    private void RemoveField()
    {
        if (SelectedRow?.Model is null || SelectedRow.IsBuiltIn) return;
        _dataModelManager.RemoveMetadataField(SelectedRow.Model);
        _mainVm.NotifyModelEdited();
        SelectedRow = _filteredRows.FirstOrDefault(r => !r.IsBuiltIn);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCustomRow))]
    private void DuplicateField()
    {
        if (SelectedRow?.Model is null || SelectedRow.IsBuiltIn) return;
        var copy = _dataModelManager.DuplicateMetadataField(SelectedRow.Model);
        _mainVm.NotifyModelEdited();
        FilterText = string.Empty;
        SelectedRow = _filteredRows.FirstOrDefault(r => r.Model == copy);
    }

    private bool HasSelectedCustomRow => SelectedRow is { IsBuiltIn: false };

    [RelayCommand]
    private async Task ExportCsv()
    {
        var path = await (RequestSaveCsvPath?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Name,Value,IsBuiltIn");

        foreach (var row in _filteredRows)
            sb.AppendLine($"\"{Esc(row.Name)}\",\"{Esc(row.Value)}\",\"{row.IsBuiltIn}\"");

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        var path = await (RequestOpenCsvPath?.Invoke() ?? Task.FromResult<string?>(null));
        if (path is null) return;

        var lines = await File.ReadAllLinesAsync(path);
        var changed = false;

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = ParseCsvLine(line);
            if (f.Length < 2 || string.IsNullOrWhiteSpace(f[0])) continue;

            var name = f[0].Trim();
            var value = f[1];

            var isBuiltIn = f.Length > 2 && bool.TryParse(f[2], out var b) && b;
            if (MetadataRowViewModel.TryParseBuiltInField(name, out var builtInField) || isBuiltIn)
            {
                if (!MetadataRowViewModel.TryParseBuiltInField(name, out builtInField))
                    continue;
                _dataModelManager.SetMetadataValue(builtInField, value);
                changed = true;
                continue;
            }

            var existing = _changeNotifier.MetadataFields
                .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var field = _dataModelManager.AddMetadataField(name);
                _dataModelManager.UpdateMetadataFieldValue(field, value);
            }
            else
            {
                _dataModelManager.UpdateMetadataFieldValue(existing, value);
            }
            changed = true;
        }

        if (!changed) return;

        FilterText = string.Empty;
        _mainVm.NotifyModelEdited();
    }

    public void MoveRow(MetadataRowViewModel draggedRow, MetadataRowViewModel targetRow)
    {
        if (draggedRow.Model is null || draggedRow.IsBuiltIn) return;

        var targetIndexInRows = _rows.IndexOf(targetRow);
        if (targetIndexInRows < 0) return;

        var toIndex = _rows.Take(targetIndexInRows).Count(r => !r.IsBuiltIn);
        _dataModelManager.MoveMetadataField(draggedRow.Model, toIndex);
        _mainVm.NotifyModelEdited();
        SelectedRow = draggedRow;
    }

    public void CommitEdits()
    {
        if (_isCommittingEdits)
            return;

        _isCommittingEdits = true;

        var changed = false;

        try
        {
            var builtInRows = _rows.Where(r => r.IsBuiltIn).ToList();
            var customRows = _rows.Where(r => !r.IsBuiltIn && r.Model is not null).ToList();

            foreach (var row in builtInRows)
            {
                var field = row.BuiltInField!.Value;
                var canonical = MetadataRowViewModel.BuiltInName(field);
                if (!string.Equals(row.Name, canonical, StringComparison.Ordinal))
                    row.Name = canonical;

                var current = _dataModelManager.GetMetadataValue(field);
                if (!string.Equals(current, row.Value, StringComparison.Ordinal))
                {
                    _dataModelManager.SetMetadataValue(field, row.Value);
                    changed = true;
                }
            }

            foreach (var row in customRows)
            {
                if (!string.Equals(row.Model!.Name, row.Name, StringComparison.Ordinal))
                {
                    _dataModelManager.UpdateMetadataFieldName(row.Model!, row.Name);
                    changed = true;
                }

                if (!string.Equals(row.Model!.Value, row.Value, StringComparison.Ordinal))
                {
                    _dataModelManager.UpdateMetadataFieldValue(row.Model!, row.Value);
                    changed = true;
                }
            }

            if (changed)
                _mainVm.NotifyModelEdited();
        }
        finally
        {
            _isCommittingEdits = false;
        }
    }

    public void ForceCanEditNotify()
    {
        RemoveFieldCommand.NotifyCanExecuteChanged();
        DuplicateFieldCommand.NotifyCanExecuteChanged();
    }

    private void OnMetadataFieldsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildRows();
        RefreshFilteredRows();
    }

    private void OnMetadataChanged()
    {
        RebuildRows();
        RefreshFilteredRows();
    }

    private void RebuildRows()
    {
        var selected = SelectedRow;
        _rows.Clear();

        foreach (var field in BuiltInOrder)
            _rows.Add(new MetadataRowViewModel(field, _dataModelManager.GetMetadataValue(field)));

        foreach (var f in _changeNotifier.MetadataFields)
            _rows.Add(new MetadataRowViewModel(f));

        SelectedRow = selected is not null ? _rows.FirstOrDefault(r => r.Model == selected.Model && r.BuiltInField == selected.BuiltInField) : null;
    }

    private void RefreshFilteredRows()
    {
        var selected = SelectedRow;
        _filteredRows.Clear();

        IEnumerable<MetadataRowViewModel> q = _rows;
        if (!string.IsNullOrWhiteSpace(FilterText))
            q = q.Where(r =>
                r.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                r.Value.Contains(FilterText, StringComparison.OrdinalIgnoreCase));

        foreach (var row in q)
            _filteredRows.Add(row);

        SelectedRow = selected is not null && _filteredRows.Contains(selected)
            ? selected
            : _filteredRows.FirstOrDefault();
    }

    private static string Esc(string? s) => s?.Replace("\"", "\"\"") ?? string.Empty;

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQ = false;
        var cur = new StringBuilder();
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