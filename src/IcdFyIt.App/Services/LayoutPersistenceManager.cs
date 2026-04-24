using Avalonia.Controls;
using Avalonia.VisualTree;
using IcdFyIt.App.Controls;
using IcdFyIt.Core.Infrastructure;

namespace IcdFyIt.App.Services;

/// <summary>
/// Persists window sizes and DraggableGrid column widths in settings.xml.
/// </summary>
public static class LayoutPersistenceManager
{
    private sealed class GridRegistration
    {
        public GridRegistration(DraggableGrid grid, string key, IReadOnlyList<double> defaultWidths)
        {
            Grid = grid;
            Key = key;
            DefaultWidths = defaultWidths.ToList();
        }

        public DraggableGrid Grid { get; }
        public string Key { get; }
        public List<double> DefaultWidths { get; }
    }

    private sealed class WindowRegistration
    {
        public WindowRegistration(Window window)
        {
            Window = window;
            Key = window.GetType().Name;

            if (IsValidSize(window.Width))
                DefaultWidth = window.Width;
            if (IsValidSize(window.Height))
                DefaultHeight = window.Height;
        }

        public Window Window { get; }
        public string Key { get; }
        public double? DefaultWidth { get; }
        public double? DefaultHeight { get; }
        public List<GridRegistration> Grids { get; } = new();

        public void DiscoverGrids()
        {
            Grids.Clear();

            var grids = Window.GetVisualDescendants().OfType<DraggableGrid>().ToList();
            for (int i = 0; i < grids.Count; i++)
            {
                var grid = grids[i];
                var gridId = string.IsNullOrWhiteSpace(grid.Name) ? $"Grid{i}" : grid.Name;
                var key = $"{Key}::{gridId}";
                Grids.Add(new GridRegistration(grid, key, grid.GetDefaultColumnWidths()));
            }
        }
    }

    private static readonly object Sync = new();
    private static readonly OptionsManager OptionsManager = new();
    private static readonly Dictionary<Window, WindowRegistration> Registrations = new();

    /// <summary>Registers a window for automatic restore/save of size and DraggableGrid columns.</summary>
    public static void Register(Window window)
    {
        if (Registrations.ContainsKey(window)) return;

        Registrations[window] = new WindowRegistration(window);
        window.Opened += OnWindowOpened;
        window.Closed += OnWindowClosed;
    }

    /// <summary>Clears persisted layout data and reapplies defaults to all currently open registered windows.</summary>
    public static void ResetToDefaultsForOpenWindows()
    {
        lock (Sync)
        {
            var options = OptionsManager.Load();
            options.WindowSizes.Clear();
            options.GridColumnSizes.Clear();
            OptionsManager.Save(options);
        }

        foreach (var reg in Registrations.Values.ToList())
        {
            if (!reg.Window.IsVisible) continue;
            if (reg.Grids.Count == 0) reg.DiscoverGrids();

            if (reg.DefaultWidth is { } w)
                reg.Window.Width = Math.Max(reg.Window.MinWidth, w);
            if (reg.DefaultHeight is { } h)
                reg.Window.Height = Math.Max(reg.Window.MinHeight, h);

            foreach (var gridReg in reg.Grids)
                gridReg.Grid.SetColumnWidths(gridReg.DefaultWidths);
        }
    }

    private static void OnWindowOpened(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        if (!Registrations.TryGetValue(window, out var reg)) return;

        reg.DiscoverGrids();

        AppOptions options;
        lock (Sync)
            options = OptionsManager.Load();

        var windowSize = options.WindowSizes.FirstOrDefault(x => x.Key == reg.Key);
        if (windowSize is not null)
        {
            if (IsValidSize(windowSize.Width))
                window.Width = Math.Max(window.MinWidth, windowSize.Width);
            if (IsValidSize(windowSize.Height))
                window.Height = Math.Max(window.MinHeight, windowSize.Height);
        }

        foreach (var gridReg in reg.Grids)
        {
            var gridSize = options.GridColumnSizes.FirstOrDefault(x => x.Key == gridReg.Key);
            if (gridSize is null || gridSize.Widths.Count == 0) continue;
            gridReg.Grid.SetColumnWidths(gridSize.Widths);
        }
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        if (!Registrations.Remove(window, out var reg)) return;

        window.Opened -= OnWindowOpened;
        window.Closed -= OnWindowClosed;

        lock (Sync)
        {
            var options = OptionsManager.Load();

            var width = window.Bounds.Width;
            var height = window.Bounds.Height;
            if (IsValidSize(width) && IsValidSize(height))
            {
                var entry = options.WindowSizes.FirstOrDefault(x => x.Key == reg.Key);
                if (entry is null)
                {
                    options.WindowSizes.Add(new WindowSizeOption
                    {
                        Key = reg.Key,
                        Width = width,
                        Height = height
                    });
                }
                else
                {
                    entry.Width = width;
                    entry.Height = height;
                }
            }

            foreach (var gridReg in reg.Grids)
            {
                var widths = gridReg.Grid.GetColumnWidths();
                var entry = options.GridColumnSizes.FirstOrDefault(x => x.Key == gridReg.Key);
                if (entry is null)
                {
                    entry = new GridColumnSizeOption { Key = gridReg.Key };
                    options.GridColumnSizes.Add(entry);
                }

                entry.Widths = widths.ToList();
            }

            OptionsManager.Save(options);
        }
    }

    private static bool IsValidSize(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
}