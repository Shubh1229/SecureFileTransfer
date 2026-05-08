using Avalonia.Controls;
using Avalonia.Interactivity;
using SecureFileTransfer.Gui.ViewModels;

namespace SecureFileTransfer.Gui.Views;

public partial class HostView : UserControl
{
    public HostView()
    {
        InitializeComponent();
    }

    private async void ChooseDownloadFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HostViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "Choose Download Folder",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        string? path = folders[0].Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
            vm.SetDownloadPath(path);
    }
}
