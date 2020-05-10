using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace AydenIO.Management {
    public class ManagementClassMapAttribute : Attribute {
        public ManagementPath Path { get; private set; }

        public ManagementClassMapAttribute(string path) {
            this.Path = new ManagementPath(path);
        }

        public string NamespacePath => this.Path.NamespacePath;
        public string ClassName => this.Path.ClassName;
    }
}
