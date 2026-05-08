using System.Net.Sockets;
using System.Text;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.security;

namespace SecureFileTransfer.src.helper
{
    public static class MessageHelper
    {
        private const int MaxPayloadLength = 16 * 1024 * 1024;
        public static void SendMessage(NetworkStream stream, string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            SendBytesWithLengthPrefix(stream, payload);

            DebugLogger.Log($"MessageHelper.SendMessage length={payload.Length}");
        }

        public static string? ReadMessage(NetworkStream stream)
        {
            DebugLogger.Log("MessageHelper.ReadMessage started.");

            byte[]? payloadBuffer = ReadBytesWithLengthPrefix(stream);

            if (payloadBuffer == null)
            {
                DebugLogger.Log("MessageHelper.ReadMessage failed to read payload.");
                return null;
            }

            DebugLogger.Log($"MessageHelper.ReadMessage completed successfully. length={payloadBuffer.Length}");
            return Encoding.UTF8.GetString(payloadBuffer);
        }

        public static void SendEncryptedMessage(
            NetworkStream stream,
            string plainText,
            SessionKeyModel sessionKey)
        {
            if (!sessionKey.IsEstablished)
            {
                DebugLogger.Log("MessageHelper.SendEncryptedMessage failed: session key not established.");
                throw new InvalidOperationException("Session key is not established.");
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = EncryptionService.Encrypt(plainBytes, sessionKey);

            SendBytesWithLengthPrefix(stream, encryptedBytes);

            DebugLogger.Log(
                $"MessageHelper.SendEncryptedMessage sent encrypted payload. plainLength={plainBytes.Length}, encryptedLength={encryptedBytes.Length}"
            );
        }

        public static string? ReadEncryptedMessage(
            NetworkStream stream,
            SessionKeyModel sessionKey)
        {
            if (!sessionKey.IsEstablished)
            {
                DebugLogger.Log("MessageHelper.ReadEncryptedMessage failed: session key not established.");
                throw new InvalidOperationException("Session key is not established.");
            }

            DebugLogger.Log("MessageHelper.ReadEncryptedMessage started.");

            byte[]? encryptedBytes = ReadBytesWithLengthPrefix(stream);

            if (encryptedBytes == null)
            {
                DebugLogger.Log("MessageHelper.ReadEncryptedMessage failed to read encrypted payload.");
                return null;
            }

            try
            {
                byte[] plainBytes = EncryptionService.Decrypt(encryptedBytes, sessionKey.Key);
                string plainText = Encoding.UTF8.GetString(plainBytes);

                DebugLogger.Log(
                    $"MessageHelper.ReadEncryptedMessage completed successfully. encryptedLength={encryptedBytes.Length}, plainLength={plainBytes.Length}"
                );

                return plainText;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MessageHelper.ReadEncryptedMessage decrypt failed", ex);
                return null;
            }
        }

        public static void SendBytesWithLengthPrefix(NetworkStream stream, byte[] payload)
        {
            byte[] lengthPrefix = new byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, payload.Length);

            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        public static byte[]? ReadBytesWithLengthPrefix(NetworkStream stream)
        {
            byte[]? lengthBuffer = ReadExact(stream, 4);

            if (lengthBuffer == null)
            {
                DebugLogger.Log("MessageHelper.ReadBytesWithLengthPrefix failed to read length prefix.");
                return null;
            }

            int payloadLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);

            if (payloadLength <= 0 || payloadLength > MaxPayloadLength)
            {
                DebugLogger.Log($"MessageHelper.ReadBytesWithLengthPrefix got invalid payload length={payloadLength}");
                return null;
            }

            byte[]? payloadBuffer = ReadExact(stream, payloadLength);

            if (payloadBuffer == null)
            {
                DebugLogger.Log(
                    $"MessageHelper.ReadBytesWithLengthPrefix failed to read payload. expectedLength={payloadLength}"
                );

                return null;
            }

            return payloadBuffer;
        }

        public static byte[]? ReadExact(NetworkStream stream, int length)
        {
            byte[] buffer = new byte[length];
            int totalRead = 0;

            while (totalRead < length)
            {
                int bytesRead = stream.Read(buffer, totalRead, length - totalRead);

                if (bytesRead == 0)
                {
                    DebugLogger.Log(
                        $"MessageHelper.ReadExact hit EOF at totalRead={totalRead}, expected={length}"
                    );

                    return null;
                }

                totalRead += bytesRead;
            }

            return buffer;
        }
    }
}