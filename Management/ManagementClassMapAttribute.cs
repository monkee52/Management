using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace AydenIO.Management {
    public class ManagementClassMapAttribute : Attribute {
        private ManagementPath _path;

        public ManagementClassMapAttribute(string path) {
            this._path = new ManagementPath(path);
        }

        public string NamespacePath => this._path.NamespacePath;
        public string ClassName => this._path.ClassName;
    }
}
