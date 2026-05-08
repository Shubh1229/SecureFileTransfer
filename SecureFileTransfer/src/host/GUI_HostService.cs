using System.Net;
using System.Net.Sockets;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.protocols;
using SecureFileTransfer.src.security;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.host
{
    public class GUI_HostService
    {
        public static async Task StartHostAsync(HostModel host, string downloadPath, Action<string>? onStatusUpdate = null, Action<long, long>? onProgressUpdate = null, CancellationToken cancellationToken = default)
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
                        onProgressUpdate,
                        cancellationToken
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

        private static Task HandleClientAsync(TcpClient client, HostModel host, string downloadPath, Action<string>? onStatusUpdate, Action<long, long>? onProgressUpdate, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using NetworkStream stream = client.GetStream();
                TransferLogging logger = new();
                ConnectionLogModel? connectionLog = null;

                try
                {
                    onStatusUpdate?.Invoke("Performing handshake...");
                    connectionLog = HandshakeProtocol.ReadHandshake(host, stream);

                    if (connectionLog == null)
                    {
                        onStatusUpdate?.Invoke("Handshake failed.");
                        return;
                    }

                    onStatusUpdate?.Invoke("Running key exchange...");
                    SessionKeyModel sessionKey = KeyExchangeProtocol.RunHost(stream);

                    if (!sessionKey.IsEstablished)
                    {
                        logger.FinishConnection(connectionLog, false);
                        onStatusUpdate?.Invoke("Key exchange failed.");
                        return;
                    }

                    TofuResult tofu = HostConfigManager.ValidateAndStorePeerFingerprint(
                        connectionLog.RemoteIPv4,
                        sessionKey.RemotePublicKeyFingerprint
                    );

                    if (tofu == TofuResult.Mismatch)
                    {
                        logger.FinishConnection(connectionLog, false);
                        onStatusUpdate?.Invoke(
                            "WARNING: Client fingerprint has changed — possible man-in-the-middle attack. Connection aborted."
                        );
                        DebugLogger.Log($"TOFU mismatch on host — aborting connection from {connectionLog.RemoteIPv4}");
                        return;
                    }

                    if (tofu == TofuResult.TrustedFirstUse)
                        onStatusUpdate?.Invoke($"Client trusted on first use. Fingerprint saved: {sessionKey.RemotePublicKeyFingerprint}");

                    onStatusUpdate?.Invoke("Receiving transfer plan...");
                    TransferPlanModel? plan = TransferPlanProtocol.Read(stream, sessionKey);

                    if (plan == null)
                    {
                        logger.FinishConnection(connectionLog, false);
                        onStatusUpdate?.Invoke("Failed to receive transfer plan.");
                        return;
                    }

                    int totalFiles = plan.FileCount;
                    int completedFiles = 0;

                    foreach (int i in Enumerable.Range(0, totalFiles))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        FileInfoModel? fileInfo = FileInfoProtocol.Read(stream, sessionKey);

                        if (fileInfo == null)
                        {
                            logger.FinishConnection(connectionLog, false);
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
                            logger.AddFileLog(connectionLog, fileInfo.FileName, fileInfo.FileSizeBytes, false);
                            logger.FinishConnection(connectionLog, false);
                            onStatusUpdate?.Invoke($"Failed: {fileInfo.FileName}");
                            return;
                        }

                        logger.AddFileLog(connectionLog, fileInfo.FileName, fileInfo.FileSizeBytes, true);
                        completedFiles++;
                        onProgressUpdate?.Invoke(completedFiles, totalFiles);
                    }

                    logger.FinishConnection(connectionLog, true);
                    onStatusUpdate?.Invoke("All files received.");
                }
                catch (OperationCanceledException)
                {
                    if (connectionLog != null)
                        logger.FinishConnection(connectionLog, false);
                    onStatusUpdate?.Invoke("Transfer cancelled.");
                    DebugLogger.Log("GUI_HostService.HandleClientAsync cancelled.");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("GUI_HostService.HandleClientAsync", ex);
                    if (connectionLog != null)
                        logger.FinishConnection(connectionLog, false);
                    onStatusUpdate?.Invoke($"Error: {ex.Message}");
                }
                finally
                {
                    if (connectionLog != null)
                        logger.SaveConnection(connectionLog);
                    client.Close();
                }
            }, cancellationToken);
        }
    }
}
