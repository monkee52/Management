using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace AydenIO.Management {
    public abstract class ManagementClassBase : IEquatable<ManagementClassBase> {
        public abstract Int32 __GENUS { get; }
        public abstract string __CLASS { get; }
        public abstract string __SUPERCLASS { get; }
        public abstract string __DYNASTY { get; }
        public abstract Int32 __PROPERTY_COUNT { get; }
        public abstract string[] __DERIVATION { get; }
        public abstract string __SERVER { get; }
        public abstract string __NAMESPACE { get; }
        public abstract string __PATH { get; }

        protected ManagementObject ManagementObject { get; set; }

        public ManagementClassBase(ManagementBaseObject managementObject) {
            if (!this.CanInitialize(managementObject)) {
                throw new ArgumentException("Class name does not match.");
            }

            this.ManagementObject = (ManagementObject)managementObject;
        }

        protected bool CanInitialize(ManagementBaseObject managementObject) {
            string className = ManagementSession.GetClassName(this.GetType());

            if (String.Equals(className, (string)managementObject.Properties["__CLASS"].Value, StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }

            string[] baseClasses = (string[])managementObject.Properties["__DERIVATION"].Value;

            for (int i = 0; i < baseClasses.Length; i++) {
                if (String.Equals(className, baseClasses[i], StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        public void Reload() {
            this.ManagementObject.Get();
        }

        public IEnumerable<T> GetRelated<T>() where T : ManagementClassBase {
            ManagementClassFactory<T> factory = ManagementSession.GetFactory<T>();

            return this.ManagementObject.GetRelated(ManagementSession.GetClassName(typeof(T))).OfType<ManagementObject>().Select(x => factory.CreateInstance((ManagementBaseObject)x));
        }

        public bool Equals(ManagementClassBase other) {
            return this.GetType().FullName == other.GetType().FullName && String.Equals(this.__PATH, other.__PATH, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object other) {
            if (Object.ReferenceEquals(null, other)) {
                return false;
            }

            if (Object.ReferenceEquals(this, other)) {
                return true;
            }

            if (other.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals(other as ManagementClassBase);
        }

        public override Int32 GetHashCode() => this.GetType().FullName.GetHashCode() ^ (this.__PATH == null ? this.ManagementObject.GetHashCode() : this.__PATH.GetHashCode());

        public static bool operator ==(ManagementClassBase obj1, ManagementClassBase obj2) {
            if ((object)obj1 == null) {
                return (object)obj2 == null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ManagementClassBase obj1, ManagementClassBase obj2) {
            return !(obj1 == obj2);
        }
    }
}
