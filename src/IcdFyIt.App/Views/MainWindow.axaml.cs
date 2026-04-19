using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using IcdFyIt.App.ViewModels;

namespace IcdFyIt.App.Views;

public partial class MainWindow : Window
{
    private bool _allowClose = false;

    public MainWindow()
    {
        InitializeComponent();

        // Wire TreeView selection so only packet-type leaf nodes update SelectedPacketType.
        PacketTypeTree.SelectionChanged += (_, _) =>
        {
            if (DataContext is not MainWindowViewModel vm) return;
            vm.SelectedPacketType = PacketTypeTree.SelectedItem as PacketTypeNodeViewModel;
        };
    }

    // ── Close guard (ICD-IF-180) ──────────────────────────────────────────────

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_allowClose) return;
        if (DataContext is not MainWindowViewModel vm) return;
        if (!vm.IsDirty) return;

        // Cancel synchronously, then handle asynchronously.
        e.Cancel = true;
        HandleClosingAsync(vm);
    }

    private async void HandleClosingAsync(MainWindowViewModel vm)
    {
        var dialog = new UnsavedChangesDialog();
        var result = await dialog.ShowDialog<string?>(this);

        if (result == "save")
        {
            if (vm.SaveDocumentCommand is IAsyncRelayCommand asyncCmd)
                await asyncCmd.ExecuteAsync(null);
            _allowClose = true;
            Close();
        }
        else if (result == "discard")
        {
            _allowClose = true;
            Close();
        }
        // else: cancelled — stay open.
    }
}
