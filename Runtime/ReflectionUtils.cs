using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace SiegeUp.Core
{
    public class ReflectionUtils : MonoBehaviour
    {
        public class Method
        {
            public System.Reflection.MethodInfo method;
            public System.Reflection.ParameterInfo[] parameters;
        }

        class MethodMap
        {
            public Method[] methods;
        }

        public delegate void MethodDelegate(object[] args);

        static System.Reflection.Assembly mainAssembly;

        static Dictionary<int, Type> componentsIdMap = new();
        static Dictionary<Type, int> componentsTypeMap = new();

        static Dictionary<Type, MethodMap> componentsTypeMethodsMap = new();

        static ReflectionUtils()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Assembly-CSharp")
                {
                    mainAssembly = assembly;
                }
            }

            foreach (var type in mainAssembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(ComponentId), false);
                if (attributes.Length == 1)
                {
                    var attribute = attributes[0] as ComponentId;
                    if (componentsIdMap.ContainsKey(attribute.Id))
                    {
                        Debug.LogError($"Component duplicate: {type.Name} and {componentsIdMap[attribute.Id].Name} Id: {attribute.Id}");
                        continue;
                    }

                    componentsIdMap.Add(attribute.Id, type);
                    componentsTypeMap.Add(type, attribute.Id);
                    var methodMap = new MethodMap { methods = new Method[100] };
                    componentsTypeMethodsMap.Add(type, methodMap);

                    var bindingFlags = System.Reflection.BindingFlags.Public
                                       | System.Reflection.BindingFlags.NonPublic
                                       | System.Reflection.BindingFlags.Instance;
                    var methods = type.GetMethods(bindingFlags);
                    foreach (var method in methods)
                    {
                        var methodIds = method.GetCustomAttributes(typeof(MethodId), false);
                        if (methodIds.Length == 1)
                        {
                            var realMethod = Array.Find(methods, i => i.Name == method.Name && i.GetCustomAttributes(typeof(MethodId), false).Length == 0);
                            var methodId = methodIds[0] as MethodId;
#if UNITY_EDITOR
                            if (realMethod != null)
                                Debug.Log($"Method: {method.Name} [{methodId.Id}] => {realMethod.Name} ({realMethod.GetParameters().Length})");
                            else
                                Debug.LogError($"Method not found: {method.Name} [{methodId.Id}]");
#endif
                            methodMap.methods[methodId.Id] = new Method
                            {
                                method = method,
                                parameters = realMethod != null ? realMethod.GetParameters() : new System.Reflection.ParameterInfo[0]
                            };
                        }
                    }
                }
            }
        }

        public static string ArgsToString(object[] args)
        {
            string argsStr = "";
            foreach (var arg in args)
            {
                if (arg == null)
                {
                    argsStr += "null";
                }
                else if (arg.GetType() == typeof(byte[]))
                {
                    argsStr += $"[{(arg as byte[]).Length} bytes]";
                }
                else
                {
                    try
                    {
                        argsStr += arg.ToString();
                    }
                    catch
                    {
                        argsStr += "[???]";
                    }
                }

                argsStr += ", ";
            }

            return argsStr;
        }

        public static Type GetComponentById(int id)
        {
            return componentsIdMap[id];
        }

        public static int GetComponentId(Type type)
        {
            if (componentsTypeMap.TryGetValue(type, out int componentId))
                return componentsTypeMap[type];
            return -1;
        }

        public static bool HasAttribute<T>(System.Reflection.MethodInfo method)
        {
            return Array.Find(method.GetCustomAttributes(true), item => item.GetType() == typeof(T)) != null;
        }

        public static Method GetMethodById(Type type, int id)
        {
            var methods = componentsTypeMethodsMap[type];
            return methods.methods[id];
        }

        public static Method GetMethodByName(Type type, string name)
        {
            var methods = componentsTypeMethodsMap[type];
            return Array.Find(methods.methods, i => i != null && i.method.Name == name);
        }

        public static int GetMethodId(System.Reflection.MethodInfo methodInfo)
        {
            var methodIds = methodInfo.GetCustomAttributes(typeof(MethodId), false);
            if (methodIds.Length == 1)
                return (methodIds[0] as MethodId).Id;
            Debug.LogError("Can't find method id for " + methodInfo.Name);
            return -1;
        }

        public static int GetFieldId(System.Reflection.FieldInfo fieldInfo)
        {
            var methodIds = fieldInfo.GetCustomAttributes(typeof(AutoSerializeAttribute), false);
            if (methodIds.Length == 1)
                return (methodIds[0] as AutoSerializeAttribute).Id;
            Debug.LogError("Can't find field id for " + fieldInfo.Name);
            return -1;
        }


        public static Guid StrToGuid(string strUniqueId)
        {
            if (string.IsNullOrEmpty(strUniqueId))
                return default;
            if (strUniqueId.Length == 36 || strUniqueId.Length == 32)
                return new Guid(strUniqueId);
            int start = strUniqueId.LastIndexOf('_');
            if (start != -1)
                return new Guid(strUniqueId.Substring(start + 1));
            Debug.LogError("String Guid seems to be broken " + strUniqueId);
            return new Guid(strUniqueId);
        }

        public static List<Guid> StrsToGuids(List<string> strUniqueIds)
        {
            return strUniqueIds.ConvertAll(item => StrToGuid(item));
        }

        public static System.Reflection.MethodInfo GetMethod(Type type, string methodName)
        {
            var currentType = type;
            do
            {
                var methodInfo = currentType.GetMethod(methodName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (methodInfo != null)
                    return methodInfo;
                currentType = currentType.BaseType;
            } while (currentType != null);

            return null;
        }
    }
}