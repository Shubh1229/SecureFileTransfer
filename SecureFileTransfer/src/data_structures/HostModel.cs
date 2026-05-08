using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecureFileTransfer.src.data_structures
{
    public class HostModel
    {
        public required string HostName {get;set;}
        public required string FullHostName {get;set;}
        public required string IPv4 {get;set;}
        public required string IPv6 {get;set;}

        public required int Port { get; set; } = 5000;

        public required PeersModel[] Peers { get; set; }

        public void PrintInfo()
        {
            Console.WriteLine($"Host Name: {HostName}");
            Console.WriteLine($"Full Host Name: {FullHostName}");
            Console.WriteLine($"IPv4 Address: {IPv4}");
            Console.WriteLine($"IPv6 Address: {IPv6}");
            Console.WriteLine($"Port: {Port}");
            Console.WriteLine("Peers: ");
            foreach (PeersModel peer in Peers)
            {
                peer.PrintInfo();
            }
        }
    }
}