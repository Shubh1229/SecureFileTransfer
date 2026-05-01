
using System.Net.Sockets;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.helper;
using SecureFileTransfer.src.host;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.protocols
{
    public static class HandshakeProtocol
    {
        public static bool SendHandShake(HostModel host, NetworkStream stream, Action<string>? onStatusUpdate = null)
        {
            HandshakeModel handshake = new()
            {
                SenderName = host.HostName,
                SenderIPv4 = host.IPv4,
                SenderIPv6 = host.IPv6,
                SenderPort = host.Port
            };

            DebugLogger.Log("Client sending handshake message.");
            MessageHelper.SendMessage(stream, handshake.ToJson());

            string? messageRead = MessageHelper.ReadMessage(stream);
            if (string.IsNullOrWhiteSpace(messageRead))
            {
                DebugLogger.Log("Client never received handshake return.");
                return false;
            }

            HandshakeModel? receivedHandshake = HandshakeModel.FromJson(messageRead);
            if (receivedHandshake == null)
            {
                DebugLogger.Log("Client failed to parse handshake return.");
                return false;
            }

            DebugLogger.Log($"Handshake completed with {receivedHandshake.SenderName} ({receivedHandshake.SenderIPv4})");
            onStatusUpdate?.Invoke($"Handshake completed with {receivedHandshake.SenderName} ({receivedHandshake.SenderIPv4})");

            return true;
        }
        
        public static ConnectionLogModel? ReadHandshake(HostModel host, NetworkStream stream, Action<string>? onStatusUpdate = null)
        {
            DebugLogger.Log("Host waiting for handshake message.");
            string? messageRead = MessageHelper.ReadMessage(stream);

            if (string.IsNullOrWhiteSpace(messageRead))
            {
                DebugLogger.Log("Host never received handshake.");
                return null;
            }

            HandshakeModel? receivedHandshake = HandshakeModel.FromJson(messageRead);
            if (receivedHandshake == null)
            {
                DebugLogger.Log("Host failed to parse handshake.");
                return null;
            }

            DebugLogger.Log($"Received handshake from {receivedHandshake.SenderName} ({receivedHandshake.SenderIPv4})");

            bool hasPeer = false;
            foreach (PeersModel peer in host.Peers)
            {
                if (peer.IPv4 == receivedHandshake.SenderIPv4 || peer.PeerName == receivedHandshake.SenderName)
                {
                    hasPeer = true;
                    break;
                }
            }

            if (!hasPeer)
            {
                DebugLogger.Log("Peer not found in host config. Adding peer.");
                AddPeer(host, receivedHandshake);
            }

            HandshakeModel handshake = new()
            {
                SenderName = host.HostName,
                SenderIPv4 = host.IPv4,
                SenderIPv6 = host.IPv6
            };

            DebugLogger.Log("Host sending handshake response.");
            MessageHelper.SendMessage(stream, handshake.ToJson());

            onStatusUpdate?.Invoke($"Handshake completed with {receivedHandshake.SenderName} ({receivedHandshake.SenderIPv4})");

            return new ConnectionLogModel()
            {
                RemoteComputerName = receivedHandshake.SenderName,
                RemoteIPv4 = receivedHandshake.SenderIPv4,
                RemoteIPv6 = receivedHandshake.SenderIPv6
            };
        }
        private static void AddPeer(HostModel host, HandshakeModel receivedHandshake)
        {
            var peers = host.Peers.ToList();
            peers.Add(new PeersModel()
            {
                PeerName = receivedHandshake.SenderName,
                IPv4 = receivedHandshake.SenderIPv4,
                IPv6 = receivedHandshake.SenderIPv6,
                Port = receivedHandshake.SenderPort
            });

            host.Peers = peers.ToArray();
            HostConfigManager.Save(host);
            DebugLogger.Log($"Peer added and host config saved: {receivedHandshake.SenderName} ({receivedHandshake.SenderIPv4})");
        }
    }
}