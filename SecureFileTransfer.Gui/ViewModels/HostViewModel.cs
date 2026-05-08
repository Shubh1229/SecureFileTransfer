using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureFileTransfer.src.host;
using SecureFileTransfer.src.setup;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SecureFileTransfer.Gui.ViewModels;

public partial class HostViewModel : ViewModelBase
{
    private readonly Action _goHome;
    private CancellationTokenSource? _hostCts;

    [ObservableProperty] private string hostName = "Unknown";
    [ObservableProperty] private string iPv4 = "Unknown";
    [ObservableProperty] private string downloadPath = "";
    [ObservableProperty] private string hostStatusMessage = "Host stopped.";
    [ObservableProperty] private double hostProgressValue = 0;
    [ObservableProperty] private bool isHostRunning = false;

    public HostViewModel(Action goHome)
    {
        _goHome = goHome;
        LoadHostInfo();
    }

    private void LoadHostInfo()
    {
        try
        {
            var host = HostConfigManager.Load();
            HostName = host.HostName;
            IPv4 = host.IPv4;
            DownloadPath = DownloadConfigManager.Load();
        }
        catch (Exception ex)
        {
            HostStatusMessage = $"Failed to load config: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        _hostCts?.Cancel();
        _goHome();
    }

    [RelayCommand]
    private async Task StartHost()
    {
        if (IsHostRunning) return;

        try
        {
            var host = HostConfigManager.Load();
            string path = DownloadPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = DownloadConfigManager.Load();
                DownloadPath = path;
            }

            _hostCts = new CancellationTokenSource();
            IsHostRunning = true;
            HostStatusMessage = "Starting host...";
            HostProgressValue = 0;

            await GUI_HostService.StartHostAsync(
                host,
                path,
                msg => HostStatusMessage = msg,
                (current, total) =>
                {
                    if (total > 0) HostProgressValue = current / (double)total * 100;
                },
                _hostCts.Token
            );
        }
        catch (Exception ex)
        {
            HostStatusMessage = $"Host error: {ex.Message}";
        }
        finally
        {
            IsHostRunning = false;
            _hostCts?.Dispose();
            _hostCts = null;
            HostStatusMessage = "Host stopped.";
        }
    }

    [RelayCommand]
    private void StopHost()
    {
        if (!IsHostRunning) return;
        HostStatusMessage = "Stopping host...";
        _hostCts?.Cancel();
    }

    public void SetDownloadPath(string path)
    {
        DownloadPath = path;
        DownloadConfigManager.Save(path);
        HostStatusMessage = $"Download folder set to: {path}";
    }
}
