using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SecureFileTransfer.Gui.ViewModels;

namespace SecureFileTransfer.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private async void ChooseDownloadFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "Choose Download Folder",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return;

        string? path = folders[0].Path.LocalPath;

        if (!string.IsNullOrWhiteSpace(path))
        {
            vm.SetDownloadPath(path);
        }
    }

    private void GitHub_Click(object? sender, PointerPressedEventArgs e)
    {
        var url = "https://github.com/Shubh1229/SecureFileTransfer";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }
}