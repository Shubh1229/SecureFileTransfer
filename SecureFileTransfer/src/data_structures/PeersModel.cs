using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecureFileTransfer.src.data_structures
{
    public class PeersModel
    {
        public required string PeerName {get;set;}
        public required string IPv4 {get;set;}
        public required string IPv6 {get;set;}

        public required int Port { get; set; } = 5000;

        internal void PrintInfo()
        {
            Console.WriteLine($"\tPeers Name: {PeerName}");
            Console.WriteLine($"\tPeers IPv4 Address: {IPv4}");
            Console.WriteLine($"\tPeers IPv6 Address: {IPv6}");
            Console.WriteLine($"\tPeers Port: {Port}");
        }
    }
}