using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Management;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace AydenIO.Management {

    public class ManagementSession {
        private static IDictionary<string, object> _factories;

        private static IDictionary<string, Type> _factoryTypes;
        private static IDictionary<string, Type> _classTypes;

        private static AssemblyName assemblyName;
        private static AssemblyBuilder assemblyBuilder;
        private static ModuleBuilder moduleBuilder;

        static ManagementSession() {
            ManagementSession._factories = new Dictionary<string, object>();

            ManagementSession._factoryTypes = new Dictionary<string, Type>();
            ManagementSession._classTypes = new Dictionary<string, Type>();

            ManagementSession.assemblyName = new AssemblyName("ManagementClassDynamicImplementations");
            ManagementSession.assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(ManagementSession.assemblyName, AssemblyBuilderAccess.RunAndSave);

#if DEBUG && false
            Type debugType = typeof(DebuggableAttribute);
            ConstructorInfo debugConInfo = debugType.GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) });
            CustomAttributeBuilder debugAttrBuilder = new CustomAttributeBuilder(debugConInfo, new object[] {
                DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default
            });

            ManagementSession.assemblyBuilder.SetCustomAttribute(debugAttrBuilder);

            bool debugSymbols = true;
#else
            bool debugSymbols = false;
#endif
            ManagementSession.moduleBuilder = ManagementSession.assemblyBuilder.DefineDynamicModule(ManagementSession.assemblyName.Name, ManagementSession.assemblyName.Name + ".dll", debugSymbols);

            ManagementSessionDebugUtilities.DefineDocument(ManagementSession.moduleBuilder);
        }

        public static string GetNamespacePath(Type t) {
            ManagementClassMapAttribute a = Attribute.GetCustomAttribute(t, typeof(ManagementClassMapAttribute)) as ManagementClassMapAttribute;

            return a?.NamespacePath;
        }

        public static string GetClassName(Type t) {
            ManagementClassMapAttribute a = Attribute.GetCustomAttribute(t, typeof(ManagementClassMapAttribute)) as ManagementClassMapAttribute;

            return a?.ClassName;
        }

        public static ManagementPath GetManagementPath(Type t) {
            ManagementClassMapAttribute a = Attribute.GetCustomAttribute(t, typeof(ManagementClassMapAttribute)) as ManagementClassMapAttribute;

            return a?.Path;
        }

        // Handle classes
        private static Type CreateClassType(Type classDefType) {
            string classTypeName = $"Impl_{classDefType.Name}";

            if (!classDefType.IsSubclassOf(typeof(ManagementClassBase))) {
                throw new Exception($"'{classDefType.Name}' does not inherit from '{nameof(ManagementClassBase)}'");
            }

            string mgmtNamespacePath = ManagementSession.GetNamespacePath(classDefType);
            string mgmtClassName = ManagementSession.GetClassName(classDefType);

            ManagementClass mgmtClass = new ManagementClass(mgmtNamespacePath, mgmtClassName, null);

            TypeBuilder classType = ManagementSession.moduleBuilder.DefineType(classTypeName, TypeAttributes.Class | TypeAttributes.NotPublic, classDefType);
            BindingFlags defaultBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Predefined method info
            MethodInfo getManagementObject = classDefType.GetMethod("get_ManagementObject", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo getManagementBaseObjectProperties = typeof(ManagementBaseObject).GetMethod("get_Properties", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo getPropertyDataCollectionItem = typeof(PropertyDataCollection).GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo getManagementScope = typeof(ManagementObject).GetMethod("get_Scope", BindingFlags.Instance | BindingFlags.Public);

            MethodInfo getFactory = typeof(ManagementSession).GetMethod("GetFactory", BindingFlags.Public | BindingFlags.Static, new GetFactoryBinder(), new Type[] { }, null);

            MethodInfo getPropertyDataValue = typeof(PropertyData).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setPropertyDataValue = typeof(PropertyData).GetMethod("set_Value", BindingFlags.Instance | BindingFlags.Public);

            MethodInfo convertToDateTime = typeof(ManagementDateTimeConverter).GetMethod("ToDateTime", BindingFlags.Static | BindingFlags.Public);
            MethodInfo convertFromDateTime = typeof(ManagementDateTimeConverter).GetMethod("ToDmtfDateTime", BindingFlags.Static | BindingFlags.Public);

            MethodInfo getPath = typeof(ManagementClassBase).GetMethod("get___PATH", BindingFlags.Instance | BindingFlags.Public);

            // Implement default constructor
            ConstructorBuilder defaultConstructor = classType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(ManagementBaseObject) });
            ILGenerator constructorIl = defaultConstructor.GetILGenerator();

            // Define parameter
            defaultConstructor.DefineParameter(1, ParameterAttributes.None, "managementObject");

            ManagementSessionDebugUtilities.MarkPoint(constructorIl);
            constructorIl.Emit(OpCodes.Ldarg_0);
            constructorIl.Emit(OpCodes.Ldarg_1);
            constructorIl.Emit(OpCodes.Call, classDefType.GetConstructor(new[] { typeof(ManagementBaseObject) }));
            constructorIl.Emit(OpCodes.Ret);

            // Implement properties
            IEnumerable<PropertyInfo> propertiesToImplement = classDefType.GetProperties(defaultBindingFlags);

            foreach (PropertyInfo propertyInfo in propertiesToImplement) {
                // Get real property name and type
                ManagementPropertyAttribute propertyAttribute = propertyInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                string realPropertyName = propertyAttribute?.Name ?? propertyInfo.Name;
                Type realPropertyType = propertyAttribute?.CastAs ?? (propertyInfo.PropertyType.IsEnum ? Enum.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType);

                // Check if underlying management object has property
                bool propertyExists = false;

                try {
                    if (mgmtClass.Properties[realPropertyName] != null) {
                        propertyExists = true;
                    }
                } catch (ManagementException e) {
                    if (e.ErrorCode != ManagementStatus.NotFound) {
                        throw e;
                    }
                }

                // Get property getter
                string propertyGetterName = $"get_{propertyInfo.Name}";
                MethodInfo getterMethodInfo = classDefType.GetMethod(propertyGetterName, defaultBindingFlags);
                bool implementGetter = getterMethodInfo != null && getterMethodInfo.IsAbstract;

                // Get property setter
                string propertySetterName = $"set_{propertyInfo.Name}";
                MethodInfo setterMethodInfo = classDefType.GetMethod(propertySetterName, defaultBindingFlags);
                bool implementSetter = setterMethodInfo != null && setterMethodInfo.IsAbstract;

                string propertyNotExistMessage = $"'{mgmtNamespacePath}:{mgmtClassName}' does not have property '{realPropertyName}";

                Debug.WriteLineIf((implementGetter || implementSetter) && !propertyExists, $"WARNING: {propertyNotExistMessage}");

                // Declare property if needed
                if (implementGetter || implementSetter) {
                    PropertyBuilder propertyBuilder = classType.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, null);

                    if (implementGetter) {
                        MethodBuilder getterMethodBuilder = classType.DefineMethod(propertyGetterName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyInfo.PropertyType, null);
                        ILGenerator il = getterMethodBuilder.GetILGenerator();

                        if (propertyExists) {
                            if (propertyInfo.PropertyType.IsSubclassOf(typeof(ManagementClassBase)) && mgmtClass.Properties[realPropertyName].Type != CimType.Reference) { // Verify CIM/WMI return type is a reference if property return type is a ManagementClassBase type
                                string err = $"'{mgmtNamespacePath}:{mgmtClassName}'.'{realPropertyName}' is not a {nameof(CimType.Reference)} type.";

                                ManagementSessionDebugUtilities.MarkPoint(il);
                                il.Emit(OpCodes.Ldstr, err);
                                il.Emit(OpCodes.Newobj, typeof(InvalidCastException).GetConstructor(new[] { typeof(String) }));
                                il.Emit(OpCodes.Throw);

                                Debug.WriteLine($"WARNING: {err}");
                            } else if (propertyInfo.PropertyType.IsSubclassOf(typeof(DateTime)) && mgmtClass.Properties[realPropertyName].Type != CimType.DateTime) { // Verify it's a DateTime type
                                string err = $"'{mgmtNamespacePath}:{mgmtClassName}'.'{realPropertyName}' is not a {nameof(CimType.DateTime)} type.";

                                ManagementSessionDebugUtilities.MarkPoint(il);
                                il.Emit(OpCodes.Ldstr, err);
                                il.Emit(OpCodes.Newobj, typeof(InvalidCastException).GetConstructor(new[] { typeof(String) }));
                                il.Emit(OpCodes.Throw);

                                Debug.WriteLine($"WARNING: {err}");
                            } else {
                                ManagementSessionDebugUtilities.MarkPoint(il);
                                // Get management object
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Call, getManagementObject);

                                // Get property - will always exist, checked above
                                il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                                il.Emit(OpCodes.Ldstr, realPropertyName);
                                il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);
                                il.Emit(OpCodes.Callvirt, getPropertyDataValue);

                                // Check if value is null
                                Label returnNull = il.DefineLabel();

                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Brfalse_S, returnNull);

                                if (realPropertyType.IsValueType) {
                                    il.Emit(OpCodes.Unbox_Any, realPropertyType);
                                }

                                ManagementSessionDebugUtilities.MarkPoint(il);

                                if (propertyInfo.PropertyType.IsSubclassOf(typeof(DateTime))) { // Convert to DateTime if needed
                                    il.Emit(OpCodes.Call, convertToDateTime);
                                } else if (propertyInfo.PropertyType.IsSubclassOf(typeof(ManagementClassBase))) { // Return type is a ManagementObject reference
                                    LocalBuilder localPath = il.DeclareLocal(typeof(ManagementPath));
                                    LocalBuilder localMgmtObj = il.DeclareLocal(typeof(ManagementObject));

                                    // Create a management object from the current scope
                                    il.Emit(OpCodes.Castclass, typeof(String));

                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Call, typeof(Debug).GetMethod("WriteLine", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(String) }, null));

                                    il.Emit(OpCodes.Newobj, typeof(ManagementPath).GetConstructor(new[] { typeof(String) }));
                                    il.Emit(OpCodes.Stloc_S, localPath);

                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Call, getManagementObject);
                                    il.Emit(OpCodes.Callvirt, getManagementScope);
                                    il.Emit(OpCodes.Ldloc_S, localPath);
                                    il.Emit(OpCodes.Ldnull);
                                    il.Emit(OpCodes.Newobj, typeof(ManagementObject).GetConstructor(new[] { typeof(ManagementScope), typeof(ManagementPath), typeof(ObjectGetOptions) }));
                                    il.Emit(OpCodes.Stloc_S, localMgmtObj);

                                    MethodInfo getTypedFactory = getFactory.MakeGenericMethod(propertyInfo.PropertyType);

                                    il.Emit(OpCodes.Call, getTypedFactory);
                                    il.Emit(OpCodes.Ldloc_S, localMgmtObj);

                                    Type factoryType = typeof(ManagementClassFactory<>).MakeGenericType(propertyInfo.PropertyType);

                                    il.Emit(OpCodes.Callvirt, factoryType.GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(ManagementObject) }, null));
                                }

                                // Return property value
                                ManagementSessionDebugUtilities.MarkPoint(il);
                                
                                if (!propertyInfo.PropertyType.IsValueType) {
                                    il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
                                }

                                il.Emit(OpCodes.Ret);

                                // Property doesn't exist, return null
                                il.MarkLabel(returnNull);

                                il.Emit(OpCodes.Pop);

                                if (propertyInfo.PropertyType.IsValueType) {
                                    Type baseType = propertyInfo.PropertyType;

                                    if (baseType.IsEnum) {
                                        baseType = Enum.GetUnderlyingType(baseType);
                                    }

                                    if (baseType == typeof(Single)) {
                                        il.Emit(OpCodes.Ldc_R4, 0.0f);
                                    } else if (baseType == typeof(Double)) {
                                        il.Emit(OpCodes.Ldc_R8, 0.0d);
                                    } else if (Marshal.SizeOf(baseType) == 8) {
                                        il.Emit(OpCodes.Ldc_I8, 0L);
                                    } else {
                                        il.Emit(OpCodes.Ldc_I4_0);
                                    }
                                } else {
                                    il.Emit(OpCodes.Ldnull);
                                    il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
                                }

                                il.Emit(OpCodes.Ret);
                            }
                        } else {
                            ManagementSessionDebugUtilities.MarkPoint(il);

                            il.Emit(OpCodes.Ldstr, propertyNotExistMessage);
                            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new[] { typeof(String) }));
                            il.Emit(OpCodes.Throw);
                        }

                        propertyBuilder.SetGetMethod(getterMethodBuilder);
                    }

                    if (implementSetter) {
                        MethodBuilder setterMethodBuilder = classType.DefineMethod(propertySetterName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyInfo.PropertyType, null);
                        ILGenerator il = setterMethodBuilder.GetILGenerator();

                        if (propertyExists) {
                            if (propertyInfo.PropertyType.IsSubclassOf(typeof(ManagementClassBase)) && mgmtClass.Properties[realPropertyName].Type != CimType.Reference) { // Verify CIM/WMI return type is a reference if property return type is a ManagementClassBase type
                                string err = $"'{mgmtNamespacePath}:{mgmtClassName}'.'{realPropertyName}' is not a {nameof(CimType.Reference)} type.";

                                ManagementSessionDebugUtilities.MarkPoint(il);
                                il.Emit(OpCodes.Ldstr, err);
                                il.Emit(OpCodes.Newobj, typeof(InvalidCastException).GetConstructor(new[] { typeof(String) }));
                                il.Emit(OpCodes.Throw);

                                Debug.WriteLine($"WARNING: {err}");
                            } else if (propertyInfo.PropertyType.IsSubclassOf(typeof(DateTime)) && mgmtClass.Properties[realPropertyName].Type != CimType.DateTime) { // Verify it's a DateTime type
                                string err = $"'{mgmtNamespacePath}:{mgmtClassName}'.'{realPropertyName}' is not a {nameof(CimType.DateTime)} type.";

                                ManagementSessionDebugUtilities.MarkPoint(il);
                                il.Emit(OpCodes.Ldstr, err);
                                il.Emit(OpCodes.Newobj, typeof(InvalidCastException).GetConstructor(new[] { typeof(String) }));
                                il.Emit(OpCodes.Throw);

                                Debug.WriteLine($"WARNING: {err}");
                            } else {
                                ManagementSessionDebugUtilities.MarkPoint(il);
                                // Get management object
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Call, getManagementObject);

                                // Get property - will always exist, checked above
                                il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                                il.Emit(OpCodes.Ldstr, realPropertyName);
                                il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                                // Get value
                                il.Emit(OpCodes.Ldarg_1);

                                ManagementSessionDebugUtilities.MarkPoint(il);

                                if (propertyInfo.PropertyType.IsSubclassOf(typeof(DateTime))) {
                                    // Convert to DMTF DateTime
                                    il.Emit(OpCodes.Call, convertFromDateTime);
                                } else if (propertyInfo.PropertyType.IsSubclassOf(typeof(ManagementClassBase))) {
                                    // Get management object path
                                    il.Emit(OpCodes.Callvirt, getPath);
                                }

                                if (realPropertyType.IsValueType) {
                                    il.Emit(OpCodes.Box, realPropertyType);
                                }

                                // Set property value
                                il.Emit(OpCodes.Callvirt, setPropertyDataValue);

                                ManagementSessionDebugUtilities.MarkPoint(il);

                                // Return value
                                il.Emit(OpCodes.Ret);
                            }
                        } else {
                            ManagementSessionDebugUtilities.MarkPoint(il);
                            il.Emit(OpCodes.Ldstr, propertyNotExistMessage);
                            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new[] { typeof(String) }));
                            il.Emit(OpCodes.Throw);
                        }

                        propertyBuilder.SetSetMethod(setterMethodBuilder);
                    }
                }
            }

            // Implement methods
            IEnumerable<MethodInfo> methodsToImplement = classDefType.GetMethods(defaultBindingFlags).Where(m => m.IsAbstract && !m.IsSpecialName);

            foreach (MethodInfo methodInfo in methodsToImplement) {
                // Get real method name, return property name, and real return type
                ManagementPropertyAttribute methodAttribute = methodInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;
                ManagementPropertyAttribute returnAttribute = methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(ManagementPropertyAttribute), false).OfType<ManagementPropertyAttribute>().FirstOrDefault();

                string realMethodName = methodAttribute?.Name ?? methodInfo.Name;
                string returnPropertyName = returnAttribute?.Name;
                Type returnPropertyType = returnAttribute?.CastAs ?? methodInfo.ReturnType;

                ManagementBaseObject mgmtInParams = null;

                try {
                    mgmtInParams = mgmtClass.GetMethodParameters(realMethodName);
                } catch (ManagementException e) {
                    if (e.ErrorCode != ManagementStatus.MethodNotImplemented) {
                        throw e;
                    }
                }

                ParameterInfo[] paramInfos = methodInfo.GetParameters();

                MethodBuilder methodBuilder = classType.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, paramInfos.Select(p => p.ParameterType).ToArray());
                ILGenerator il = methodBuilder.GetILGenerator();

                if (mgmtInParams == null) {
                    string err = $"'{mgmtNamespacePath}:{mgmtClassName}' does not have method '{realMethodName}'";

                    il.Emit(OpCodes.Ldstr, err);
                    il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new[] { typeof(String) }));
                    il.Emit(OpCodes.Throw);

                    Debug.WriteLine($"WARNING: {err}");

                    continue;
                }

                LocalBuilder inParams = il.DeclareLocal(typeof(ManagementBaseObject));

                if (!paramInfos.Where(p => !(p.IsOut || p.ParameterType.IsByRef)).Any()) {
                    // No parameters
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stloc_S, inParams);
                } else {
                    // Get method parameters, store in 'inParams'
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, getManagementObject);
                    il.Emit(OpCodes.Ldstr, realMethodName);
                    il.Emit(OpCodes.Call, typeof(ManagementObject).GetMethod("GetMethodParameters", BindingFlags.Instance | BindingFlags.Public));
                    il.Emit(OpCodes.Stloc_S, inParams);

                    for (int paramIndex = 0; paramIndex < paramInfos.Length; paramIndex++) {
                        ParameterInfo paramInfo = paramInfos[paramIndex];

                        // Define parameter name
                        methodBuilder.DefineParameter(paramIndex + 1, paramInfo.Attributes, paramInfo.Name);

                        if (paramInfo.IsOut) {
                            continue;
                        }

                        // Get property name and real type
                        ManagementPropertyAttribute paramAttribute = paramInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                        string realParamPropertyName = paramAttribute?.Name ?? paramInfo.Name;
                        Type realParamPropertyType = paramAttribute?.CastAs ?? (paramInfo.ParameterType.IsEnum ? Enum.GetUnderlyingType(paramInfo.ParameterType) : paramInfo.ParameterType);

                        bool paramExists = false;

                        try {
                            if (mgmtInParams.Properties[realParamPropertyName] != null) {
                                paramExists = true;
                            }
                        } catch (ManagementException e) {
                            if (e.ErrorCode != ManagementStatus.NotFound) {
                                throw e;
                            }
                        }

                        if (!paramExists) {
                            string err = $"'{mgmtNamespacePath}:{mgmtClassName}'.'{realMethodName}' does not have parameter '{realParamPropertyName}'";

                            Debug.WriteLine($"WARNING: {err}");

                            continue;
                        }

                        // Get property - will always exist, checked above
                        il.Emit(OpCodes.Ldloc_S, inParams);
                        il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                        il.Emit(OpCodes.Ldstr, realParamPropertyName);
                        il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                        // Get argument, ldarg.0 = this
                        il.Emit(OpCodes.Ldarg_S, paramIndex + 1);

                        if (paramInfo.ParameterType.IsByRef) {
                            il.Emit(OpCodes.Ldind_Ref);
                        }

                        Type underlyingParamType = paramInfo.ParameterType.IsByRef ? paramInfo.ParameterType.GetElementType() : paramInfo.ParameterType;

                        if (underlyingParamType.IsSubclassOf(typeof(DateTime))) {
                            // Convert to DMTF DateTime
                            il.Emit(OpCodes.Call, convertFromDateTime);
                        } else if (underlyingParamType.IsSubclassOf(typeof(ManagementClassBase))) {
                            // Get management object path
                            il.Emit(OpCodes.Callvirt, getPath);
                        }

                        if (realParamPropertyType.IsValueType) {
                            il.Emit(OpCodes.Box, realParamPropertyType);
                        }

                        // Set property value
                        il.Emit(OpCodes.Callvirt, setPropertyDataValue);
                    }
                }

                LocalBuilder outParams = il.DeclareLocal(typeof(ManagementBaseObject));

                // outParams = this.ManagementObject.InvokeMethod(realMethodName, inParams, null)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, getManagementObject);
                il.Emit(OpCodes.Ldstr, realMethodName);
                il.Emit(OpCodes.Ldloc_S, inParams);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Callvirt, typeof(ManagementObject).GetMethod("InvokeMethod", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(String), typeof(ManagementBaseObject), typeof(InvokeMethodOptions) }, null));
                il.Emit(OpCodes.Stloc_S, outParams);

                // Get error code
                il.Emit(OpCodes.Ldloc_S, outParams);
                il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                il.Emit(OpCodes.Ldstr, "ReturnValue");
                il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);
                il.Emit(OpCodes.Callvirt, getPropertyDataValue);
                il.Emit(OpCodes.Unbox_Any, typeof(UInt32));

                // Check if error code != 0
                Label invokeSuccess = il.DefineLabel();

                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brfalse_S, invokeSuccess);

                // Throw error
                il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("ThrowExceptionForHR", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Int32) }, null));

                if (methodInfo.ReturnType != typeof(void)) {
                    il.Emit(OpCodes.Ldnull);
                }

                il.Emit(OpCodes.Ret);

                // No error
                il.MarkLabel(invokeSuccess);

                il.Emit(OpCodes.Pop);

                // Handle out parameters
                for (int paramIndex = 0; paramIndex < paramInfos.Length; paramIndex++) {
                    ParameterInfo paramInfo = paramInfos[paramIndex];

                    if (!paramInfo.ParameterType.IsByRef) {
                        continue;
                    }

                    // Get property name and real type
                    ManagementPropertyAttribute paramAttribute = paramInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                    string realParamPropertyName = paramAttribute?.Name ?? paramInfo.Name;
                    Type underlyingParamType = paramInfo.ParameterType.GetElementType();
                    Type realParamPropertyType = paramAttribute?.CastAs ?? (underlyingParamType.IsEnum ? Enum.GetUnderlyingType(underlyingParamType) : underlyingParamType);

                    il.Emit(OpCodes.Ldloc_S, outParams);
                    il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                    il.Emit(OpCodes.Ldstr, realParamPropertyName);
                    il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                    Label propertyIsNull = il.DefineLabel();
                    Label propertyIsNotNull = il.DefineLabel();

                    // Check if property exists
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brfalse_S, propertyIsNull);

                    // Property exists
                    il.Emit(OpCodes.Callvirt, getPropertyDataValue);

                    if (realParamPropertyType.IsValueType) {
                        il.Emit(OpCodes.Unbox_Any, realParamPropertyType);
                    }

                    if (underlyingParamType.IsSubclassOf(typeof(DateTime))) {
                        // Convert to DateTime if needed
                        il.Emit(OpCodes.Call, convertToDateTime);
                    } else if (paramInfo.ParameterType.GetElementType().IsSubclassOf(typeof(ManagementClassBase))) { // Return type is a ManagementObject reference
                        LocalBuilder localPath = il.DeclareLocal(typeof(ManagementPath));
                        LocalBuilder localMgmtObj = il.DeclareLocal(typeof(ManagementObject));

                        // Create a management object from the current scope
                        il.Emit(OpCodes.Castclass, typeof(String));
                        il.Emit(OpCodes.Newobj, typeof(ManagementPath).GetConstructor(new[] { typeof(String) }));
                        il.Emit(OpCodes.Stloc_S, localPath);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, getManagementObject);
                        il.Emit(OpCodes.Callvirt, getManagementScope);
                        il.Emit(OpCodes.Ldloc_S, localPath);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Newobj, typeof(ManagementObject).GetConstructor(new[] { typeof(ManagementScope), typeof(ManagementPath), typeof(ObjectGetOptions) }));
                        il.Emit(OpCodes.Stloc_S, localMgmtObj);

                        MethodInfo getTypedFactory = getFactory.MakeGenericMethod(underlyingParamType);

                        il.Emit(OpCodes.Call, getTypedFactory);
                        il.Emit(OpCodes.Ldloc_S, localMgmtObj);

                        Type factoryType = typeof(ManagementClassFactory<>).MakeGenericType(underlyingParamType);
                        MethodInfo createInstance = factoryType.GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(ManagementObject) }, null);

                        il.Emit(OpCodes.Callvirt, createInstance);
                    }

                    LocalBuilder returnValue = il.DeclareLocal(underlyingParamType);

                    // By ref values
                    il.Emit(OpCodes.Stloc_S, returnValue);
                    il.Emit(OpCodes.Ldarg_S, paramIndex + 1); // Address
                    il.Emit(OpCodes.Ldloc_S, returnValue); // Value

                    if (paramInfo.ParameterType.IsValueType) {
                        il.Emit(OpCodes.Box, underlyingParamType);
                    }

                    il.Emit(OpCodes.Stind_Ref); // Store value at address
                    
                    il.Emit(OpCodes.Br_S, propertyIsNotNull);

                    // Property doesn't exist
                    il.MarkLabel(propertyIsNull);

                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldstr, $"'{mgmtNamespacePath}:{mgmtClassName}'.'{realMethodName}' does not return property '{realParamPropertyName}'");
                    il.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new[] { typeof(String) }));
                    il.Emit(OpCodes.Throw);

                    il.MarkLabel(propertyIsNotNull);
                }

                if (methodInfo.ReturnType != typeof(void)) {
                    il.Emit(OpCodes.Ldloc_S, outParams);
                    il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                    il.Emit(OpCodes.Ldstr, returnPropertyName);
                    il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                    Label returnPropertyIsNull = il.DefineLabel();

                    // Check if property exists
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brfalse_S, returnPropertyIsNull);

                    // Property exists
                    il.Emit(OpCodes.Callvirt, getPropertyDataValue);

                    if (returnPropertyType.IsValueType) {
                        il.Emit(OpCodes.Unbox_Any, returnPropertyType);
                    }

                    if (methodInfo.ReturnType.IsSubclassOf(typeof(DateTime))) {
                        // Convert to DateTime if needed
                        il.Emit(OpCodes.Call, convertToDateTime);
                    } else if (methodInfo.ReturnType.IsSubclassOf(typeof(ManagementClassBase))) { // Return type is a ManagementObject reference
                        LocalBuilder localPath = il.DeclareLocal(typeof(ManagementPath));
                        LocalBuilder localMgmtObj = il.DeclareLocal(typeof(ManagementObject));

                        // Create a management object from the current scope
                        il.Emit(OpCodes.Castclass, typeof(String));
                        il.Emit(OpCodes.Newobj, typeof(ManagementPath).GetConstructor(new[] { typeof(String) }));
                        il.Emit(OpCodes.Stloc_S, localPath);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, getManagementObject);
                        il.Emit(OpCodes.Callvirt, getManagementScope);
                        il.Emit(OpCodes.Ldloc_S, localPath);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Newobj, typeof(ManagementObject).GetConstructor(new[] { typeof(ManagementScope), typeof(ManagementPath), typeof(ObjectGetOptions) }));
                        il.Emit(OpCodes.Stloc_S, localMgmtObj);

                        MethodInfo getTypedFactory = getFactory.MakeGenericMethod(methodInfo.ReturnType);

                        il.Emit(OpCodes.Call, getTypedFactory);
                        il.Emit(OpCodes.Ldloc_S, localMgmtObj);

                        Type factoryType = typeof(ManagementClassFactory<>).MakeGenericType(methodInfo.ReturnType);

                        il.Emit(OpCodes.Callvirt, factoryType.GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(ManagementObject) }, null));
                    }

                    if (!methodInfo.ReturnType.IsValueType) {
                        il.Emit(OpCodes.Castclass, methodInfo.ReturnType);
                    }

                    il.Emit(OpCodes.Ret);

                    // Property doesn't exist
                    il.MarkLabel(returnPropertyIsNull);

                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldstr, $"'{mgmtNamespacePath}:{mgmtClassName}'.'{realMethodName}' does not return property '{returnPropertyName}'");
                    il.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new[] { typeof(String) }));
                    il.Emit(OpCodes.Throw);
                } else {
                    il.Emit(OpCodes.Ret);
                }
            }

            return classType.CreateType();
        }

        public static Type GetClassType(Type classDefType) {
            if (ManagementSession._classTypes.ContainsKey(classDefType.FullName)) {
                return ManagementSession._classTypes[classDefType.FullName];
            }

            Type classImplType = ManagementSession.CreateClassType(classDefType);

            // Cache class
            ManagementSession._classTypes[classDefType.FullName] = classImplType;

            return classImplType;
        }

        public static Type GetClassType<T>() where T : ManagementClassBase => ManagementSession.GetClassType(typeof(T));

        // Handle factories
        private static Type CreateFactoryType(Type factoryDefType) {
            if (factoryDefType.BaseType == typeof(Object) && factoryDefType.GetGenericTypeDefinition() == typeof(ManagementClassFactory<>)) {
                return factoryDefType;
            }

            string factoryTypeName = $"Impl_{factoryDefType.Name}";

            if (factoryDefType.BaseType.GetGenericTypeDefinition() != typeof(ManagementClassFactory<>)) {
                throw new Exception($"'{factoryDefType.Name}' does not immediately inherit from '{typeof(ManagementClassFactory<>).Name}'");
            }

            Type classDefType = factoryDefType.BaseType.GetGenericArguments().First();
            Type classType = ManagementSession.GetClassType(classDefType);

            string mgmtNamespacePath = ManagementSession.GetNamespacePath(classDefType);
            string mgmtClassName = ManagementSession.GetClassName(classDefType);

            ManagementClass mgmtClass = new ManagementClass(mgmtNamespacePath, mgmtClassName, null);

            TypeBuilder factoryType = ManagementSession.moduleBuilder.DefineType(factoryTypeName, TypeAttributes.Class | TypeAttributes.NotPublic, factoryDefType);
            BindingFlags defaultBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

            // Predefined method info
            MethodInfo getManagementObject = classType.GetMethod("get_ManagementObject", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo getManagementBaseObjectProperties = typeof(ManagementBaseObject).GetMethod("get_Properties", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo getPropertyDataCollectionItem = typeof(PropertyDataCollection).GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);

            MethodInfo getPropertyDataValue = typeof(PropertyData).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setPropertyDataValue = typeof(PropertyData).GetMethod("set_Value", BindingFlags.Instance | BindingFlags.Public);

            MethodInfo convertToDateTime = typeof(ManagementDateTimeConverter).GetMethod("ToDateTime", BindingFlags.Static | BindingFlags.Public);
            MethodInfo convertFromDateTime = typeof(ManagementDateTimeConverter).GetMethod("ToDmtfDateTime", BindingFlags.Static | BindingFlags.Public);

            IEnumerable<MethodInfo> methodsToImplement = factoryDefType.GetMethods(defaultBindingFlags).Where(m => m.IsAbstract);

            foreach (MethodInfo methodInfo in methodsToImplement) {
                if (methodInfo.Name != "CreateInstance") {
                    throw new Exception($"Unable to implement method '{methodInfo.Name}'. Name is not CreateInstance");
                }

                ParameterInfo[] paramInfos = methodInfo.GetParameters();

                MethodBuilder methodBuilder = factoryType.DefineMethod("CreateInstance", MethodAttributes.Public | MethodAttributes.Virtual, classDefType, paramInfos.Select(p => p.ParameterType).ToArray());
                ILGenerator il = methodBuilder.GetILGenerator();

                LocalBuilder managementClass = il.DeclareLocal(typeof(ManagementClass));

                // managementClass = new ManagementClass(mgmtNamespacePath, mgmtClassName, null)
                il.Emit(OpCodes.Ldstr, mgmtNamespacePath);
                il.Emit(OpCodes.Ldstr, mgmtClassName);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Newobj, typeof(ManagementClass).GetConstructor(new[] { typeof(String), typeof(String), typeof(ObjectGetOptions) }));
                il.Emit(OpCodes.Stloc_S, managementClass);

                for (int paramIndex = 0; paramIndex < paramInfos.Length; paramIndex++) {
                    ParameterInfo paramInfo = paramInfos[paramIndex];

                    ManagementPropertyAttribute paramAttribute = paramInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                    string realParamPropertyName = paramAttribute?.Name ?? paramInfo.Name;
                    Type realParamPropertyType = paramAttribute?.CastAs ?? paramInfo.ParameterType;

                    if (mgmtClass.Properties[realParamPropertyName] == null) {
                        throw new ArgumentException($"'{mgmtNamespacePath}:{mgmtClassName}' does not have key '{realParamPropertyName}'");
                    }

                    // Get property - will always exist, checked above
                    il.Emit(OpCodes.Ldloc_S, managementClass);
                    il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                    il.Emit(OpCodes.Ldstr, realParamPropertyName);
                    il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                    // Get argument, ldarg.0 = this
                    il.Emit(OpCodes.Ldarg_S, paramIndex + 1);

                    if (paramInfo.ParameterType == typeof(DateTime)) {
                        // Convert to DMTF DateTime
                        il.Emit(OpCodes.Call, convertFromDateTime);
                    } else if (realParamPropertyType != paramInfo.ParameterType) {
                        if (!paramInfo.ParameterType.IsValueType) {
                            // TODO: Get converter
                        }
                    }

                    if (realParamPropertyType.IsValueType) {
                        il.Emit(OpCodes.Box, realParamPropertyType);
                    }

                    // Set property value
                    il.Emit(OpCodes.Callvirt, setPropertyDataValue);
                }

                // managementObject = managementClass.CreateInstance()
                il.Emit(OpCodes.Ldloc_S, managementClass);
                il.Emit(OpCodes.Call, typeof(ManagementClass).GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.Public));

                // [classDefType] instance = new [classType](managementObject)
                il.Emit(OpCodes.Newobj, classType.GetConstructor(new[] { typeof(ManagementBaseObject) }));

                // return instance
                il.Emit(OpCodes.Ret);
            }

            return factoryType.CreateType();
        }

        private static Type GetFactoryType(Type factoryDefType) {
            if (ManagementSession._factoryTypes.ContainsKey(factoryDefType.FullName)) {
                return ManagementSession._factoryTypes[factoryDefType.FullName];
            }

            Type factoryImplType = ManagementSession.CreateFactoryType(factoryDefType);

            // Cache factory def
            ManagementSession._factoryTypes[factoryDefType.FullName] = factoryImplType;

            return factoryImplType;
        }

        public static TFactory GetFactory<T, TFactory>() where T : ManagementClassBase where TFactory : ManagementClassFactory<T> {
            if (ManagementSession._factories.ContainsKey(typeof(TFactory).FullName)) {
                return (TFactory)ManagementSession._factories[typeof(TFactory).FullName];
            }

            Type factoryImplType = ManagementSession.GetFactoryType(typeof(TFactory));
            TFactory factoryImpl = (TFactory)Activator.CreateInstance(factoryImplType);

            // Cache factory impl
            ManagementSession._factories[typeof(TFactory).FullName] = factoryImpl;

            return factoryImpl;
        }

        public static ManagementClassFactory<T> GetFactory<T>() where T : ManagementClassBase {
            return ManagementSession.GetFactory<T, ManagementClassFactory<T>>();
        }

        public static void Save() {
            ManagementSession.assemblyBuilder.Save(ManagementSession.assemblyName.Name + ".dll");
        }
    }
}
