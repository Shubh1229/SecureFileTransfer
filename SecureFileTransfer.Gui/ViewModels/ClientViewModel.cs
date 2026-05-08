using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureFileTransfer.src.client;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.setup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecureFileTransfer.Gui.ViewModels;

public partial class ClientViewModel : ViewModelBase
{
    private readonly Action _goHome;
    private readonly GUI_ClientService _clientService = new();
    private CancellationTokenSource? _clientCts;

    [ObservableProperty] private PeersModel? selectedPeer;
    [ObservableProperty] private double clientProgressValue = 0;
    [ObservableProperty] private string clientStatusMessage = "Select a peer and add files to send.";
    [ObservableProperty] private bool isClientSending = false;

    public ObservableCollection<PeersModel> Peers { get; } = new();
    public ObservableCollection<string> SelectedFiles { get; } = new();

    public ClientViewModel(Action goHome)
    {
        _goHome = goHome;
        LoadPeers();
    }

    private void LoadPeers()
    {
        try
        {
            Peers.Clear();
            var host = HostConfigManager.Load();
            foreach (var peer in host.Peers)
                Peers.Add(peer);

            if (Peers.Count > 0)
            {
                SelectedPeer = Peers[0];
                ClientStatusMessage = "Peers loaded. Select a peer and add files.";
            }
            else
            {
                ClientStatusMessage = "No peers found. Add peers in Settings.";
            }
        }
        catch (Exception ex)
        {
            ClientStatusMessage = $"Failed to load peers: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NavigateBack() => _goHome();

    [RelayCommand]
    private void ClearSelectedFiles()
    {
        SelectedFiles.Clear();
        ClientProgressValue = 0;
        ClientStatusMessage = "Files cleared.";
    }

    public void AddSelectedFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
            if (!SelectedFiles.Contains(path))
                SelectedFiles.Add(path);

        ClientStatusMessage = $"{SelectedFiles.Count} file(s) selected.";
    }

    [RelayCommand]
    private async Task StartClient()
    {
        if (IsClientSending) return;

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
            var host = HostConfigManager.Load();
            _clientCts = new CancellationTokenSource();
            IsClientSending = true;
            ClientProgressValue = 0;
            ClientStatusMessage = $"Connecting to {SelectedPeer.PeerName}...";

            await _clientService.SendFilesAsync(
                host,
                SelectedPeer,
                SelectedFiles.ToList(),
                msg => ClientStatusMessage = msg,
                (current, total) =>
                {
                    if (total > 0) ClientProgressValue = current / (double)total * 100;
                },
                _clientCts.Token
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
            _clientCts?.Dispose();
            _clientCts = null;
        }
    }
}
