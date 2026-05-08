using System.Net.Sockets;
using System.Text.Json;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.helper;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.security;

namespace SecureFileTransfer.src.protocols
{
    public static class TransferPlanProtocol
    {
        public static void Send(
            NetworkStream stream,
            int fileCount,
            SessionKeyModel sessionKey)
        {
            TransferPlanModel plan = new()
            {
                FileCount = fileCount
            };

            string json = JsonSerializer.Serialize(plan);

            MessageHelper.SendEncryptedMessage(stream, json, sessionKey);

            DebugLogger.Log($"Sent encrypted transfer plan for {fileCount} file(s).");
        }

        public static TransferPlanModel? Read(
            NetworkStream stream,
            SessionKeyModel sessionKey)
        {
            DebugLogger.Log("Host waiting for encrypted transfer plan.");

            string? json = MessageHelper.ReadEncryptedMessage(stream, sessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                DebugLogger.Log("No encrypted transfer plan received.");
                return null;
            }

            TransferPlanModel? plan = JsonSerializer.Deserialize<TransferPlanModel>(json);

            if (plan == null)
            {
                DebugLogger.Log("Failed to deserialize encrypted transfer plan.");
                return null;
            }

            DebugLogger.Log($"Encrypted transfer plan deserialized successfully. FileCount={plan.FileCount}");
            return plan;
        }
    }
}