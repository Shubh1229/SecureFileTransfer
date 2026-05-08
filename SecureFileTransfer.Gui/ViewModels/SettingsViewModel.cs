using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.setup;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SecureFileTransfer.Gui.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action _goHome;

    [ObservableProperty] private string port = "5000";
    [ObservableProperty] private string downloadPath = "";
    [ObservableProperty] private string portStatusMessage = "";
    [ObservableProperty] private string peerStatusMessage = "";
    [ObservableProperty] private string newPeerName = "";
    [ObservableProperty] private string newPeerIPv4 = "";
    [ObservableProperty] private string newPeerPort = "5000";

    public ObservableCollection<PeersModel> Peers { get; } = new();

    public SettingsViewModel(Action goHome)
    {
        _goHome = goHome;
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var host = HostConfigManager.Load();
            Port = host.Port.ToString();
            DownloadPath = DownloadConfigManager.Load();
            Peers.Clear();
            foreach (var peer in host.Peers)
                Peers.Add(peer);
        }
        catch (Exception ex)
        {
            PortStatusMessage = $"Failed to load settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NavigateBack() => _goHome();

    [RelayCommand]
    private void SavePort()
    {
        if (!int.TryParse(Port, out int portNum) || portNum < 1 || portNum > 65535)
        {
            PortStatusMessage = "Invalid port. Must be 1–65535.";
            return;
        }

        try
        {
            HostConfigManager.SetPort(portNum);
            PortStatusMessage = $"Port saved: {portNum}";
        }
        catch (Exception ex)
        {
            PortStatusMessage = $"Failed to save port: {ex.Message}";
        }
    }

    public void SetDownloadPath(string path)
    {
        DownloadPath = path;
        DownloadConfigManager.Save(path);
    }

    [RelayCommand]
    private void AddPeer()
    {
        if (string.IsNullOrWhiteSpace(NewPeerName) || string.IsNullOrWhiteSpace(NewPeerIPv4))
        {
            PeerStatusMessage = "Name and IPv4 are required.";
            return;
        }

        if (!int.TryParse(NewPeerPort, out int peerPort) || peerPort < 1 || peerPort > 65535)
        {
            PeerStatusMessage = "Invalid port.";
            return;
        }

        if (Peers.Any(p => p.IPv4 == NewPeerIPv4.Trim() || p.PeerName == NewPeerName.Trim()))
        {
            PeerStatusMessage = "A peer with that name or IP already exists.";
            return;
        }

        var newPeer = new PeersModel
        {
            PeerName = NewPeerName.Trim(),
            IPv4 = NewPeerIPv4.Trim(),
            IPv6 = "",
            Port = peerPort,
            PublicKeyFingerprint = ""
        };

        HostConfigManager.AddPeerIfNew(newPeer);
        Peers.Add(newPeer);

        NewPeerName = "";
        NewPeerIPv4 = "";
        NewPeerPort = "5000";
        PeerStatusMessage = $"Peer '{newPeer.PeerName}' added.";
    }

    [RelayCommand]
    private void RemovePeer(PeersModel peer)
    {
        try
        {
            var host = HostConfigManager.Load();
            host.Peers = host.Peers.Where(p => p.IPv4 != peer.IPv4).ToArray();
            HostConfigManager.Save(host);
            Peers.Remove(peer);
            PeerStatusMessage = $"Peer '{peer.PeerName}' removed.";
        }
        catch (Exception ex)
        {
            PeerStatusMessage = $"Failed to remove peer: {ex.Message}";
        }
    }
}
