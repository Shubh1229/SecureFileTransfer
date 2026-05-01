using System.Net;
using System.Net.Sockets;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.protocols;
using SecureFileTransfer.src.security;

namespace SecureFileTransfer.src.host
{
    public class GUI_HostService
    {
        public async Task StartHostAsync(HostModel host, string downloadPath, Action<string>? onStatusUpdate = null, Action<long, long>? onProgressUpdate = null, CancellationToken cancellationToken = default)
        {
            DebugLogger.Separator("GUI HOST SESSION START");

            if (string.IsNullOrWhiteSpace(host.IPv4))
            {
                onStatusUpdate?.Invoke("No valid IPv4 address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(downloadPath))
            {
                onStatusUpdate?.Invoke("Invalid download path.");
                return;
            }

            int port = host.Port;

            try
            {
                IPAddress ipAddress = IPAddress.Parse(host.IPv4);
                TcpListener tcp = new(ipAddress, port);

                tcp.Start();

                onStatusUpdate?.Invoke($"Listening on {host.IPv4}:{port}...");
                DebugLogger.Log($"TCP listener started on {host.IPv4}:{port}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    onStatusUpdate?.Invoke("Waiting for client...");

                    TcpClient client = await tcp.AcceptTcpClientAsync(cancellationToken);

                    onStatusUpdate?.Invoke("Client connected!");

                    _ = HandleClientAsync(
                        client,
                        host,
                        downloadPath,
                        onStatusUpdate,
                        onProgressUpdate
                    );
                }

                tcp.Stop();
            }
            catch (OperationCanceledException)
            {
                onStatusUpdate?.Invoke("Host stopped.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("GUI_HostService.StartHostAsync", ex);
                onStatusUpdate?.Invoke($"Error: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, HostModel host, string downloadPath, Action<string>? onStatusUpdate, Action<long, long>? onProgressUpdate)
        {
            using NetworkStream stream = client.GetStream();

            try
            {
                onStatusUpdate?.Invoke("Performing handshake...");
                var handshakeSuccess = HandshakeProtocol.ReadHandshake(host, stream);

                if (handshakeSuccess == null || !handshakeSuccess.WasSuccessful)
                {
                    onStatusUpdate?.Invoke("Handshake failed.");
                    return;
                }

                onStatusUpdate?.Invoke("Running key exchange...");
                SessionKeyModel sessionKey = KeyExchangeProtocol.RunHost(stream);

                if (!sessionKey.IsEstablished)
                {
                    onStatusUpdate?.Invoke("Key exchange failed.");
                    return;
                }

                onStatusUpdate?.Invoke("Receiving transfer plan...");
                TransferPlanModel? plan = TransferPlanProtocol.Read(stream, sessionKey);

                if (plan == null)
                {
                    onStatusUpdate?.Invoke("Failed to receive transfer plan.");
                    return;
                }

                int totalFiles = plan.FileCount;
                int completedFiles = 0;

                foreach (int i in Enumerable.Range(0, totalFiles))
                {
                    FileInfoModel? fileInfo = FileInfoProtocol.Read(stream, sessionKey);

                    if (fileInfo == null)
                    {
                        onStatusUpdate?.Invoke("Failed to receive file info.");
                        return;
                    }

                    string filePath = Path.Combine(downloadPath, fileInfo.SuggestedSaveName);

                    onStatusUpdate?.Invoke($"Receiving {fileInfo.FileName}...");

                    bool received = FileTransferProtocol.Read(
                        stream,
                        filePath,
                        fileInfo.FileSizeBytes,
                        sessionKey
                    );

                    if (!received)
                    {
                        onStatusUpdate?.Invoke($"Failed: {fileInfo.FileName}");
                        return;
                    }

                    completedFiles++;
                    onProgressUpdate?.Invoke(completedFiles, totalFiles);
                }

                onStatusUpdate?.Invoke("All files received.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("GUI_HostService.HandleClientAsync", ex);
                onStatusUpdate?.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
