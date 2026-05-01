using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SecureFileTransfer.src.setup
{
    public static class HostConfigManager
    {
        private static readonly string PathToConfig = AppPaths.HostConfigPath;

        public static HostModel Load()
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

        public static void Save(HostModel host)
        {
            AppPaths.EnsureAppDirectoryExists();

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            string yaml = serializer.Serialize(host);
            File.WriteAllText(PathToConfig, yaml);

            DebugLogger.Log($"HostConfigManager saved host config for: {host.HostName} ({host.IPv4})");
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