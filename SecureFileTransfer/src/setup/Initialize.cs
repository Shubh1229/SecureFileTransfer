using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SecureFileTransfer.src.setup
{
    public class Initialize
    {
        public Initialize()
        {
            AppPaths.EnsureAppDirectoryExists();

            DebugLogger.Separator("INITIALIZE");
            DebugLogger.Log("Initialize started.");

            string path = AppPaths.FindFilePathConfig;
            string findFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Desktop"
            );

            if (!File.Exists(path))
            {
                File.WriteAllText(path, findFilePath);
                DebugLogger.Log($"Created find_file_path.txt with default path: {findFilePath}");
            }

            string fullHostName = Dns.GetHostName();
            string hostName = "";

            foreach (char c in fullHostName)
            {
                if (c == '.') break;
                hostName += c;
            }

            DebugLogger.Log($"Detected host name: {hostName}");
            Console.WriteLine($"Host Name: {hostName}");

            string IPv4_address = "", IPv6_address = "";

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                if (ni.Name.StartsWith("utun") || ni.Name.StartsWith("awdl"))
                    continue;

                var properties = ni.GetIPProperties();

                foreach (UnicastIPAddressInformation addr in properties.UnicastAddresses)
                {
                    IPAddress ip = addr.Address;

                    if (IPAddress.IsLoopback(ip))
                        continue;

                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IPv4_address = ip.ToString();
                        DebugLogger.Log($"Detected usable IPv4 on {ni.Name}: {IPv4_address}");
                        Console.WriteLine($"Usable IPv4 ({ni.Name}): {ip}");
                    }
                    else if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !ip.IsIPv6LinkLocal)
                    {
                        IPv6_address = ip.ToString();
                        DebugLogger.Log($"Detected usable IPv6 on {ni.Name}: {IPv6_address}");
                        Console.WriteLine($"Usable IPv6 ({ni.Name}): {ip}");
                    }
                }
            }

            HostModel host = new()
            {
                HostName = hostName,
                FullHostName = fullHostName,
                IPv4 = IPv4_address,
                IPv6 = IPv6_address,
                Port = 5000,
                Peers = Array.Empty<PeersModel>()
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            string yaml = serializer.Serialize(host);

            path = AppPaths.HostConfigPath;
            if (!File.Exists(path))
            {
                File.WriteAllText(path, yaml);
                DebugLogger.Log("Created host.yaml");
            }

            TransferHistoryModel logs = new();
            yaml = serializer.Serialize(logs);

            path = AppPaths.TransferLogsPath;
            if (!File.Exists(path))
            {
                File.WriteAllText(path, yaml);
                DebugLogger.Log("Created transfer_logs.yaml");
            }

            path = AppPaths.DownloadPathConfig;
            string downloadPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            if (!File.Exists(path))
            {
                File.WriteAllText(path, downloadPath);
                DebugLogger.Log($"Created download_path.txt with default path: {downloadPath}");
            }

            path = AppPaths.DebugLogPath;
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "");
            }

            DebugLogger.Log($"Application data directory: {AppPaths.AppDataDirectory}");
            DebugLogger.Log("Initialize finished.");
        }
    }
}