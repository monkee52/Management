using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_Volume")]
    public abstract class MSFT_Volume : MSFT_StorageObject {
        public enum EDedupMode : UInt16 {
            Disabled = 0,
            GeneralPurpose = 1,
            HyperV = 2,
            Backup = 3,
            NotAvailable = 4
        }

        public enum EDriveType : UInt16 {
            Unknown = 0,
            InvalidRootPath = 1,
            Removable = 2,
            Fixed = 3,
            Remote = 4,
            CDROM = 5,
            RAMDisk = 6
        }

        public enum EHealthStatus : UInt16 {
            /// <summary>
            /// The volume is functioning normally.
            /// </summary>
            Healthy = 0,

            /// <summary>
            /// The volume is still functioning, but has detected errors or issues that require administrator intervention.
            /// </summary>
            Warning = 1,

            /// <summary>
            /// The volume is not functioning, due to errors or failures.The volume needs immediate attention from an administrator.
            /// </summary>
            Unhealthy = 2
        }

        public MSFT_Volume(ManagementBaseObject managementObject) : base(managementObject) {

        }

        // Properties
        public abstract uint AllocationUnitSize { get; }
        public abstract EDedupMode DedupMode { get; }
        public abstract char DriveLetter { get; }
        public abstract EDriveType DriveType { get; }
        public abstract string FileSystem { get; }
        public abstract string FileSystemLabel { get; }
        public abstract ushort FileSystemType { get; }

        /// <summary>
        /// The health status of the Volume.
        /// </summary>
        public abstract EHealthStatus HealthStatus { get; }

        public abstract ushort[] OperationalStatus { get; }

        /// <summary>
        /// Guid path of the volume.
        /// </summary>
        [ManagementProperty(Name = "Path")]
        public abstract string VolumePath { get; }

        /// <summary>
        /// Total size of the volume.
        /// </summary>
        public abstract ulong Size { get; }

        /// <summary>
        /// Available space on the volume.
        /// </summary>
        public abstract ulong SizeRemaining { get; }
    }
}
