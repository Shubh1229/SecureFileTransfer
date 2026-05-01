using SecureFileTransfer.src.client;
using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.host;
using SecureFileTransfer.src.logging;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.start
{
    public static class StartSFTCLI
    {
        public static void Start()
        {
            DebugLogger.Separator("PROGRAM START");
            DebugLogger.Log("Program.cs entered.");

            Console.Clear();

            string path = Path.Combine(Directory.GetCurrentDirectory(), "data", ".data", "host.yaml");
            DebugLogger.Log($"Checking for host config at: {path}");

            if (!File.Exists(path))
            {
                DebugLogger.Log("Host config not found. Running Initialize.");
                new Initialize();
            }

            HostModel host = HostConfigManager.Load();
            DebugLogger.Log($"Loaded host config for {host.HostName} ({host.IPv4})");

            string menu =
            """
            Secure File Transfer
            1. View host info
            2. Manage peers
            3. Start host
            4. Start client
            5. Re-run setup
            6. Exit
            """;

            Console.WriteLine(menu + "\n");

            while (true)
            {
                string? option = Console.ReadLine();

                if (option == null || option.Length > 1 || !char.IsDigit(option[0]))
                {
                    DebugLogger.Log($"Invalid menu input: '{option}'");
                    Console.WriteLine("You Must Give a Valid Input...\nPress any key to continue");
                    Console.ReadKey();
                    Console.Clear();
                    Console.WriteLine(menu + "\n");
                    continue;
                }

                int choice = int.Parse(option);
                DebugLogger.Log($"Menu choice selected: {choice}");

                switch (choice)
                {
                    case 1:
                        Console.Clear();
                        host.PrintInfo();
                        break;

                    case 2:
                        Console.Clear();
                        ManagePeers mhp = new();
                        host = mhp.ManageHostPeers(host);
                        DebugLogger.Log("Returned from ManagePeers.");
                        break;

                    case 3:
                        CLI_HostService service = new();
                        service.StartHost(host);
                        DebugLogger.Log("Returned from HostService.StartHost.");
                        break;

                    case 4:
                        CLI_ClientService client = new();
                        client.StartClient(host);
                        DebugLogger.Log("Returned from ClientService.StartClient.");
                        break;

                    case 5:
                        Console.Clear();
                        DebugLogger.Log("Re-running Initialize from menu.");
                        new Initialize();
                        break;

                    case 6:
                    default:
                        Console.Clear();
                        HostConfigManager.Save(host);
                        DebugLogger.Log("Saved host config and exiting program.");
                        Environment.Exit(0);
                        break;
                }

                Console.WriteLine("\nPress any key to continue");
                Console.ReadKey();
                Console.Clear();
                Console.WriteLine(menu + "\n\n");
            }
        }

    }
}