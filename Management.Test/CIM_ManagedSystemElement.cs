using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Cimv2:CIM_ManagedSystemElement")]
    public abstract class CIM_ManagedSystemElement : ManagementClassBase {
        public CIM_ManagedSystemElement(ManagementBaseObject managementObject) : base(managementObject) {

        }

        public abstract string Caption { get; }
        public abstract string Description { get; }
        public abstract DateTime InstallDate { get; }
        public abstract string Name { get; }
        public abstract string Status { get; }
    }
}
