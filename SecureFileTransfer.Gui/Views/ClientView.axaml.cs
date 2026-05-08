using Avalonia.Controls;
using Avalonia.Interactivity;
using SecureFileTransfer.Gui.ViewModels;
using System.Linq;

namespace SecureFileTransfer.Gui.Views;

public partial class ClientView : UserControl
{
    public ClientView()
    {
        InitializeComponent();
    }

    private async void AddFiles_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ClientViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new()
        {
            Title = "Select Files to Send",
            AllowMultiple = true
        });

        if (files.Count == 0) return;

        var paths = files.Select(f => f.Path.LocalPath).Where(p => !string.IsNullOrWhiteSpace(p));
        vm.AddSelectedFiles(paths);
    }
}
