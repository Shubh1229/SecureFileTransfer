using System.Net.Sockets;
using SecureFileTransfer.src.helper;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.security;

namespace SecureFileTransfer.src.protocols
{
    public static class FileTransferProtocol
    {
        private const int BUFFER_SIZE = 8192;

        public static bool Send(NetworkStream stream, string selectedFile, SessionKeyModel sessionKey)
        {
            try
            {
                if (!sessionKey.IsEstablished)
                {
                    DebugLogger.Log("FileTransferProtocol.Send failed: session key not established.");
                    return false;
                }

                using FileStream fileStream = new(
                    selectedFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead;
                long totalPlainBytesSent = 0;
                int chunkCount = 0;

                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] plainChunk = buffer[..bytesRead];
                    byte[] encryptedChunk = EncryptionService.Encrypt(plainChunk, sessionKey);

                    MessageHelper.SendBytesWithLengthPrefix(stream, encryptedChunk);

                    totalPlainBytesSent += bytesRead;
                    chunkCount++;
                }

                stream.Flush();

                DebugLogger.Log(
                    $"Sent encrypted file: {selectedFile}. plainBytes={totalPlainBytesSent}, chunks={chunkCount}"
                );

                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"FileTransferProtocol.Send ({selectedFile})", ex);
                return false;
            }
        }

        public static bool Read(NetworkStream stream, string destinationPath, long fileSizeBytes, SessionKeyModel sessionKey)
        {
            try
            {
                if (!sessionKey.IsEstablished)
                {
                    DebugLogger.Log("FileTransferProtocol.Read failed: session key not established.");
                    return false;
                }

                string? directory = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using FileStream fileStream = new(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );

                long totalPlainBytesWritten = 0;
                int chunkCount = 0;

                while (totalPlainBytesWritten < fileSizeBytes)
                {
                    byte[]? encryptedChunk = MessageHelper.ReadBytesWithLengthPrefix(stream);

                    if (encryptedChunk == null)
                    {
                        DebugLogger.Log(
                            $"FileTransferProtocol.Read failed: encrypted chunk was null. written={totalPlainBytesWritten}, expected={fileSizeBytes}"
                        );

                        return false;
                    }

                    byte[] plainChunk = EncryptionService.Decrypt(encryptedChunk, sessionKey.Key);

                    if (totalPlainBytesWritten + plainChunk.Length > fileSizeBytes)
                    {
                        DebugLogger.Log(
                            $"FileTransferProtocol.Read failed: decrypted bytes exceed expected file size. written={totalPlainBytesWritten}, chunk={plainChunk.Length}, expected={fileSizeBytes}"
                        );

                        return false;
                    }

                    fileStream.Write(plainChunk, 0, plainChunk.Length);

                    totalPlainBytesWritten += plainChunk.Length;
                    chunkCount++;

                    
                }

                fileStream.Flush();

                DebugLogger.Log(
                    $"Received encrypted file into: {destinationPath}. plainBytes={totalPlainBytesWritten}, chunks={chunkCount}"
                );

                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"FileTransferProtocol.Read ({destinationPath})", ex);
                return false;
            }
        }
    }
}