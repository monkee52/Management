using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    [ManagementClassMap("ROOT\\Microsoft\\Windows\\Storage:MSFT_StorageObject")]
    public abstract class MSFT_StorageObject : ManagementClassBase {
        public MSFT_StorageObject(ManagementBaseObject managementObject) : base(managementObject) {

        }

        // Properties
        /// <summary>
        /// ObjectId is a mandatory property that is used to opaquely and uniquely identify an instance
        /// of a class. ObjectIds must be unique within the scope of the management server (which is
        /// hosting the provider). The ObjectId is created and maintained for use of the Storage
        /// Management Providers and their clients to track instances of objects. If an object is visible
        /// through two different paths (for example: there are two separate Storage Management Providers
        /// that point to the same storage subsystem) then the same object may appear with two different
        /// ObjectIds. For determining if two object instances are the same object, refer to the UniqueId
        /// property.
        /// </summary>
        public abstract string ObjectId { get; }

        /// <summary>
        /// UniqueId is a mandatory property that is used to uniquely identify a logical instance of a storage
        /// subsystem's object. This value must be the same for an object viewed by two or more provider
        /// instances (even if they are running on seperate management servers). UniqueId can be any globally
        /// unique, opaque value unless otherwise specified by a derived class.
        /// </summary>
        public abstract string UniqueId { get; }

        /// <summary>
        /// A comma-separated list of all implementation specific keys. This list is used by storage management
        /// applications to access the vendor proprietary object model. The list should be in the form:
        /// key1='value1', key2='value2'.
        /// </summary>
        public abstract string PassThroughIds { get; }

        /// <summary>
        /// The computer that is hosting the proprietary storage provider classes.
        /// </summary>
        public abstract string PassThroughServer { get; }

        /// <summary>
        /// The WMI namespace that contains the proprietary storage provider classes.
        /// </summary>
        /// 
        public abstract string PassThroughNamespace { get; }
        /// <summary>
        /// The WMI class name of the proprietary storage provider object.
        /// </summary>
        public abstract string PassThroughClass { get; }
    }

    public abstract class MSFT_StorageObjectFactory : ManagementClassFactory<MSFT_StorageObject> {
        public abstract MSFT_StorageObject CreateInstance(
            [ManagementProperty(Name = "ObjectId")]
            string objectId
        );
    }
}
