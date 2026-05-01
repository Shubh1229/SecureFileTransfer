using System.Net;
using System.Net.Sockets;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.helper;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.protocols;
using SecureFileTransfer.src.security;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.host
{
    public class CLI_HostService
    {
        private readonly int PORT = 5000;

        public void StartHost(HostModel host)
        {
            DebugLogger.Separator("HOST SESSION START");
            DebugLogger.Log("Entered HostService.StartHost");

            Console.Clear();

            if (string.IsNullOrWhiteSpace(host.IPv4))
            {
                DebugLogger.Log("Host IPv4 was empty. Exiting host start.");
                Console.WriteLine("No valid host IPv4 address found.");
                return;
            }

            string currentDownloadPath = DownloadConfigManager.Load();
            DebugLogger.Log($"Loaded current download path: {currentDownloadPath}");

            FileBrowserService downloadDirectoryBrowser = new();
            string? selectedDownloadPath = downloadDirectoryBrowser.BrowseForDirectory(currentDownloadPath);

            if (selectedDownloadPath == null)
            {
                selectedDownloadPath = currentDownloadPath;
                DebugLogger.Log("Download directory selection cancelled. Keeping existing path.");
            }

            DownloadConfigManager.Save(selectedDownloadPath);
            DebugLogger.Log($"Using download path: {selectedDownloadPath}");

            Console.WriteLine($"Files will be saved to: {selectedDownloadPath}");

            IPAddress ipAddress = IPAddress.Parse(host.IPv4);
            TcpListener tcp = new(ipAddress, PORT);
            TransferLogging logger = new();
            ConnectionLogModel? connection = null;
            bool connectionSaved = false;

            try
            {
                tcp.Start();
                DebugLogger.Log($"TCP listener started on {host.IPv4}:{PORT}");
                Console.WriteLine($"Waiting on {host.IPv4}:{PORT}...");

                using TcpClient client = tcp.AcceptTcpClient();
                DebugLogger.Log("Client accepted by host.");
                Console.WriteLine("Client connected!");

                IPEndPoint? remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

                if (remoteEndPoint != null)
                {
                    DebugLogger.Log($"Remote endpoint: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                    Console.WriteLine($"Remote endpoint: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                }

                using NetworkStream stream = client.GetStream();

                DebugLogger.Log("Starting host handshake.");
                connection = HandshakeProtocol.ReadHandshake(host, stream);

                if (connection == null)
                {
                    DebugLogger.Log("Host handshake returned null.");
                    Console.WriteLine("Connection error...\nPress any key to continue");
                    Console.ReadKey();
                    Console.Clear();
                    return;
                }

                DebugLogger.Log("Starting host key exchange.");
                SessionKeyModel sessionKey = KeyExchangeProtocol.RunHost(stream);

                if (!sessionKey.IsEstablished)
                {
                    logger.FinishConnection(connection, false);
                    logger.SaveConnection(connection);
                    connectionSaved = true;

                    DebugLogger.Log("Host key exchange failed.");
                    Console.WriteLine("Key exchange failed...");
                    return;
                }

                DebugLogger.Log("Host key exchange completed successfully.");

                DebugLogger.Log("Waiting for encrypted transfer plan.");
                TransferPlanModel? plan = TransferPlanProtocol.Read(stream, sessionKey);

                if (plan == null)
                {
                    logger.FinishConnection(connection, false);
                    logger.SaveConnection(connection);
                    connectionSaved = true;

                    DebugLogger.Log("Encrypted transfer plan was null. Ending host session.");
                    return;
                }

                DebugLogger.Log($"Expecting {plan.FileCount} file(s).");
                Console.WriteLine($"\nExpecting {plan.FileCount} file(s)...");

                for (int i = 0; i < plan.FileCount; i++)
                {
                    DebugLogger.Log($"Reading encrypted file info #{i + 1} of {plan.FileCount}");
                    FileInfoModel? incomingFile = FileInfoProtocol.Read(stream, sessionKey);

                    if (incomingFile == null)
                    {
                        logger.FinishConnection(connection, false);
                        logger.SaveConnection(connection);
                        connectionSaved = true;

                        DebugLogger.Log("Encrypted incoming file info was null. Ending host session.");
                        return;
                    }

                    string filePath = Path.Combine(selectedDownloadPath, incomingFile.SuggestedSaveName);
                    DebugLogger.Log($"Prepared destination path for incoming file: {filePath}");

                    DebugLogger.Log($"Starting encrypted file receive for: {incomingFile.FileName}");
                    bool fileReceived = FileTransferProtocol.Read(
                        stream,
                        filePath,
                        incomingFile.FileSizeBytes,
                        sessionKey
                    );

                    logger.AddFileLog(
                        connection,
                        incomingFile.FileName,
                        incomingFile.FileSizeBytes,
                        fileReceived
                    );

                    if (!fileReceived)
                    {
                        logger.FinishConnection(connection, false);
                        logger.SaveConnection(connection);
                        connectionSaved = true;

                        DebugLogger.Log($"Encrypted file byte transfer failed for: {incomingFile.FileName}");
                        return;
                    }

                    DebugLogger.Log($"Completed encrypted file receive for: {incomingFile.FileName}");
                }

                logger.FinishConnection(connection, true);
                logger.SaveConnection(connection);
                connectionSaved = true;

                DebugLogger.Log("Host session completed successfully.");
            }
            catch (Exception ex)
            {
                if (connection != null && !connectionSaved)
                {
                    logger.FinishConnection(connection, false);
                    logger.SaveConnection(connection);
                    connectionSaved = true;
                }

                DebugLogger.LogError("HostService.StartHost", ex);
                Console.WriteLine($"Error creating TCP host: {ex.Message}");
            }
            finally
            {
                tcp.Stop();
                DebugLogger.Log("TCP listener stopped.");
                Console.WriteLine("Host stopped...\nPress any key to continue");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }
}