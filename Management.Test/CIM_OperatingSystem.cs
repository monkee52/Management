using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Cimv2:CIM_OperatingSystem")]
    public abstract class CIM_OperatingSystem : CIM_LogicalElement {
        public CIM_OperatingSystem(ManagementBaseObject managementObject) : base(managementObject) {

        }

        public abstract string CreationClassName { get; }
        public abstract string CSCreationClassName { get; }
        public abstract string CSName { get; }
        public abstract Int16 CurrentTimeZone { get; }
        public abstract bool Distributed { get; }
        public abstract UInt64 FreePhysicalMemory { get; }
        public abstract UInt64 FreeSpaceInPagingFiles { get; }
        public abstract UInt64 FreeVirtualMemory { get; }
        public abstract DateTime LastBootUpTime { get; }
        public abstract DateTime LocalDateTime { get; }
        public abstract UInt32 MaxNumberOfProcesses { get; }
        public abstract UInt64 MaxProcessMemorySize { get; }
        public abstract UInt32 NumberOfLicensedUsers { get; }
        public abstract UInt32 NumberOfProcesses { get; }
        public abstract UInt32 NumberOfUsers { get; }
        public abstract UInt16 OSType { get; }
        public abstract string OtherTypeDescription { get; }
        public abstract UInt64 SizeStoredInPagingFiles { get; }
        public abstract UInt64 TotalSwapSpaceSize { get; }
        public abstract UInt64 TotalVirtualMemorySize { get; }
        public abstract UInt64 TotalVisibleMemorySize { get; }
        public abstract string Version { get; }
    }
}
