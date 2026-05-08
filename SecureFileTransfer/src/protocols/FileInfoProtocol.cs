using System.Net.Sockets;
using System.Text.Json;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.helper;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.security;

namespace SecureFileTransfer.src.protocols
{
    public static class FileInfoProtocol
    {
        public static void Send(
            NetworkStream stream,
            string selectedFile,
            SessionKeyModel sessionKey)
        {
            FileInfo fileStats = new(selectedFile);

            FileInfoModel file = new()
            {
                FileName = fileStats.Name,
                FileSizeBytes = fileStats.Length,
                RelativeSourcePath = "",
                SuggestedSaveName = fileStats.Name
            };

            string json = JsonSerializer.Serialize(file);

            MessageHelper.SendEncryptedMessage(stream, json, sessionKey);

            DebugLogger.Log($"Sent encrypted file info: {file.FileName}, {file.FileSizeBytes} bytes");
        }

        public static FileInfoModel? Read(
            NetworkStream stream,
            SessionKeyModel sessionKey)
        {
            DebugLogger.Log("Host waiting for encrypted file info.");

            string? json = MessageHelper.ReadEncryptedMessage(stream, sessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                DebugLogger.Log("No encrypted file info received.");
                return null;
            }

            FileInfoModel? fileInfo = JsonSerializer.Deserialize<FileInfoModel>(json);

            if (fileInfo == null)
            {
                DebugLogger.Log("Failed to deserialize encrypted file info.");
                return null;
            }

            DebugLogger.Log($"Received encrypted file info: {fileInfo.FileName}, {fileInfo.FileSizeBytes} bytes");

            return fileInfo;
        }
    }
}