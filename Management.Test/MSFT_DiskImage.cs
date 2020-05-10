using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_DiskImage")]
    public abstract class MSFT_DiskImage : ManagementClassBase {
        public enum EStorageType : UInt32 {
            Unknown,
            ISO,
            VHD,
            VHDX,
            VHDSet
        }

        public enum EAccess : UInt16 {
            Unknown,
            ReadWrite = 2,
            ReadOnly = 3
        }

        public MSFT_DiskImage(ManagementBaseObject managementObject) : base(managementObject) {

        }

        public abstract bool Attached { get; }
        public abstract ulong BlockSize { get; }
        public abstract string DevicePath { get; }
        public abstract ulong FileSize { get; }
        public abstract string ImagePath { get; }
        public abstract ulong LogicalSectorSize { get; }
        public abstract uint Number { get; }
        public abstract ulong Size { get; }
        public abstract EStorageType StorageType { get; }

        public abstract void Mount(
            [ManagementProperty(CastAs = typeof(ushort), Name = "Access")]
            EAccess access,
            [ManagementProperty(Name = "NoDriveLetter")]
            bool noDriveLetter
        );

        public abstract void Dismount();
    }
}
