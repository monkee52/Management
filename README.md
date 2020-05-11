AydenIO.Management
==================

A library to build strongly typed CIM/WMI classes at runtime.

Example
-------
Create an abstract class defining the CIM/WMI class. This class can inherit
from other abstract classes, or `ManagementClassBase`. Use the
`ManagementClassMap` attribute to declare the underlying namespace and class.
The abstract class must also define a default constructor taking in a
`ManagementBaseObject`. Any properties you wish to use from the CIM/WMI class
should be added as an abstract property or method. If you cannot use the true
property name, you can use the `ManagementProperty` attribute to define the
actual name of the property.

```CSharp
namespace Example {
	using System;
	using System.Management;

	[ManagementClassMap("ROOT\\CIMv2:CIM_ManagedSystemElement")]
	public abstract class CIM_ManagedSystemElement : ManagementClassBase {
		public CIM_ManagedSystemElement(ManagementBaseObject managementObject) : base(managementObject) {
			
		}

		public abstract string Caption { get; }
		public abstract string Description { get; }
		public abstract DateTime InstallDate { get; }
		public abstract string Name { get; }

		[ManagementProperty(Name = "Status")]
		public abstract string Status { get; }
	}
}
```

To get the actual instances of `CIM_ManagedSystemElement`, call
`ManagementSession.GetFactory<CIM_ManagedSystemElement>().GetInstances()`.

You can retrieve the properties of the CIM/WMI class by using the properties:

**WARNING: The below code takes a very long time to complete if actually used**

```CSharp
var managedSystemElements = ManagementSession.GetFactory<CIM_ManagedSystemElement>().GetInstances();

foreach (CIM_ManagedSystemElement element in managedSystemElements) {
	Console.WriteLine(element.Name);
}
```

Other examples may be found in the `Management.Test` project.
