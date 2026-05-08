using SecureFileTransfer.src.data_structures;
using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.host
{
    public class ManagePeers
    {
        public HostModel ManageHostPeers(HostModel host)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Manage Peers");
                Console.WriteLine("1. View peers");
                Console.WriteLine("2. Add peer");
                Console.WriteLine("3. Remove peer");
                Console.WriteLine("4. Back");

                string? option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        Console.Clear();
                        if (host.Peers.Length == 0)
                        {
                            Console.WriteLine("No peers saved.");
                        }
                        else
                        {
                            foreach (PeersModel peer in host.Peers)
                            {
                                peer.PrintInfo();
                                Console.WriteLine();
                            }
                        }
                        Pause();
                        break;

                    case "2":
                        Console.Clear();
                        Console.Write("Peer name: ");
                        string peerName = Console.ReadLine() ?? "";

                        Console.Write("Peer IPv4: ");
                        string ipv4 = Console.ReadLine() ?? "";

                        Console.Write("Peer IPv6 (optional): ");
                        string ipv6 = Console.ReadLine() ?? "";

                        var peers = host.Peers.ToList();

                        if (peers.Any(p => p.IPv4.Equals(ipv4) || p.PeerName.Equals(peerName)))
                        {
                            Console.WriteLine("Peer is already recorded...");
                            Pause();
                            break;
                        }

                        peers.Add(new PeersModel
                        {
                            PeerName = peerName,
                            IPv4 = ipv4,
                            IPv6 = ipv6,
                            Port = 5000
                        });

                        host.Peers = peers.ToArray();
                        HostConfigManager.Save(host);

                        Console.WriteLine("Peer added.");
                        Pause();
                        break;
                    case "3":
                        Console.Clear();
                        if (host.Peers.Length == 0)
                        {
                            Console.WriteLine("No peers to remove.");
                            Pause();
                            break;
                        }

                        for (int i = 0; i < host.Peers.Length; i++)
                        {
                            Console.WriteLine($"{i + 1}. {host.Peers[i].PeerName} - {host.Peers[i].IPv4}");
                        }

                        Console.Write("Choose peer number to remove: ");
                        string? input = Console.ReadLine();

                        if (int.TryParse(input, out int index) &&
                            index >= 1 &&
                            index <= host.Peers.Length)
                        {
                            var peersList = host.Peers.ToList();
                            peersList.RemoveAt(index - 1);
                            host.Peers = peersList.ToArray();
                            HostConfigManager.Save(host);
                            Console.WriteLine("Peer removed.");
                        }
                        else
                        {
                            Console.WriteLine("Invalid selection.");
                        }

                        Pause();
                        break;

                    case "4":
                        return host;

                    default:
                        Console.WriteLine("Invalid option.");
                        Pause();
                        break;
                }
            }
        }

        private void Pause()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}