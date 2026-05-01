using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.protocols;
using SecureFileTransfer.src.security;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.client
{
    public class GUI_ClientService
    {

        public async Task SendFilesAsync(HostModel host, PeersModel peer, List<string> selectedFiles, Action<string>? onStatusUpdate = null, Action<long, long>? onProgressUpdate = null)
        {
            DebugLogger.Separator("CLIENT SESSION START");
            DebugLogger.Log("Entered ClientService.SendFilesAsync");

            if (string.IsNullOrWhiteSpace(peer.IPv4))
            {
                onStatusUpdate?.Invoke("Selected peer does not have a valid IPv4 address.");
                DebugLogger.Log("Selected peer did not have a valid IPv4 address.");
                return;
            }

            if (selectedFiles.Count == 0)
            {
                onStatusUpdate?.Invoke("No files selected.");
                DebugLogger.Log("No files selected. Client session ending.");
                return;
            }

            TransferLogging logger = new();

            ConnectionLogModel connectionLog = logger.StartConnection(
                peer.PeerName,
                peer.IPv4,
                peer.IPv6
            );

            try
            {
                int PORT = peer.Port;
                onStatusUpdate?.Invoke($"Connecting to {peer.PeerName} at {peer.IPv4}:{PORT}...");
                DebugLogger.Log($"Attempting TCP connection to {peer.IPv4}:{PORT}");

                using TcpClient tcpClient = new();

                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                await tcpClient.ConnectAsync(peer.IPv4, PORT, cts.Token);

                onStatusUpdate?.Invoke($"Connected to {peer.PeerName}.");
                DebugLogger.Log("TCP connection established.");

                using NetworkStream stream = tcpClient.GetStream();
                stream.ReadTimeout = 30000;
                stream.WriteTimeout = 30000;

                onStatusUpdate?.Invoke("Starting handshake...");
                bool handshakeSuccess = HandshakeProtocol.SendHandShake(host, stream);

                if (!handshakeSuccess)
                {
                    logger.FinishConnection(connectionLog, false);
                    onStatusUpdate?.Invoke("Handshake failed.");
                    DebugLogger.Log("Client handshake failed.");
                    return;
                }

                onStatusUpdate?.Invoke("Starting key exchange...");
                SessionKeyModel sessionKey = KeyExchangeProtocol.RunClient(stream);

                if (!sessionKey.IsEstablished)
                {
                    logger.FinishConnection(connectionLog, false);
                    onStatusUpdate?.Invoke("Key exchange failed.");
                    DebugLogger.Log("Client key exchange failed.");
                    return;
                }

                onStatusUpdate?.Invoke("Key exchange completed.");
                DebugLogger.Log("Client key exchange completed successfully.");

                TransferPlanProtocol.Send(stream, selectedFiles.Count, sessionKey);

                int completedFiles = 0;

                foreach (string filePath in selectedFiles)
                {
                    FileInfo fileStats = new(filePath);

                    onStatusUpdate?.Invoke($"Sending {fileStats.Name}...");
                    DebugLogger.Log($"Sending encrypted file info for: {filePath}");

                    FileInfoProtocol.Send(stream, filePath, sessionKey);

                    DebugLogger.Log($"Starting encrypted file byte transfer for: {filePath}");

                    bool fileSent = FileTransferProtocol.Send(stream, filePath, sessionKey);

                    if (!fileSent)
                    {
                        logger.FinishConnection(connectionLog, false);
                        onStatusUpdate?.Invoke($"Failed to send {fileStats.Name}.");
                        DebugLogger.Log($"Encrypted file byte transfer failed for: {filePath}");
                        return;
                    }

                    completedFiles++;
                    onProgressUpdate?.Invoke(completedFiles, selectedFiles.Count);

                    logger.AddFileLog(connectionLog, fileStats.Name, fileStats.Length, true);
                    DebugLogger.Log($"Completed encrypted file byte transfer for: {filePath}");
                }

                logger.FinishConnection(connectionLog, true);
                onStatusUpdate?.Invoke("File transfer completed successfully.");
                DebugLogger.Log("Client session completed successfully.");
            }
            catch (OperationCanceledException)
            {
                logger.FinishConnection(connectionLog, false);
                onStatusUpdate?.Invoke("Connection timed out.");
                DebugLogger.Log("Client connection timed out after 30 seconds.");
            }
            catch (Exception ex)
            {
                logger.FinishConnection(connectionLog, false);
                onStatusUpdate?.Invoke($"Could not set up client connection: {ex.Message}");
                DebugLogger.LogError("ClientService.SendFilesAsync", ex);
            }
            finally
            {
                logger.SaveConnection(connectionLog);
                DebugLogger.Log("Saved client connection log.");
            }
        }
    }
}