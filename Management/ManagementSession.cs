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
        private static IDictionary<Type, object> _factories;

        private static IDictionary<Type, Type> _factoryTypes;
        private static IDictionary<Type, Type> _classTypes;

        private static AssemblyName assemblyName;
        private static AssemblyBuilder assemblyBuilder;
        private static ModuleBuilder moduleBuilder;

        static ManagementSession() {
            ManagementSession._factories = new Dictionary<Type, object>();

            ManagementSession._factoryTypes = new Dictionary<Type, Type>();
            ManagementSession._classTypes = new Dictionary<Type, Type>();

            ManagementSession.assemblyName = new AssemblyName("ManagementClassDynamicImplementations");
            ManagementSession.assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(ManagementSession.assemblyName, AssemblyBuilderAccess.RunAndSave);

#if DEBUG
            Type debugType = typeof(DebuggableAttribute);
            ConstructorInfo debugConInfo = debugType.GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) });
            CustomAttributeBuilder debugAttrBuilder = new CustomAttributeBuilder(debugConInfo, new object[] {
                DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default
            });

            ManagementSession.assemblyBuilder.SetCustomAttribute(debugAttrBuilder);
#endif

            ManagementSession.moduleBuilder = ManagementSession.assemblyBuilder.DefineDynamicModule(ManagementSession.assemblyName.Name, ManagementSession.assemblyName.Name + ".dll");
        }

        public static string GetNamespacePath(Type t) {
            ManagementClassMapAttribute a = Attribute.GetCustomAttribute(t, typeof(ManagementClassMapAttribute)) as ManagementClassMapAttribute;

            return a?.NamespacePath;
        }

        public static string GetClassName(Type t) {
            ManagementClassMapAttribute a = Attribute.GetCustomAttribute(t, typeof(ManagementClassMapAttribute)) as ManagementClassMapAttribute;

            return a?.ClassName;
        }

        // Handle classes
        private static Type CreateClassType(Type classDefType) {
            string classTypeName = $"Impl_{classDefType.Name}";

            if (classDefType.BaseType != typeof(ManagementClassBase) && !classDefType.BaseType.IsSubclassOf(typeof(ManagementClassBase))) {
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

            MethodInfo getPropertyDataValue = typeof(PropertyData).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setPropertyDataValue = typeof(PropertyData).GetMethod("set_Value", BindingFlags.Instance | BindingFlags.Public);

            MethodInfo convertToDateTime = typeof(ManagementDateTimeConverter).GetMethod("ToDateTime", BindingFlags.Static | BindingFlags.Public);
            MethodInfo convertFromDateTime = typeof(ManagementDateTimeConverter).GetMethod("ToDmtfDateTime", BindingFlags.Static | BindingFlags.Public);

            MethodInfo convertTo = typeof(Convert).GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Object), typeof(Type) }, null);

            // Implement default constructor
            ConstructorBuilder defaultConstructor = classType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(ManagementBaseObject) });
            ILGenerator constructorIl = defaultConstructor.GetILGenerator();

            constructorIl.Emit(OpCodes.Ldarg_0);
            constructorIl.Emit(OpCodes.Ldarg_1);
            constructorIl.Emit(OpCodes.Call, classDefType.GetConstructor(new[] { typeof(ManagementBaseObject) }));
            constructorIl.Emit(OpCodes.Ret);

            // Implement properties
            IEnumerable<PropertyInfo> propertiesToImplement = classDefType.GetProperties(defaultBindingFlags);

            Debug.WriteLine(String.Join(", ", propertiesToImplement.Select(p => p.Name).ToArray()));

            foreach (PropertyInfo propertyInfo in propertiesToImplement) {
                // Get real property name and type
                ManagementPropertyAttribute propertyAttribute = propertyInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                string realPropertyName = propertyAttribute?.Name ?? propertyInfo.Name;
                Type realPropertyType = propertyAttribute?.CastAs ?? propertyInfo.PropertyType;

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

                // Declare property if needed
                if (implementGetter || implementSetter) {
                    PropertyBuilder propertyBuilder = classType.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, null);

                    if (implementGetter) {
                        MethodBuilder getterMethodBuilder = classType.DefineMethod(propertyGetterName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, getterMethodInfo.ReturnType, null);
                        ILGenerator il = getterMethodBuilder.GetILGenerator();

                        if (propertyExists) {
                            // Get management object
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, getManagementObject);

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

                            if (propertyInfo.PropertyType == typeof(DateTime)) {
                                // Convert to DateTime if needed
                                il.Emit(OpCodes.Call, convertToDateTime);
                            } else if (realPropertyType != propertyInfo.PropertyType) {
                                if (!propertyInfo.PropertyType.IsValueType) {
                                    // TODO: Get converter
                                }
                            }

                            // Return property value
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
                        } else {
                            il.Emit(OpCodes.Ldstr, $"'{mgmtNamespacePath}:{mgmtClassName}' does not have property '{realPropertyName}");
                            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new[] { typeof(String) }));
                            il.Emit(OpCodes.Throw);
                        }

                        propertyBuilder.SetGetMethod(getterMethodBuilder);
                    }

                    if (implementSetter) {
                        MethodBuilder setterMethodBuilder = classType.DefineMethod(propertySetterName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, getterMethodInfo.ReturnType, null);
                        ILGenerator il = setterMethodBuilder.GetILGenerator();

                        if (propertyExists) {
                            // Get management object
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, getManagementObject);

                            // Get property - will always exist, checked above
                            il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                            il.Emit(OpCodes.Ldstr, realPropertyName);
                            il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                            if (propertyInfo.PropertyType == typeof(DateTime)) {
                                // Convert to DMTF DateTime
                                il.Emit(OpCodes.Call, convertFromDateTime);
                            } else if (realPropertyType != propertyInfo.PropertyType) {
                                if (!propertyInfo.PropertyType.IsValueType) {
                                    // TODO: Get converter
                                }
                            }

                            if (realPropertyType.IsValueType) {
                                il.Emit(OpCodes.Box, realPropertyType);
                            }

                            // Set property value
                            il.Emit(OpCodes.Callvirt, setPropertyDataValue);

                            // Return value
                            il.Emit(OpCodes.Ret);
                        } else {
                            il.Emit(OpCodes.Ldstr, $"'{mgmtNamespacePath}:{mgmtClassName}' does not have property '{realPropertyName}'");
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
                    il.Emit(OpCodes.Ldstr, $"'{mgmtNamespacePath}:{mgmtClassName}' does not have method '{realMethodName}'");
                    il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new[] { typeof(String) }));
                    il.Emit(OpCodes.Throw);

                    continue;
                }

                LocalBuilder inParams = il.DeclareLocal(typeof(ManagementBaseObject));

                if (!paramInfos.Where(p => !(p.IsOut || p.ParameterType.IsByRef)).Any()) {
                    // No parameters
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stloc, inParams);
                } else {
                    // Get method parameters, store in 'inParams'
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, getManagementObject);
                    il.Emit(OpCodes.Ldstr, realMethodName);
                    il.Emit(OpCodes.Call, typeof(ManagementObject).GetMethod("GetMethodParameters", BindingFlags.Instance | BindingFlags.Public));
                    il.Emit(OpCodes.Stloc, inParams);

                    for (int paramIndex = 0; paramIndex < paramInfos.Length; paramIndex++) {
                        ParameterInfo paramInfo = paramInfos[paramIndex];

                        if (paramInfo.IsOut) {
                            continue;
                        }

                        // Get property name and real type
                        ManagementPropertyAttribute paramAttribute = paramInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                        string realParamPropertyName = paramAttribute?.Name ?? paramInfo.Name;
                        Type realParamPropertyType = paramAttribute?.CastAs ?? paramInfo.ParameterType;

                        if (mgmtInParams.Properties[realParamPropertyName] == null) {
                            throw new ArgumentException($"'{mgmtNamespacePath}:{mgmtClassName}'.'{realMethodName}' does not have parameter '{realParamPropertyName}'");
                        }

                        // Get property - will always exist, checked above
                        il.Emit(OpCodes.Ldloc, inParams);
                        il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                        il.Emit(OpCodes.Ldstr, realParamPropertyName);
                        il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                        // Get argument, ldarg.0 = this
                        il.Emit(OpCodes.Ldarg, paramIndex + 1);

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
                }

                LocalBuilder outParams = il.DeclareLocal(typeof(ManagementBaseObject));

                // outParams = this.ManagementObject.InvokeMethod(realMethodName, inParams, null)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, getManagementObject);
                il.Emit(OpCodes.Ldstr, realMethodName);
                il.Emit(OpCodes.Ldloc, inParams);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Callvirt, typeof(ManagementObject).GetMethod("InvokeMethod", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(String), typeof(ManagementBaseObject), typeof(InvokeMethodOptions) }, null));
                il.Emit(OpCodes.Stloc, outParams);

                // Get error code
                il.Emit(OpCodes.Ldloc, outParams);
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
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);

                // No error
                il.MarkLabel(invokeSuccess);

                il.Emit(OpCodes.Pop);

                // Handle out parameters
                for (int paramIndex = 0; paramIndex < paramInfos.Length; paramIndex++) {
                    ParameterInfo paramInfo = paramInfos[paramIndex];

                    if (!(paramInfo.IsOut || paramInfo.ParameterType.IsByRef)) {
                        continue;
                    }

                    // Get property name and real type
                    ManagementPropertyAttribute paramAttribute = paramInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                    string realParamPropertyName = paramAttribute?.Name ?? paramInfo.Name;
                    Type realParamPropertyType = paramAttribute?.CastAs ?? paramInfo.ParameterType;

                    il.Emit(OpCodes.Ldloc, outParams);
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

                    if (paramInfo.ParameterType == typeof(DateTime)) {
                        // Convert to DateTime if needed
                        il.Emit(OpCodes.Call, convertToDateTime);
                    } else if (realParamPropertyType != paramInfo.ParameterType) {
                        if (!paramInfo.ParameterType.IsValueType) {
                            // TODO: Get converter
                        }
                    }

                    il.Emit(OpCodes.Starg, paramIndex + 1);
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
                    il.Emit(OpCodes.Ldloc, outParams);
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

                    if (methodInfo.ReturnType == typeof(DateTime)) {
                        // Convert to DateTime if needed
                        il.Emit(OpCodes.Call, convertToDateTime);
                    } else if (returnPropertyType != methodInfo.ReturnType) {
                        if (!methodInfo.ReturnType.IsValueType) {
                            // TODO: Get converter
                        }
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
            if (ManagementSession._classTypes.ContainsKey(classDefType)) {
                return ManagementSession._classTypes[classDefType];
            }

            Type classImplType = ManagementSession.CreateClassType(classDefType);

            // Cache class
            ManagementSession._classTypes[classDefType] = classImplType;

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
                il.Emit(OpCodes.Stloc, managementClass);

                for (int paramIndex = 0; paramIndex < paramInfos.Length; paramIndex++) {
                    ParameterInfo paramInfo = paramInfos[paramIndex];

                    ManagementPropertyAttribute paramAttribute = paramInfo.GetCustomAttribute(typeof(ManagementPropertyAttribute)) as ManagementPropertyAttribute;

                    string realParamPropertyName = paramAttribute?.Name ?? paramInfo.Name;
                    Type realParamPropertyType = paramAttribute?.CastAs ?? paramInfo.ParameterType;

                    if (mgmtClass.Properties[realParamPropertyName] == null) {
                        throw new ArgumentException($"'{mgmtNamespacePath}:{mgmtClassName}' does not have key '{realParamPropertyName}'");
                    }

                    // Get property - will always exist, checked above
                    il.Emit(OpCodes.Ldloc, managementClass);
                    il.Emit(OpCodes.Callvirt, getManagementBaseObjectProperties);
                    il.Emit(OpCodes.Ldstr, realParamPropertyName);
                    il.Emit(OpCodes.Callvirt, getPropertyDataCollectionItem);

                    // Get argument, ldarg.0 = this
                    il.Emit(OpCodes.Ldarg, paramIndex + 1);

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
                il.Emit(OpCodes.Ldloc, managementClass);
                il.Emit(OpCodes.Call, typeof(ManagementClass).GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.Public));

                // [classDefType] instance = new [classType](managementObject)
                il.Emit(OpCodes.Newobj, classType.GetConstructor(new[] { typeof(ManagementBaseObject) }));

                // return instance
                il.Emit(OpCodes.Ret);
            }

            return factoryType.CreateType();
        }

        private static Type GetFactoryType(Type factoryDefType) {
            if (ManagementSession._factoryTypes.ContainsKey(factoryDefType)) {
                return ManagementSession._factoryTypes[factoryDefType];
            }

            Type factoryImplType = ManagementSession.CreateFactoryType(factoryDefType);

            // Cache factory def
            ManagementSession._factoryTypes[factoryDefType] = factoryImplType;

            return factoryImplType;
        }

        public static TFactory GetFactory<T, TFactory>() where T : ManagementClassBase where TFactory : ManagementClassFactory<T> {
            if (ManagementSession._factories.ContainsKey(typeof(TFactory))) {
                return (TFactory)ManagementSession._factories[typeof(TFactory)];
            }

            Type factoryImplType = ManagementSession.GetFactoryType(typeof(TFactory));
            TFactory factoryImpl = (TFactory)Activator.CreateInstance(factoryImplType);

            // Cache factory impl
            ManagementSession._factories[typeof(TFactory)] = factoryImpl;

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
