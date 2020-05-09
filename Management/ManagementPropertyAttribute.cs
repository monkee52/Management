using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.Management {
    public class ManagementPropertyAttribute : Attribute {
        public Type CastAs;
        public string Name;
    }
}
