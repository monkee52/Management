using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_StorageFaultDomain")]
    public abstract class MSFT_StorageFaultDomain : MSFT_StorageObject {
        public MSFT_StorageFaultDomain(ManagementBaseObject managementObject) : base(managementObject) {

        }

        public abstract string Manufacturer { get; }
        public abstract string Model { get; }
        public abstract string SerialNumber { get; }
        public abstract string PhysicalLocation { get; }
        public abstract UInt16 HealthStatus { get; }
        public abstract UInt16[] OperationalStatus { get; }
    }
}
