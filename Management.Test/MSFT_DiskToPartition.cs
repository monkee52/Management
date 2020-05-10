using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_DiskToPartition")]
    public abstract class MSFT_DiskToPartition : ManagementClassBase {
        public MSFT_DiskToPartition(ManagementBaseObject managementObject) : base(managementObject) {
            
        }

        [ManagementProperty(Name = "Disk")]
        protected abstract string DiskObjectId { get; }

        [ManagementProperty(Name = "Partition")]
        protected abstract string PartitionObjectId { get; }

        public abstract MSFT_Disk Disk { get; }

        public abstract MSFT_Partition Partition { get; }

        //public MSFT_Disk Disk => ManagementSession.GetFactory<MSFT_Disk>().CreateInstance(new ManagementObject(this.ManagementObject.Scope, new ManagementPath(this.DiskObjectId), null));

        //public MSFT_Partition Partition => ManagementSession.GetFactory<MSFT_Partition>().CreateInstance(new ManagementObject(this.ManagementObject.Scope, new ManagementPath(this.PartitionObjectId), null));
    }
}
