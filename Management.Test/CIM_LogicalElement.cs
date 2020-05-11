using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Cimv2:CIM_LogicalElement")]
    public abstract class CIM_LogicalElement : CIM_ManagedSystemElement {
        public CIM_LogicalElement(ManagementBaseObject managementObject) : base(managementObject) {

        }
    }
}
