using System.Net.Sockets;
using System.Text.Json;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.helper;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.security;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.protocols
{
    public static class KeyExchangeProtocol
    {
        public static SessionKeyModel RunClient(NetworkStream stream)
        {
            using var clientKeyPair = KeyExchangeService.CreateKeyPair();

            byte[] clientPublicKey = KeyExchangeService.ExportPublicKey(clientKeyPair);
            string localIdentityKey = HostConfigManager.Load().IdentityPublicKey;

            KeyExchangeModel clientKeyExchange = new()
            {
                PublicKeyBase64 = Convert.ToBase64String(clientPublicKey),
                IdentityPublicKeyBase64 = localIdentityKey
            };

            string clientJson = JsonSerializer.Serialize(clientKeyExchange);

            DebugLogger.Log("Client sending public key.");
            MessageHelper.SendMessage(stream, clientJson);

            DebugLogger.Log("Client waiting for host public key.");
            string? hostJson = MessageHelper.ReadMessage(stream);

            if (string.IsNullOrWhiteSpace(hostJson))
            {
                DebugLogger.Log("Client did not receive host public key.");
                return new SessionKeyModel();
            }

            KeyExchangeModel? hostKeyExchange =
                JsonSerializer.Deserialize<KeyExchangeModel>(hostJson);

            if (hostKeyExchange == null || string.IsNullOrWhiteSpace(hostKeyExchange.PublicKeyBase64))
            {
                DebugLogger.Log("Client failed to parse host public key.");
                return new SessionKeyModel();
            }

            byte[] hostPublicKey = Convert.FromBase64String(hostKeyExchange.PublicKeyBase64);
            byte[] aesKey = KeyExchangeService.DeriveSharedKey(clientKeyPair, hostPublicKey);

            // Fingerprint the persistent identity key for TOFU, not the ephemeral session key.
            string fingerprint = string.IsNullOrWhiteSpace(hostKeyExchange.IdentityPublicKeyBase64)
                ? KeyExchangeService.ComputeFingerprint(hostPublicKey)
                : KeyExchangeService.ComputeFingerprint(
                    Convert.FromBase64String(hostKeyExchange.IdentityPublicKeyBase64));

            DebugLogger.Log($"Client session key established. Host identity fingerprint: {fingerprint}");

            return new SessionKeyModel
            {
                Key = aesKey,
                RemotePublicKeyFingerprint = fingerprint
            };
        }

        public static SessionKeyModel RunHost(NetworkStream stream)
        {
            using var hostKeyPair = KeyExchangeService.CreateKeyPair();

            DebugLogger.Log("Host waiting for client public key.");
            string? clientJson = MessageHelper.ReadMessage(stream);

            if (string.IsNullOrWhiteSpace(clientJson))
            {
                DebugLogger.Log("Host did not receive client public key.");
                return new SessionKeyModel();
            }

            KeyExchangeModel? clientKeyExchange =
                JsonSerializer.Deserialize<KeyExchangeModel>(clientJson);

            if (clientKeyExchange == null || string.IsNullOrWhiteSpace(clientKeyExchange.PublicKeyBase64))
            {
                DebugLogger.Log("Host failed to parse client public key.");
                return new SessionKeyModel();
            }

            byte[] clientPublicKey = Convert.FromBase64String(clientKeyExchange.PublicKeyBase64);
            byte[] hostPublicKey = KeyExchangeService.ExportPublicKey(hostKeyPair);
            string localIdentityKey = HostConfigManager.Load().IdentityPublicKey;

            KeyExchangeModel hostKeyExchange = new()
            {
                PublicKeyBase64 = Convert.ToBase64String(hostPublicKey),
                IdentityPublicKeyBase64 = localIdentityKey
            };

            string hostJson = JsonSerializer.Serialize(hostKeyExchange);

            DebugLogger.Log("Host sending public key.");
            MessageHelper.SendMessage(stream, hostJson);

            byte[] aesKey = KeyExchangeService.DeriveSharedKey(hostKeyPair, clientPublicKey);

            // Fingerprint the persistent identity key for TOFU, not the ephemeral session key.
            string fingerprint = string.IsNullOrWhiteSpace(clientKeyExchange.IdentityPublicKeyBase64)
                ? KeyExchangeService.ComputeFingerprint(clientPublicKey)
                : KeyExchangeService.ComputeFingerprint(
                    Convert.FromBase64String(clientKeyExchange.IdentityPublicKeyBase64));

            DebugLogger.Log($"Host session key established. Client identity fingerprint: {fingerprint}");

            return new SessionKeyModel
            {
                Key = aesKey,
                RemotePublicKeyFingerprint = fingerprint
            };
        }
    }
}