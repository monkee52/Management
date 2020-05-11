using AydenIO.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    class Program {
        static void Main(string[] args) {
            var w = ManagementSession.GetFactory<MSFT_Disk>().GetInstances();
            var x = ManagementSession.GetFactory<MSFT_DiskToPartition>().GetInstances();

            foreach (MSFT_Disk disk in w) {
                Console.WriteLine(disk.FriendlyName);
            }

            foreach (MSFT_DiskToPartition d2p in x) {
                Console.WriteLine(d2p.Disk.FriendlyName);
                Console.WriteLine(d2p.Partition.AccessPaths?.FirstOrDefault());
            }

            ManagementSession.Save();

            Console.Write("Press any key to continue...");
            Console.ReadKey(true);
            Console.WriteLine();
        }
    }
}
