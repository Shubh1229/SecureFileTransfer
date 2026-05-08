using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureFileTransfer.src.client;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.host;
using SecureFileTransfer.src.setup;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace SecureFileTransfer.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string hostName = "Unknown";

    [ObservableProperty]
    private string iPv4 = "Unknown";

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isHostRunning = false;

    [ObservableProperty]
    private string hostStatusMessage = "Host stopped.";

    [ObservableProperty]
    private double hostProgressValue = 0;

    [ObservableProperty]
    private string downloadPath = "";

    [ObservableProperty]
    private double clientProgressValue = 0;

    [ObservableProperty]
    private string clientStatusMessage = "Client ready.";

    [ObservableProperty]
    private bool isClientSending = false;

    [ObservableProperty]
    private PeersModel? selectedPeer;

    public ObservableCollection<PeersModel> Peers { get; } = new();

    public ObservableCollection<string> SelectedFiles { get; } = new();

    private CancellationTokenSource? _hostCancellationTokenSource;
    private readonly GUI_HostService _hostService = new();

    private CancellationTokenSource? _clientCancellationTokenSource;
    private readonly GUI_ClientService _clientService = new();

    public MainWindowViewModel()
    {
        LoadHostInfo();
        LoadPeers();
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
            DownloadPath = DownloadConfigManager.Load();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load host config: {ex.Message}";
        }
    }

    private void LoadPeers()
    {
        try
        {
            Peers.Clear();

            HostModel host = HostConfigManager.Load();

            foreach (PeersModel peer in host.Peers)
            {
                Peers.Add(peer);
            }

            if (Peers.Count > 0)
            {
                SelectedPeer = Peers[0];
                ClientStatusMessage = "Peers loaded.";
            }
            else
            {
                ClientStatusMessage = "No saved peers found.";
            }
        }
        catch (Exception ex)
        {
            ClientStatusMessage = $"Failed to load peers: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StartHost()
    {
        if (IsHostRunning)
            return;

        try
        {
            HostModel host = HostConfigManager.Load();
            string downloadPath = DownloadPath;

            if (string.IsNullOrWhiteSpace(downloadPath))
            {
                downloadPath = DownloadConfigManager.Load();
                DownloadPath = downloadPath;
            }

            _hostCancellationTokenSource = new CancellationTokenSource();

            IsHostRunning = true;
            HostStatusMessage = "Starting host...";
            HostProgressValue = 0;

            await _hostService.StartHostAsync(
                host,
                downloadPath,
                message => HostStatusMessage = message,
                (current, total) =>
                {
                    if (total > 0)
                    {
                        HostProgressValue = current / (double)total * 100;
                    }
                },
                _hostCancellationTokenSource.Token
            );
        }
        catch (Exception ex)
        {
            HostStatusMessage = $"Host error: {ex.Message}";
        }
        finally
        {
            IsHostRunning = false;
            _hostCancellationTokenSource?.Dispose();
            _hostCancellationTokenSource = null;
            HostStatusMessage = "Host stopped.";
        }
    }

    [RelayCommand]
    private void StopHost()
    {
        if (!IsHostRunning)
            return;

        HostStatusMessage = "Stopping host...";
        _hostCancellationTokenSource?.Cancel();
    }

    public void SetDownloadPath(string path)
    {
        DownloadPath = path;
        DownloadConfigManager.Save(path);
        HostStatusMessage = $"Download folder set to: {path}";
    }

    public void AddSelectedFiles(IEnumerable<string> filePaths)
    {
        foreach (string filePath in filePaths)
        {
            if (!SelectedFiles.Contains(filePath))
            {
                SelectedFiles.Add(filePath);
            }
        }

        ClientStatusMessage = $"{SelectedFiles.Count} file(s) selected.";
    }

    [RelayCommand]
    private void ClearSelectedFiles()
    {
        SelectedFiles.Clear();
        ClientProgressValue = 0;
        ClientStatusMessage = "Selected files cleared.";
    }

    [RelayCommand]
    private async Task StartClient()
    {
        if (IsClientSending)
            return;

        if (SelectedPeer == null)
        {
            ClientStatusMessage = "No peer selected.";
            return;
        }

        if (SelectedFiles.Count == 0)
        {
            ClientStatusMessage = "No files selected.";
            return;
        }

        try
        {
            HostModel host = HostConfigManager.Load();

            _clientCancellationTokenSource = new CancellationTokenSource();

            IsClientSending = true;
            ClientProgressValue = 0;
            ClientStatusMessage = $"Connecting to {SelectedPeer.PeerName}...";

            await _clientService.SendFilesAsync(
                host,
                SelectedPeer,
                SelectedFiles.ToList(),
                message => ClientStatusMessage = message,
                (current, total) =>
                {
                    if (total > 0)
                    {
                        ClientProgressValue = current / (double)total * 100;
                    }
                },
                _clientCancellationTokenSource.Token
            );

            ClientStatusMessage = "Transfer finished.";
        }
        catch (Exception ex)
        {
            ClientStatusMessage = $"Client error: {ex.Message}";
        }
        finally
        {
            IsClientSending = false;
            _clientCancellationTokenSource?.Dispose();
            _clientCancellationTokenSource = null;
        }
    }
}