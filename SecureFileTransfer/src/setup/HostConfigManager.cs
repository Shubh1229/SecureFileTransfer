using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SecureFileTransfer.src.setup
{
    public enum TofuResult
    {
        Trusted,          // fingerprint matches stored value
        TrustedFirstUse,  // no stored fingerprint — this is the first connection; fingerprint saved
        Mismatch,         // fingerprint differs from stored value — potential MITM
        PeerNotFound      // no peer record exists for this IP yet
    }

    public static class HostConfigManager
    {
        private static readonly string PathToConfig = AppPaths.HostConfigPath;
        private static readonly object _lock = new();

        public static HostModel Load()
        {
            lock (_lock)
            {
                AppPaths.EnsureAppDirectoryExists();
                DebugLogger.Log($"HostConfigManager loading host config from: {PathToConfig}");

                if (!File.Exists(PathToConfig))
                {
                    throw new FileNotFoundException($"Host config not found: {PathToConfig}");
                }

                string yaml = File.ReadAllText(PathToConfig);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                HostModel host = deserializer.Deserialize<HostModel>(yaml);

                host.Peers ??= Array.Empty<PeersModel>();
                host.IPv6 ??= "";

                DebugLogger.Log($"HostConfigManager loaded host: {host.HostName} ({host.IPv4})");
                return host;
            }
        }

        public static void Save(HostModel host)
        {
            lock (_lock)
            {
                AppPaths.EnsureAppDirectoryExists();

                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                string yaml = serializer.Serialize(host);
                File.WriteAllText(PathToConfig, yaml);

                DebugLogger.Log($"HostConfigManager saved host config for: {host.HostName} ({host.IPv4})");
            }
        }

        public static void AddPeerIfNew(PeersModel newPeer)
        {
            lock (_lock)
            {
                HostModel host = Load();
                if (host.Peers.Any(p => p.IPv4 == newPeer.IPv4 || p.PeerName == newPeer.PeerName))
                    return;

                host.Peers = [..host.Peers, newPeer];
                Save(host);
                DebugLogger.Log($"HostConfigManager added new peer: {newPeer.PeerName} ({newPeer.IPv4})");
            }
        }

        public static TofuResult ValidateAndStorePeerFingerprint(string peerIPv4, string fingerprint)
        {
            lock (_lock)
            {
                HostModel host = Load();
                PeersModel? peer = host.Peers.FirstOrDefault(p => p.IPv4 == peerIPv4);

                if (peer == null)
                {
                    DebugLogger.Log($"TOFU: no peer record for {peerIPv4}");
                    return TofuResult.PeerNotFound;
                }

                if (string.IsNullOrEmpty(peer.PublicKeyFingerprint))
                {
                    peer.PublicKeyFingerprint = fingerprint;
                    Save(host);
                    DebugLogger.Log($"TOFU: stored first-use fingerprint for {peerIPv4}: {fingerprint}");
                    return TofuResult.TrustedFirstUse;
                }

                if (peer.PublicKeyFingerprint == fingerprint)
                {
                    DebugLogger.Log($"TOFU: fingerprint verified for {peerIPv4}");
                    return TofuResult.Trusted;
                }

                DebugLogger.Log($"TOFU MISMATCH for {peerIPv4}! Stored: {peer.PublicKeyFingerprint} | Received: {fingerprint}");
                return TofuResult.Mismatch;
            }
        }

        public static string GetPath()
        {
            return PathToConfig;
        }

        public static int GetPort()
        {
            AppPaths.EnsureAppDirectoryExists();
            int port = Load().Port;
            DebugLogger.Log($"HostConfigManager retrieved port: {port}");
            return port;
        }
        public static void SetPort(int port)
        {
            HostModel host = Load();
            host.Port = port;
            Save(host);
            DebugLogger.Log($"HostConfigManager set port to: {port}");
        }
        public static string GetIPv6()
        {
            AppPaths.EnsureAppDirectoryExists();
            string ipv6 = Load().IPv6;
            if (string.IsNullOrWhiteSpace(ipv6))
            {
                DebugLogger.Log("HostConfigManager retrieved empty IPv6 address.");
                return "";
            }
            DebugLogger.Log($"HostConfigManager retrieved IPv4: {ipv6}");
            return ipv6;
        }
        public static void SetIPv6(string ipv6)
        {
            HostModel host = Load();
            host.IPv6 = ipv6;
            Save(host);
            DebugLogger.Log($"HostConfigManager set IPv6 to: {ipv6}");
        }
    }
}