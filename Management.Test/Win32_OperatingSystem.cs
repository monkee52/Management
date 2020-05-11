using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Cimv2:Win32_OperatingSystem")]
    public abstract class Win32_OperatingSystem : CIM_OperatingSystem {
        public Win32_OperatingSystem(ManagementBaseObject managementObject) : base(managementObject) {

        }

        public abstract string BootDevice { get; }
         public abstract string BuildNumber { get; }
         public abstract string BuildType { get; }
         public abstract string CodeSet { get; }
         public abstract string CountryCode { get; }
         public abstract string CSDVersion { get; }
         public abstract bool DataExecutionPrevention_Available { get; }
         public abstract bool DataExecutionPrevention_32BitApplications { get; }
         public abstract bool DataExecutionPrevention_Drivers { get; }
         public abstract Byte DataExecutionPrevention_SupportPolicy { get; }
         public abstract bool Debug { get; }
         public abstract UInt32 EncryptionLevel { get; }
         public abstract Byte ForegroundApplicationBoost { get; }
         public abstract UInt32 LargeSystemCache { get; }
         public abstract string Locale { get; }
         public abstract string Manufacturer { get; }
         public abstract string[] MUILanguages { get; }
         public abstract UInt32 OperatingSystemSKU { get; }
         public abstract string Organization { get; }
         public abstract string OSArchitecture { get; }
         public abstract UInt32 OSLanguage { get; }
         public abstract UInt32 OSProductSuite { get; }
         public abstract bool PAEEnabled { get; }
         public abstract string PlusProductID { get; }
         public abstract string PlusVersionNumber { get; }
         public abstract bool PortableOperatingSystem { get; }
         public abstract bool Primary { get; }
         public abstract UInt32 ProductType { get; }
         public abstract string RegisteredUser { get; }
         public abstract string SerialNumber { get; }
         public abstract UInt16 ServicePackMajorVersion { get; }
         public abstract UInt16 ServicePackMinorVersion { get; }
         public abstract UInt32 SuiteMask { get; }
         public abstract string SystemDevice { get; }
         public abstract string SystemDirectory { get; }
         public abstract string SystemDrive { get; }
         public abstract string WindowsDirectory { get; }
         public abstract Byte QuantumLength { get; }
         public abstract Byte QuantumType { get; }
    }
}
