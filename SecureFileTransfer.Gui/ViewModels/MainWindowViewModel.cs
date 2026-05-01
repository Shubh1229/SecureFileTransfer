using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.setup;
using SecureFileTransfer.src.host;


namespace SecureFileTransfer.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string hostName = "Unknown";

    [ObservableProperty]
    private string iPv4 = "Unknown";

    [ObservableProperty]
    private string statusMessage = "Ready";

    public MainWindowViewModel()
    {
        LoadHostInfo();
    }

    [RelayCommand]
    private void LoadHostInfo()
    {
        try
        {
            HostModel host = HostConfigManager.Load();

            HostName = host.HostName;
            IPv4 = host.IPv4;
            StatusMessage = "Host config loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load host config: {ex.Message}";
        }
    }
    [RelayCommand]
    private void StartHost()
    {
        try
        {
            StatusMessage = "Starting host...";

            HostModel host = HostConfigManager.Load();

            Task.Run(() =>
            {
                
                GUI_HostService service = new();
                service.StartHostAsync(host, AppPaths.FindFilePathConfig).Wait();
            });

            StatusMessage = $"Hosting on {host.IPv4}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
