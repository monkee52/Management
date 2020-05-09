using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_Disk")]
    public abstract class MSFT_Disk : MSFT_StorageObject {
        public enum EBusType {
            Unknown = 0,
            SCSI = 1,
            ATAPI = 2,
            ATA = 3,
            IEEE1394 = 4,
            SSA = 5,
            FibreChannel = 6,
            USB = 7,
            RAID = 8,
            ISCSI = 9,
            SAS = 10,
            SATA = 11,
            SD = 12,
            MMC = 13,
            Virtual = 14,
            FileBackedVirtual = 15,
            StorageSpaces = 16,
            NVMe = 17
        }

        public enum EOfflineReason {
            Policy = 1,
            RedundantPath = 2,
            Snapshot = 3,
            Collision = 4,
            ResourceExhaustion = 5,
            CriticalWriteFailures = 6,
            DataIntegrityScanRequired = 7
        }

        public enum EOperationalStatus {
            Unknown = 0,
            Other = 1,
            OK = 2,
            Degraded = 3,
            Stressed = 4,
            PredictiveFailure = 5,
            Error = 6,
            NonRecoverableError = 7,
            Starting = 8,
            Stopping = 9,
            Stopped = 10,
            InService = 11,
            NoContact = 12,
            LostCommunication = 13,
            Aborted = 14,
            Dormant = 15,
            SupportingEntityInError = 16,
            Completed = 17,
            Online = 0xd010,
            NotReady = 0xd011,
            NoMedia = 0xd012,
            Offline = 0xd013,
            Failed = 0xd014
        }

        public enum EPartitionStyle {
            Unknown = 0,
            MBR = 1,
            GPT = 2
        }

        public enum EProvisioningType {
            Unknown = 0,
            Thin = 1,
            Fixed = 2
        }

        public enum EHealthStatus {
            Healthy = 0,
            Warning = 1,
            Unhealthy = 2
        }

        public MSFT_Disk(ManagementBaseObject managementObject) : base(managementObject) {

        }

        public abstract string Path { get; }
        public abstract string Location { get; }
        public abstract string FriendlyName { get; }
        public abstract UInt32 Number { get; }
        public abstract string SerialNumber { get; }
        public abstract string FirmwareVersion { get; }
        public abstract string Manufacturer { get; }
        public abstract string Model { get; }
        public abstract UInt64 Size { get; }
        public abstract UInt64 AllocatedSize { get; }
        public abstract UInt32 LogicalSectorSize { get; }
        public abstract UInt32 PhysicalSectorSize { get; }
        public abstract UInt64 LargestFreeExtent { get; }
        public abstract UInt32 NumberOfPartitions { get; }

        [ManagementProperty(CastAs = typeof(UInt16))]
        public abstract EProvisioningType ProvisioningType { get; }

        [ManagementProperty(CastAs = typeof(UInt16))]
        public abstract EOperationalStatus OperationalStatus { get; }

        [ManagementProperty(CastAs = typeof(UInt16))]
        public abstract EHealthStatus HealthStatus { get; }

        [ManagementProperty(CastAs = typeof(UInt16))]
        public abstract EBusType BusType { get; }

        [ManagementProperty(CastAs = typeof(UInt16))]
        public abstract EPartitionStyle PartitionStyle { get; }

        public abstract UInt32 Signature { get; }
        public abstract string Guid { get; }
        public abstract bool IsOffline { get; }

        [ManagementProperty(CastAs = typeof(UInt16))]
        public abstract EOfflineReason OfflineReason { get; }

        public abstract bool IsReadOnly { get; }
        public abstract bool IsSystem { get; }
        public abstract bool IsClustered { get; }
        public abstract bool IsBoot { get; }
        public abstract bool BootFromDisk { get; }

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string SetAttributes(
            [ManagementProperty(Name = "IsReadOnly")]
            bool isReadOnly,
            [ManagementProperty(Name = "Signature")]
            UInt32 signature,
            [ManagementProperty(Name = "Guid")]
            string guid
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string Refresh();

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string Online();

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string Offline();

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string Initialize(
            [ManagementProperty(Name = "PartitionStyle")]
            UInt16 partitionStyle
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string CreatePartition(
            [ManagementProperty(Name = "Size")]
            UInt64 size,
            [ManagementProperty(Name = "UseMaximumSize")]
            bool useMaximumSize,
            [ManagementProperty(Name = "Offset")]
            UInt64 offset,
            [ManagementProperty(Name = "Alignment")]
            UInt32 alignment,
            [ManagementProperty(Name = "DriveLetter")]
            Char driveLetter,
            [ManagementProperty(Name = "AssignDriveLetter")]
            bool assignDriveLetter,
            [ManagementProperty(Name = "MbrType")]
            UInt16 mbrType,
            [ManagementProperty(Name = "GptType")]
            string gptType,
            [ManagementProperty(Name = "IsHidden")]
            bool isHidden,
            [ManagementProperty(Name = "IsActive")]
            bool isActive,
            [ManagementProperty(Name = "CreatedPartition")]
            out string createdPartition
        );

        [return: ManagementProperty(Name = "ExtendedStatus")]
        public abstract string ConvertStyle(
            [ManagementProperty(Name = "PartitionStyle")]
            UInt16 partitionStyle
        );

        [return: ManagementProperty(Name = "Clear")]
        public abstract string Clear(
            [ManagementProperty(Name = "RemoveData")]
            bool removeData,
            [ManagementProperty(Name = "RemoveOEM")]
            bool removeOem,
            [ManagementProperty(Name = "ZeroOutEntireDisk")]
            bool zeroOutEntireDisk
        );
    }
}
