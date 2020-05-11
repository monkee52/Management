using System;
using System.Collections;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace AydenIO.Management {
    public class ManagementClassFactory<T> where T : ManagementClassBase {
        public string NamespacePath => ManagementSession.GetNamespacePath(typeof(T));
        public string ClassName => ManagementSession.GetClassName(typeof(T));

        public T CreateInstance() {
            ManagementClass managementClass = new ManagementClass(this.NamespacePath, this.ClassName, null);

            return this.CreateInstance(managementClass.CreateInstance());
        }

        public T CreateInstance(ManagementBaseObject managementObject) => (T)Activator.CreateInstance(ManagementSession.GetClassType(typeof(T)), managementObject);

        public ManagementClassCollection<T> GetInstances() {
            ManagementClass managementClass = new ManagementClass(null, ManagementSession.GetManagementPath(typeof(T)), null);
            EnumerationOptions enumerationOptions = new EnumerationOptions();

            enumerationOptions.EnsureLocatable = true;
            enumerationOptions.EnumerateDeep = true;

            return new ManagementClassCollection<T>(managementClass.GetInstances(enumerationOptions));
        }

        public ManagementClassCollection<T> GetInstances(string condition) {
            EnumerationOptions enumerationOptions = new EnumerationOptions();

            enumerationOptions.EnsureLocatable = true;
            enumerationOptions.EnumerateDeep = true;

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(null, new SelectQuery(this.ClassName, condition), enumerationOptions)) {
                return new ManagementClassCollection<T>(searcher.Get());
            }
        }
    }
}
