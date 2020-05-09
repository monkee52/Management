using AydenIO.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_PhysicalDisk")]
    public abstract class MSFT_PhysicalDisk : MSFT_StorageFaultDomain {
        public MSFT_PhysicalDisk(ManagementBaseObject managementObject) : base(managementObject) {

        }

        public abstract UInt16 UniqueIdFormat { get; }
        public abstract string DeviceId { get; }
        public abstract string FriendlyName { get; }
        public abstract string[] OperationalDetails { get; }

        public abstract UInt64 VirtualDiskFootprint { get; }
        public abstract UInt16 Usage { get; }
        public abstract UInt16[] SupportedUsages { get; }
        public abstract string Description { get; }
        public abstract string PartNumber { get; }
        public abstract string FirmwareVersion { get; }
        public abstract string SoftwareVersion { get; }
        public abstract UInt64 Size { get; }
        public abstract UInt64 AllocatedSize { get; }
        public abstract UInt16 BusType { get; }
        public abstract bool IsWriteCacheEnabled { get; }
        public abstract bool IsPowerProtected { get; }
        public abstract UInt64 PhysicalSectorSize { get; }
        public abstract UInt64 LogicalSectorSize { get; }
        public abstract UInt32 SpindleSpeed { get; }
        public abstract bool IsIndicationEnabled { get; }
        public abstract UInt16 EnclosureNumber { get; }
        public abstract UInt16 SlotNumber { get; }
        public abstract bool CanPool { get; }
        public abstract UInt16[] CannotPoolReason { get; }
        public abstract string OtherCannotPoolReasonDescription { get; }
        public abstract bool IsPartial { get; }
        public abstract UInt16 MediaType { get; }

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string SetWriteCache(
            [ManagementProperty(Name = "WriteCacheEnabled")]
            bool writeCacheEnabled
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string SetUsage(
            [ManagementProperty(Name = "Usage")]
            UInt16 usage
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string SetFriendlyName(
            [ManagementProperty(Name = "FriendlyName")]
            string friendlyName
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string SetDescription(
            [ManagementProperty(Name = "Description")]
            string description
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string SetAttributes(
            [ManagementProperty(Name = "MediaType")]
            UInt16 mediaType
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string Reset();

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string Maintenance(
            [ManagementProperty(Name = "EnableIndication")]
            bool enableIndication
        );
    }
}
