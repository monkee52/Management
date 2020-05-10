using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_Partition")]
    public abstract class MSFT_Partition : MSFT_StorageObject {
        public enum EMbrType : UInt16 {
            None = 0,
            FAT12 = 1,
            FAT16 = 4,
            Extended = 5,
            Huge = 6,
            IFS = 7,
            FAT32 = 12
        }

        public enum EOperationalStatus : UInt16 {
            Unknown = 0,
            Online = 1,
            NoMedia = 3,
            Failed = 5,
            Offline = 4
        }

        public MSFT_Partition(ManagementBaseObject managementObject) : base(managementObject) {

        }

        // Properties
        /// <summary>
        /// This property is an array of all the various mount points for the partition. This
        /// list includes drive letters, as well as mounted folders.
        /// </summary>
        public abstract string[] AccessPaths { get; }

        /// <summary>
        /// This property is identical to the ObjectId field of the disk object that contains
        /// this partition.
        /// </summary>
        public abstract string DiskId { get; }

        /// <summary>
        /// The operating system's number for the disk that contains this partition. Disk
        /// numbers may not necessarily remain the same across reboots.
        /// </summary>
        public abstract uint DiskNumber { get; }

        /// <summary>
        /// The currently assigned drive letter to the partition. This property is NULL (\0) if no
        /// drive letter has been assigned.
        /// </summary>
        public abstract char DriveLetter { get; }

        /// <summary>
        /// This property indicates the partition's GPT type. This property is only valid
        /// when the disk's PartitionStyle property is set to 2 - 'GPT' and will be null for
        /// all other partition styles.
        /// </summary>
        public abstract string GptType { get; }

        /// <summary>
        /// This property is a string representation of the partition's GPT GUID. This
        /// property is only valid if the disk's PartitionStyle property is set to 2 - 'GPT' and
        /// will be null for all other partition styles.
        /// </summary>
        public abstract string Guid { get; }

        /// <summary>
        /// Signifies whether or not the partition is active and can be booted. This property
        /// is only relevant for MBR disks.
        /// </summary>
        public abstract bool IsActive { get; }

        public abstract bool IsBoot { get; }

        /// <summary>
        /// If this property is set to TRUE, the partition is in direct access mode. In this mode
        /// a memory mapped file doesn't reside in RAM, instead it is mapped directly onto the
        /// Storage Class Memory device and IOs bypass the storage stack. If set to FALSE, the
        /// partiton is in the standard block mode.
        /// </summary>
        public abstract bool IsDAX { get; }

        /// <summary>
        /// If this property is set to TRUE, the partition is not detected by the mount manager. As
        /// a result, the partition does not receive a drive letter, does not receive a volume GUID
        /// path, does not host volume mount points, and is not enumerated by calls to FindFirstVolume
        /// and FindNextVolume. This ensures that applications such as disk defragmenter do not access
        /// the partition. The Volume Shadow Copy Service (VSS) uses this attribute on its shadow copies.
        /// </summary>
        public abstract bool IsHidden { get; }

        public abstract bool IsOffline { get; }
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// If this property is set to TRUE, the partition is a shadow copy of another partition. This
        /// attribute is used by the Volume Shadow Copy service (VSS). This attribute is an indication
        /// for file system filter driver-based software (such as antivirus programs) to avoid
        /// attaching to the volume. An application can use this attribute to differentiate a shadow
        /// copy partition from a production partition. For example, an application that performs a fast
        /// recovery will break a shadow copy virtual disk by clearing the read-only and hidden
        /// attributes and this attribute. This attribute is set when the shadow copy is created and
        /// cleared when the shadow copy is broken.
        /// </summary>
        public abstract bool IsShadowCopy { get; }

        public abstract bool IsSystem { get; }

        /// <summary>
        /// This property indicates the partition's MBR type. This property is only valid when
        /// the disk's PartitionStyle property is set to 1 - 'MBR' and will be NULL for all
        /// other partition styles.
        /// </summary>
        public abstract EMbrType MbrType { get; }

        /// <summary>
        /// If this property is set to TRUE, the operating system does not assign a drive letter
        /// automatically when the partition is discovered. This is only honored for GPT disks and
        /// is assumed to be FALSE for MBR disks. This attribute is useful in storage area network
        /// (SAN) environments.
        /// </summary>
        public abstract bool NoDefaultDriveLetter { get; }

        /// <summary>
        /// This property indicates the partition's offset from the beginning of the disk, measured in
        /// bytes.
        /// </summary>
        public abstract ulong Offset { get; }
        public abstract EOperationalStatus OperationalStatus { get; }

        /// <summary>
        /// The operating system's number for the partition. Ordering is based on the partition's offset,
        /// relative to other partitions. This means that the value for this property may change based off
        /// of the partition configuration in the offset range preceding this partition.
        /// </summary>
        public abstract uint PartitionNumber { get; }

        public abstract ulong Size { get; }
        public abstract ushort TransitionState { get; }

        // Methods
        public abstract void AddAccessPath(
            [ManagementProperty(Name = "AccessPath")]
            string accessPath,
            [ManagementProperty(Name = "AssignDriveLetter")]
            bool assignDriveLetter
        );

        public abstract void DeleteObject();

        public abstract string[] GetAccessPaths();

        //SupportedSize GetSupportedSize();

        public abstract void Offline();
        public abstract void Online();

        public abstract void RemoveAccessPath(
            [ManagementProperty(Name = "AccessPath")]
            string accessPath
        );

        public abstract void Resize(
            [ManagementProperty(Name = "Size")]
            ulong size
        );

        public abstract void SetAttributes(
            [ManagementProperty(Name = "GptType")]
            string gptType,
            [ManagementProperty(Name = "IsActive")]
            bool isActive,
            [ManagementProperty(Name = "IsDAX")]
            bool isDAX,
            [ManagementProperty(Name = "IsHidden")]
            bool isHidden,
            [ManagementProperty(Name = "IsReadOnly")]
            bool isReadOnly,
            [ManagementProperty(Name = "IsShadowCopy")]
            bool isShadowCopy,
            [ManagementProperty(CastAs = typeof(ushort), Name = "MbrType")]
            EMbrType mbrType,
            [ManagementProperty(Name = "NoDefaultDriveLetter")]
            bool noDefaultDriveLetter
        );
    }
}
