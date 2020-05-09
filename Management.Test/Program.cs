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
            ManagementSession.GetFactory<MSFT_StorageObject>().GetInstances().OfType<MSFT_StorageObject>().ToArray();
            ManagementSession.GetFactory<MSFT_StorageFaultDomain>().GetInstances().OfType<MSFT_StorageFaultDomain>().ToArray();
            ManagementSession.GetFactory<MSFT_PhysicalDisk>().GetInstances().OfType<MSFT_PhysicalDisk>().ToArray();
            ManagementSession.GetFactory<MSFT_Disk>().GetInstances().OfType<MSFT_Disk>().ToArray();
            ManagementSession.GetFactory<MSFT_Volume>().GetInstances().OfType<MSFT_Volume>().ToArray();
            ManagementSession.GetFactory<MSFT_Partition>().GetInstances().OfType<MSFT_Partition>().ToArray();
            ManagementSession.GetFactory<MSFT_DiskImage>().GetInstances().OfType<MSFT_DiskImage>().ToArray();
            ManagementSession.GetFactory<MSFT_DiskToPartition>().GetInstances().OfType<MSFT_DiskToPartition>().ToArray();

            var x = ManagementSession.GetFactory<MSFT_DiskToPartition>().GetInstances().OfType<MSFT_DiskToPartition>().ToArray();

            foreach (MSFT_DiskToPartition d2p in x) {
                Console.WriteLine(d2p.Disk.FriendlyName);
                Console.WriteLine(String.Join(", ", d2p.Partition.AccessPaths ?? new string[] { }));
            }

            ManagementSession.Save();

            Console.Write("Press any key to continue...");
            Console.ReadKey(true);
            Console.WriteLine();
        }
    }
}
