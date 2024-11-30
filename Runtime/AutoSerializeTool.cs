using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Serialization;
using static SiegeUp.Core.AutoSerializeTool.ClassReflectionCache;

namespace SiegeUp.Core
{


    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class ComponentId : Attribute
    {
        public ComponentId(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }
    }

    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class MethodId : Attribute
    {
        public MethodId(int id = -1)
        {
            Id = id;
        }

        public int Id { get; private set; }
    }

    public class AutoSerializeAttribute : Attribute
    {
        public AutoSerializeAttribute(int id = -1)
        {
            Id = id;
        }

        public int Id { private set; get; }
    }

    public class AlwaysSerializeAttribute : Attribute
    {
    }

    public abstract class ScriptableObjectWithId : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("uid")]
        string id;

        public string Id => id;

        public void ResetId(string id) => this.id = id;

        public void UpdateId()
        {
#if UNITY_EDITOR
            var newId = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
            if (!string.IsNullOrEmpty(newId) && Id != newId)
            {
                ResetId(newId);
                EditorUtility.SetDirty(this);
            }
#endif
        }
    }

    [Serializable]
    public struct AutoSerializedField
    {
        public string name;
        public string value;
    }

    [Serializable]
    public class AutoSerializedObjectBin
    {
        [AutoSerialize(1)]
        public List<AutoSerializedFieldBin> fields = new();
    }

    [Serializable]
    public struct AutoSerializedFieldBin : ISerializationCallbackReceiver
    {
        [AutoSerialize(1)]
        public int id;

        [AutoSerialize(2)]
        public byte[] data;

        [SerializeField]
        string _data;

        public void OnBeforeSerialize()
        {
            if (data == null || data.Length == 0)
                _data = "";
            else
                _data = Convert.ToBase64String(data);
        }

        public void OnAfterDeserialize()
        {
            if (_data != null)
                data = Convert.FromBase64String(_data);
        }
    }

    [Serializable]
    public class AutoSerializedObject
    {
        public List<AutoSerializedField> autoSerializedFields = new();
    }

    public class ObjectContext
    {
        public delegate GameObject FindObjectById(Guid id);

        public PrefabManager prefabManager;
        public ScriptableObjectManager scriptableObjectManager;
        public int formatVersion;
        public FindObjectById findObjectById;
        public object obj;
        public string nameOnScene;
        public Type type;

        public ObjectContext(PrefabManager prefabManager, ScriptableObjectManager scriptableObjectManager, int formatVersion, FindObjectById findObjectById, object obj, string nameOnScene, Type type)
        {
            this.prefabManager = prefabManager;
            this.scriptableObjectManager = scriptableObjectManager;
            this.findObjectById = findObjectById;
            this.obj = obj;
            this.nameOnScene = nameOnScene;
            this.type = type;
            this.formatVersion = formatVersion;
        }

        public ObjectContext(ObjectContext parentContext, object obj, Type type) 
            : this(parentContext.prefabManager, parentContext.scriptableObjectManager, parentContext.formatVersion, parentContext.findObjectById, obj, parentContext.nameOnScene, type)
        {
        }
    }

    public class AutoSerializeTool
    {
        public class ClassReflectionCache
        {
            public struct FieldCache
            {
                public int id;
                public bool alwaysSerialize;
                public FieldInfo fieldInfo;
            }

            public List<FieldCache> fields;
        }

        struct ListValue
        {
            public List<string> list;
        }

        struct BoundsValue
        {
            public Vector3 position;
            public Vector3 size;
        }

        static Dictionary<string, ClassReflectionCache> classReflectionCaches = new();
        static Dictionary<string, bool> hasAutoSerializeAttributeMap = new();
        static Dictionary<string, Type> cachedTypesMap = new();
        static Dictionary<string, Type> nestedTypesMapLegacy = new();
        static Dictionary<string, Type> nestedTypesMap = new();

        public static string ExtractId(GameObject targetObject)
        {
            return targetObject == null ? "" : targetObject.GetComponent<UniqueId>()?.StringId;
        }

        public static string ExtractId(Component targetObject)
        {
            return targetObject == null ? "" : ExtractId(targetObject.gameObject);
        }

        public static MethodInfo GetMethod(Type componentType, string methodName)
        {
            var method = ReflectionUtils.GetMethodByName(componentType, methodName).method;
            return method;
        }

        public static FieldInfo GetField(Type componentType, string fieldName)
        {
            var bindingFlags = BindingFlags.Public
                               | BindingFlags.NonPublic
                               | BindingFlags.Instance;
            var field = componentType.GetField(fieldName, bindingFlags);
            return field;
        }

        public static Guid ExtractGuid(GameObject targetObject)
        {
            if (targetObject == null)
                return default;
            var uniqueId = targetObject.GetComponent<UniqueId>();
            if (!uniqueId)
                return default;
            return uniqueId.GetGuid();
        }

        public static Guid ExtractGuid(Component targetObject)
        {
            return targetObject == null ? default : ExtractGuid(targetObject.gameObject);
        }

        public static ClassReflectionCache GetClassReflectionCache(Type type)
        {
            ClassReflectionCache cache;
            if (classReflectionCaches.TryGetValue(type.FullName, out cache))
            {
                return cache;
            }

            var fieldsToSerialize = GetListOfFields(type);
            fieldsToSerialize.RemoveAll(item => item.GetCustomAttribute<AutoSerializeAttribute>() == null);
            var fields = fieldsToSerialize.ConvertAll(item => new ClassReflectionCache.FieldCache
            {
                id = item.GetCustomAttribute<AutoSerializeAttribute>().Id,
                fieldInfo = item,
                alwaysSerialize = item.GetCustomAttribute<AlwaysSerializeAttribute>() != null
            });

            if (HasDuplicateIds(fields, type))
                return null;

            cache = new ClassReflectionCache { fields = fields };
            classReflectionCaches.Add(type.FullName, cache);
            return cache;
        }

        public static bool HasDuplicateIds(List<FieldCache> fields, Type type)
        {
            var duplicateIds = fields
                .GroupBy(field => field.id)
                .Where(group => group.Count() > 1)
                .ToList();

            bool hasDuplicatedIds = false;
            foreach (var group in duplicateIds)
            {
                hasDuplicatedIds = true;
                Debug.LogError($"Error: Duplicate id {group.Key} found for fields: {string.Join(", ", group.Select(field => field.fieldInfo.Name))} in type {type.Name}");
            }
            return hasDuplicatedIds;
        }

        public static List<FieldInfo> GetListOfFields(Type type)
        {
            var fieldsToSerialize = new List<FieldInfo>(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            var currentType = type;
            while (currentType.BaseType != null)
            {
                fieldsToSerialize.AddRange(currentType.BaseType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                currentType = currentType.BaseType;
            }
            return fieldsToSerialize;
        }

        public static int ReadFileHeader(byte[] bytes, ref int pos, string expectedMagic)
        {
            if (bytes.Length < expectedMagic.Length + sizeof(short))
                throw new Exception($"File is too short. Can't be a SiegeUp binary {expectedMagic} file.");
            string magic = BinaryUtil.ReadString(ref bytes, pos, 4);
            pos += magic.Length;
            if (magic != expectedMagic)
                throw new Exception($"Wrong file format. SiegeUp binary {expectedMagic} file is expected. Make sure the game was saved in SiegeUp ver 1.1.0 or above.");
            int formatVersion = BinaryUtil.ReadInt16(ref bytes, pos);
            pos += sizeof(short);
            return formatVersion;
        }

        public static int WriteFileHeader(ref byte[] bytes, ref int pos, string magic, int formatVersion)
        {
            int targetSize = magic.Length + sizeof(short);
            if (bytes.Length < targetSize)
                Array.Resize(ref bytes, targetSize);
            pos += BinaryUtil.WriteString(ref bytes, pos, magic);
            pos += BinaryUtil.WriteInt16(ref bytes, pos, (short)formatVersion);
            return formatVersion;
        }

        public static void Write(ref byte[] dest, ref int pos, Type type, object val)
        {
            if (!BitConverter.IsLittleEndian)
                throw new Exception("Big endian is not supported!");

            if (type.IsSubclassOf(typeof(Component)))
            {
                if (type.IsAssignableFrom(typeof(PrefabRef)))
                {
                    var prefabRef = val as PrefabRef;
                    var guid = prefabRef.GetGuid();
                    pos += BinaryUtil.WriteGuid(ref dest, pos, guid);
                }
                else
                {
                    var behaviour = val as Component;
                    var guid = ExtractGuid(behaviour);
                    pos += BinaryUtil.WriteGuid(ref dest, pos, guid);
                }
            }
            else if (typeof(ScriptableObjectWithId).IsAssignableFrom(type))
            {
                var translation = val as ScriptableObjectWithId;
                var resultStr = translation && translation != null ? translation.Id : "";
                Write(ref dest, ref pos, typeof(string), resultStr);
            }
            else if (type.IsAssignableFrom(typeof(GameObject)))
            {
                var obj = val as GameObject;
                var guid = ExtractGuid(obj);
                pos += BinaryUtil.WriteGuid(ref dest, pos, guid);
            }
            else if (type.IsValueType)
            {
                if (val is float)
                {
                    pos += BinaryUtil.WriteSingle(ref dest, pos, (float)val);
                }
                else if (val is int || type.IsEnum)
                {
                    pos += BinaryUtil.WriteInt32(ref dest, pos, (int)val);
                }
                else if (val is long)
                {
                    pos += BinaryUtil.WriteInt64(ref dest, pos, (long)val);
                }
                else if (val is short)
                {
                    pos += BinaryUtil.WriteInt16(ref dest, pos, (short)val);
                }
                else if (type == typeof(byte))
                {
                    pos += BinaryUtil.WriteByte(ref dest, pos, (byte)val);
                }
                else if (type == typeof(bool))
                {
                    pos += BinaryUtil.WriteBoolean(ref dest, pos, (bool)val);
                }
                else if (type == typeof(Guid))
                {
                    pos += BinaryUtil.WriteGuid(ref dest, pos, (Guid)val);
                }
                else if (type.IsAssignableFrom(typeof(Bounds)))
                {
                    var bounds = (Bounds)val;
                    var boundsValue = new BoundsValue { position = bounds.center, size = bounds.size };
                    pos += BinaryUtil.WriteSingle(ref dest, pos, bounds.center.x);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, bounds.center.y);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, bounds.center.z);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, bounds.size.x);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, bounds.size.y);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, bounds.size.z);
                }
                else if (type.IsAssignableFrom(typeof(Vector2Int)))
                {
                    var vec = (Vector2Int)val;
                    pos += BinaryUtil.WriteInt32(ref dest, pos, vec.x);
                    pos += BinaryUtil.WriteInt32(ref dest, pos, vec.y);
                }
                else if (type.IsAssignableFrom(typeof(Vector3)))
                {
                    var vec = (Vector3)val;
                    pos += BinaryUtil.WriteSingle(ref dest, pos, vec.x);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, vec.y);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, vec.z);
                }
                else if (type.IsAssignableFrom(typeof(Quaternion)))
                {
                    var q = (Quaternion)val;
                    pos += BinaryUtil.WriteSingle(ref dest, pos, q.x);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, q.y);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, q.z);
                    pos += BinaryUtil.WriteSingle(ref dest, pos, q.w);
                }
                else // struct
                {
                    WriteObject(ref dest, ref pos, val);
                }
            }
            else if (type == typeof(string))
            {
                var str = (string)val;
                int lengthPos = pos;
                pos += sizeof(int);
                int strLength = BinaryUtil.WriteString(ref dest, pos, str);
                pos += strLength;
                BinaryUtil.WriteInt32(ref dest, lengthPos, strLength);
            }
            else if (type == typeof(byte[]))
            {
                var bytes = (byte[])val;
                pos += BinaryUtil.WriteInt32(ref dest, pos, bytes.Length);
                pos += BinaryUtil.WriteBytes(ref dest, pos, bytes);
            }
            else if (type.IsArray)
            {
                if (val != null)
                {
                    var array = val as Array;
                    var elementType = type.GetElementType();
                    pos += BinaryUtil.WriteInt32(ref dest, pos, array.Length);
                    int nullItemPos = pos;
                    pos += sizeof(int);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var item = array.GetValue(i);
                        if (item == null)
                        {
                            BinaryUtil.WriteInt32(ref dest, nullItemPos, i);
                            nullItemPos = pos;
                            pos += sizeof(int);
                        }
                        else
                        {
                            Write(ref dest, ref pos, elementType, item);
                        }
                    }
                    BinaryUtil.WriteInt32(ref dest, nullItemPos, -1);
                }
            }
            else if (Array.Find(type.GetInterfaces(), item => item == typeof(IList)) != null)
            {
                if (val != null)
                {
                    var list = val as IList;
                    pos += BinaryUtil.WriteInt32(ref dest, pos, list.Count);
                    int nullItemPos = pos;
                    pos += sizeof(int);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item == null)
                        {
                            BinaryUtil.WriteInt32(ref dest, nullItemPos, i);
                            nullItemPos = pos;
                            pos += sizeof(int);
                        }
                        else
                        {
                            Write(ref dest, ref pos, type.GenericTypeArguments[0], item);
                        }
                    }

                    BinaryUtil.WriteInt32(ref dest, nullItemPos, -1);
                }
            }
            else if (type.IsClass)
            {
                WriteObject(ref dest, ref pos, val);
            }
            else
            {
                throw new Exception("Unknown value type " + type.Name);
            }
        }

        public static List<AutoSerializedFieldBin> SerializeObjectFields(object obj, object baseObj)
        {
            var serializedFields = new List<AutoSerializedFieldBin>();

            var type = obj.GetType();
            var cache = GetClassReflectionCache(type);
            foreach (var field in cache.fields)
            {
                var value = field.fieldInfo.GetValue(obj);
                if (value != null)
                {
                    var baseValue = baseObj != null ? field.fieldInfo.GetValue(baseObj) : null;
                    if (field.alwaysSerialize || !value.Equals(baseValue))
                        serializedFields.Add(new AutoSerializedFieldBin { id = field.id, data = Serialize(value, field.fieldInfo.FieldType) });
                }
            }

            return serializedFields;
        }

        public static byte[] Serialize(object obj, Type type)
        {
            var bytes = new byte[1024];
            int pos = 0;
            Write(ref bytes, ref pos, type, obj);
            Array.Resize(ref bytes, pos);
            return bytes;
        }

        public static void WriteObject(ref byte[] dest, ref int pos, object obj)
        {
            var type = obj.GetType();
            var cache = GetClassReflectionCache(type);
            int countPos = pos;
            int count = 0;
            pos += sizeof(byte);
            foreach (var field in cache.fields)
            {
                var value = field.fieldInfo.GetValue(obj);
                if (value != null)
                {
                    pos += BinaryUtil.WriteByte(ref dest, pos, (byte)field.id);
                    Write(ref dest, ref pos, field.fieldInfo.FieldType, value);
                    count++;
                }
            }

            BinaryUtil.WriteByte(ref dest, countPos, (byte)count);
        }

        public static unsafe object Read(byte[] source, ref int pos, Type type, ObjectContext context)
        {
            object result = null;

            if (type.IsSubclassOf(typeof(Component)) || type.IsAssignableFrom(typeof(Transform)))
            {
                if (type == typeof(PrefabRef))
                {
                    var guid = BinaryUtil.ReadGuid(ref source, pos);
                    pos += sizeof(Guid);
                    if (guid != Guid.Empty)
                    {
                        var prefabObj = context.prefabManager.GetPrefab(guid);
                        if (prefabObj)
                            result = prefabObj.GetComponent<PrefabRef>();
                        else
                            Debug.LogError($"Prefab is not found {guid}");
                    }
                }
                else
                {
                    var guid = BinaryUtil.ReadGuid(ref source, pos);
                    pos += sizeof(Guid);
                    if (guid != Guid.Empty)
                    {
                        var obj = context.findObjectById(guid);
                        result = obj?.GetComponent(type);
                    }
                }
            }
            else if (typeof(ScriptableObjectWithId).IsAssignableFrom(type))
            {
                var str = Read(source, ref pos, typeof(string), context) as string;
                result = context.scriptableObjectManager.GetScriptableObject(str);
            }
            else if (type.IsAssignableFrom(typeof(GameObject)))
            {
                var guid = BinaryUtil.ReadGuid(ref source, pos);
                pos += sizeof(Guid);
                if (guid != Guid.Empty)
                {
                    var obj = context.findObjectById(guid);
                    result = obj;
                }
            }
            else if (type.IsValueType)
            {
                if (type == typeof(float))
                {
                    result = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                }
                else if (type.IsEnum || type == typeof(int))
                {
                    result = BinaryUtil.ReadInt32(ref source, pos);
                    pos += sizeof(int);
                }
                else if (type == typeof(long))
                {
                    result = BinaryUtil.ReadInt64(ref source, pos);
                    pos += sizeof(long);
                }
                else if (type == typeof(short))
                {
                    result = BinaryUtil.ReadInt16(ref source, pos);
                    pos += sizeof(short);
                }
                else if (type == typeof(byte))
                {
                    result = BinaryUtil.ReadByte(ref source, pos);
                    pos += sizeof(byte);
                }
                else if (type == typeof(bool))
                {
                    result = BinaryUtil.ReadBoolean(ref source, pos);
                    pos += sizeof(bool);
                }
                else if (type == typeof(Guid))
                {
                    var guid = BinaryUtil.ReadGuid(ref source, pos);
                    pos += sizeof(Guid);
                    result = guid;
                }
                else if (type.IsAssignableFrom(typeof(Bounds)))
                {
                    float x = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float y = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float z = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float sX = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float sY = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float sZ = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    result = new Bounds(new Vector3(x, y, z), new Vector3(sX, sY, sZ));
                }
                else if (type.IsAssignableFrom(typeof(Vector2Int)))
                {
                    int x = BinaryUtil.ReadInt32(ref source, pos);
                    pos += sizeof(int);
                    int y = BinaryUtil.ReadInt32(ref source, pos);
                    pos += sizeof(int);
                    result = new Vector2Int(x, y);
                }
                else if (type.IsAssignableFrom(typeof(Vector3)))
                {
                    float x = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float y = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float z = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    result = new Vector3(x, y, z);
                }
                else if (type.IsAssignableFrom(typeof(Quaternion)))
                {
                    float x = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float y = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float z = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    float w = BinaryUtil.ReadSingle(ref source, pos);
                    pos += sizeof(float);
                    result = new Quaternion(x, y, z, w);
                }
                else if (!type.IsPrimitive) // struct
                {
                    object instance = Activator.CreateInstance(type);
                    var nestedContext = new ObjectContext(context, instance, type);
                    ReadObject(ref source, ref pos, nestedContext);
                    result = instance;
                }
                else
                {
                    Debug.LogError("Can't deserialize, unknown type " + type.Name);
                }
            }
            else if (type == typeof(string))
            {
                int length = BinaryUtil.ReadInt32(ref source, pos);
                pos += sizeof(int);
                result = BinaryUtil.ReadString(ref source, pos, length);
                pos += length;
            }
            else if (type == typeof(byte[]))
            {
                int length = BinaryUtil.ReadInt32(ref source, pos);
                pos += sizeof(int);
                result = BinaryUtil.ReadBytes(ref source, pos, length);
                pos += length;
            }
            else if (type.IsArray)
            {
                int length = BinaryUtil.ReadInt32(ref source, pos);
                pos += sizeof(int);

                int nextNullIndex = BinaryUtil.ReadInt32(ref source, pos);
                pos += sizeof(int);

                var elementType = type.GetElementType();

                Array array = Array.CreateInstance(elementType, length);

                for (int i = 0; i < length; i++)
                {
                    if (i == nextNullIndex)
                    {
                        array.SetValue(null, i);

                        nextNullIndex = BinaryUtil.ReadInt32(ref source, pos);
                        pos += sizeof(int);
                    }
                    else
                    {
                        var item = Read(source, ref pos, elementType, context);
                        array.SetValue(item, i);
                    }
                }
                result = array;
            }
            else if (Array.Find(type.GetInterfaces(), item => item == typeof(IList)) != null)
            {
                int length = BinaryUtil.ReadInt32(ref source, pos);
                pos += sizeof(int);
                int nextNullIndex = BinaryUtil.ReadInt32(ref source, pos);
                pos += sizeof(int);
                IList instance = Activator.CreateInstance(type) as IList;
                for (int i = 0; i < length; i++)
                {
                    if (i == nextNullIndex)
                    {
                        instance.Add(null);
                        nextNullIndex = BinaryUtil.ReadInt32(ref source, pos);
                        pos += sizeof(int);
                    }
                    else
                    {
                        var item = Read(source, ref pos, type.GenericTypeArguments[0], context);
                        instance.Add(item);
                    }
                }

                result = instance;
            }
            else if (type.IsClass)
            {
                object instance = Activator.CreateInstance(type);
                var nestedContext = new ObjectContext(context, instance, type);
                ReadObject(ref source, ref pos, nestedContext);
                result = instance;
            }

            return result;
        }

        public static object Deserialize(byte[] bytes, Type type, ObjectContext context)
        {
            int pose = 0;
            return Read(bytes, ref pose, type, context);
        }

        public static void ReadObject(ref byte[] source, ref int pos, ObjectContext context)
        {
            int count = BinaryUtil.ReadByte(ref source, pos);
            pos += sizeof(byte);

            var cache = GetClassReflectionCache(context.type);
            for (int i = 0; i < count; i++)
            {
                int id = BinaryUtil.ReadByte(ref source, pos);
                pos += sizeof(byte);

                int fieldIndex = cache.fields.FindIndex(item => item.id == id);

                if (fieldIndex != -1)
                {
                    var field = cache.fields[fieldIndex];
                    var value = Read(source, ref pos, field.fieldInfo.FieldType, context);
                    if (value != null && value.GetType() != typeof(object))
                    {
                        field.fieldInfo.SetValue(context.obj, value);
                    }
                    // ignore
                }
                else
                {
                    throw new Exception("Data can't be read due to unknown size. No such field in object " + context.type.Name + " Id:" + id + " Offset: " + pos + " Index: " + i + "/" + count);
                }
            }
        }

        public static void DeserializeObjectFields(List<AutoSerializedFieldBin> serializedFields, ObjectContext context)
        {
            var cache = GetClassReflectionCache(context.type);
            foreach (var serializedField in serializedFields)
            {
                var fieldIndex = cache.fields.FindIndex(item => item.id == serializedField.id);
                if (fieldIndex == -1)
                    continue;
                var field = cache.fields[fieldIndex];
                int pos = 0;
                var value = Read(serializedField.data, ref pos, field.fieldInfo.FieldType, context);
                if (value != null && value.GetType() != typeof(object))
                {
                    field.fieldInfo.SetValue(context.obj, value);
                }
            }
        }







        public static SerializedGameObjectBin SerializeBin(GameObject targetObject)
        {
            var serializedGameObject = new SerializedGameObjectBin();
            serializedGameObject.id = targetObject.GetComponent<UniqueId>().GetGuid();
            var prefabRef = targetObject.GetComponent<PrefabRef>();
            if (prefabRef != null)
            {
                serializedGameObject.prefabRef = Service<PrefabManager>.Instance.GetPrefab(prefabRef).GetComponent<PrefabRef>();
                serializedGameObject.name = null;
            }
            else
            {
                serializedGameObject.name = targetObject.name;
            }

            serializedGameObject.position = targetObject.transform.position;
            serializedGameObject.rotation = targetObject.transform.rotation;

            SerializeComponentsBin(serializedGameObject, targetObject, serializedGameObject.prefabRef);

            return serializedGameObject;
        }

        static bool HasAutoSerializeAttribute(Type type)
        {
            bool result = false;
            if (!hasAutoSerializeAttributeMap.TryGetValue(type.FullName, out result))
            {
                result = System.Array.Find(type.GetCustomAttributes(true), item => item.GetType() == typeof(AutoSerializeAttribute)) != null;
                hasAutoSerializeAttributeMap.Add(type.FullName, result);
            }

            return result;
        }


        public static bool IsSerializableComponent(Type componentType)
        {
            return !(typeof(Transform).IsAssignableFrom(componentType)) && ReflectionUtils.GetComponentId(componentType) != -1;
        }

        static void SerializeComponentsBin(SerializedGameObjectBin serializedGameObject, GameObject targetObject, PrefabRef prefab)
        {
            serializedGameObject.serializedComponents = new List<SerializedComponentBin>();
            var prefabComponents = prefab ? prefab.GetComponents<Component>() : null;
            var components = targetObject.GetComponents<Component>();
            if (prefabComponents != null && components.Length != prefabComponents.Length)
                prefabComponents = null;
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                    continue;

                var componentType = component.GetType();

                if (componentType == typeof(Transform))
                    continue;

                int componentId = ReflectionUtils.GetComponentId(component.GetType());

                if (componentId == -1)
                    continue;

                object baseComponent = null;
                if (prefabComponents != null)
                {
                    baseComponent = prefabComponents[i];
                    if (baseComponent.GetType() != component.GetType())
                        baseComponent = null;
                }

                var serializedComponent = new SerializedComponentBin {
                    id = componentId,
                    autoSerialize = new AutoSerializedObjectBin { fields = AutoSerializeTool.SerializeObjectFields(component, baseComponent) }
                };

                object serializedStruct = SerializeComponent(componentType, component);
                if (serializedStruct != null)
                {
                    var bytes = AutoSerializeTool.Serialize(serializedStruct, serializedStruct.GetType());
                    serializedComponent.data = bytes;
                }

                if (serializedComponent.data != null
                    || serializedComponent.autoSerialize.fields.Count > 0
                    || HasAutoSerializeAttribute(componentType))
                {
                    serializedGameObject.serializedComponents.Add(serializedComponent);
                }
            }
        }

        static Type GetTypeCached(string name)
        {
            Type result;
            if (!cachedTypesMap.TryGetValue(name, out result))
            {
                result = System.Type.GetType(name);
                cachedTypesMap.Add(name, result);
            }

            return result;
        }


        static Type GetNestedType(Type type, string nestedName)
        {
            var currentType = type;
            do
            {
                var dataStructType = currentType.GetNestedType(nestedName);
                if (dataStructType != null)
                    return dataStructType;
                currentType = currentType.BaseType;
            } while (currentType != null);

            return null;
        }

        static Type GetNestedTypeCachedLegacy(Type componentType)
        {
            var name = componentType.FullName;
            Type result;
            if (!nestedTypesMapLegacy.TryGetValue(name, out result))
            {
                result = GetNestedType(componentType, "Legacy_DATA");
                nestedTypesMapLegacy.Add(name, result);
            }

            return result;
        }

        static Type GetNestedTypeCached(Type componentType)
        {
            var name = componentType.FullName;
            Type result;
            if (!nestedTypesMap.TryGetValue(name, out result))
            {
                result = GetNestedType(componentType, "DATA");
                nestedTypesMap.Add(name, result);
            }

            return result;
        }

        static object SerializeComponent(Type componentType, Component component)
        {
            object serializedStruct = null;
            var serializeClass = GetTypeCached("DATA_" + componentType.Name);
            if (serializeClass != null)
            {
                var serializeMethod = serializeClass.GetMethod("DATA_Serialize");
                serializedStruct = serializeMethod.Invoke(null, new object[] { component });
            }
            else
            {
                var dataStructType = GetNestedTypeCached(componentType);
                if (dataStructType != null)
                {
                    var serializeMethod = ReflectionUtils.GetMethod(componentType, "DATA_Serialize");

                    if (serializeMethod != null)
                    {
                        try
                        {
                            serializedStruct = serializeMethod.Invoke(component, null);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Can't serialize component " + component.GetType().Name);
                            Debug.LogException(e);
                        }
                    }
                }
            }

            return serializedStruct;
        }


        public static List<SerializedGameObjectBin> SerializeGameObjects(UniqueId[] uniqueIds)
        {
            var elements = new List<SerializedGameObjectBin>();
            foreach (var uniqueId in uniqueIds)
            {
                if (uniqueId.transform.parent == null || uniqueId.transform.parent.name != "SceneModels")
                    elements.Add(SerializeBin(uniqueId.gameObject));
            }

            return elements;
        }

        static void DeserializeComponentBin(byte[] data, Component component, Type componentType, RestoreProcess restoreProcess)
        {
            try
            {
                var deserializeClass = GetTypeCached("DATA_" + componentType.Name);
                if (deserializeClass != null)
                {
                    var dataStructType = deserializeClass.GetNestedType("DATA");
                    var deserializeMethod = deserializeClass.GetMethod("DATA_Deserialize");
                    var context = new ObjectContext(Service<PrefabManager>.Instance,
                        Service<ScriptableObjectManager>.Instance,
                        restoreProcess.formatVersion,
                        id => FindObjectById(id, restoreProcess),
                        component,
                        component.gameObject.name,
                        null);
                    var deserialized = Deserialize(data, dataStructType, context);
                    deserializeMethod.Invoke(null, new[] { component, deserialized, restoreProcess });
                }
                else
                {
                    var dataStructType = GetNestedTypeCached(componentType);
                    if (dataStructType != null)
                    {
                        var context = new ObjectContext(Service<PrefabManager>.Instance,
                            Service<ScriptableObjectManager>.Instance,
                            restoreProcess.formatVersion,
                            id => FindObjectById(id, restoreProcess),
                            component,
                            component.gameObject.name,
                            null);

                        var deserialized = Deserialize(data, dataStructType, context);
                        if (deserialized != null)
                        {
                            var deserializeMethod = ReflectionUtils.GetMethod(componentType, "DATA_Deserialize");
                            deserializeMethod?.Invoke(component, new[] { deserialized, restoreProcess });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Can't deserialize component " + componentType.Name);
                Debug.LogException(e);
            }
        }

        public static void DeserializeComponentsBin(SerializedGameObjectBin element, GameObject targetObject, RestoreProcess restoreProcess)
        {
            var targetObjectPos = element.position;
            if (restoreProcess.version == 0)
            {
                targetObjectPos += new Vector3(50, 0, 50);
            }

            targetObject.transform.position = targetObjectPos;
            targetObject.transform.rotation = element.rotation;
            var deserializedComponents = targetObject.GetComponents<Component>();
            var components = new List<Component>(deserializedComponents);
            foreach (var serializedComponent in element.serializedComponents)
            {
                Component componentToDelete = null;
                foreach (var component in components)
                {
                    if (component == null)
                        continue;
                    var componentType = component.GetType();
                    int componentId = ReflectionUtils.GetComponentId(componentType);
                    if (componentId != -1 && serializedComponent.id == componentId)
                    {
                        var context = new ObjectContext(
                            Service<PrefabManager>.Instance,
                            Service<ScriptableObjectManager>.Instance,
                            restoreProcess.formatVersion,
                            id => FindObjectById(id, restoreProcess),
                            component,
                            component.gameObject.name,
                            componentType);

                        if (serializedComponent.data != null)
                        {
                            DeserializeComponentBin(serializedComponent.data, component, componentType, restoreProcess);
                        }

                        AutoSerializeTool.DeserializeObjectFields(serializedComponent.autoSerialize.fields, context);

                        componentToDelete = component;
                        break;
                    }
                }

                if (componentToDelete != null) // We remove processed component from list to allow multiple components deserialization
                    components.Remove(componentToDelete);
            }

            foreach (var component in deserializedComponents)
            {
                if (component)
                {
                    var componentType = component.GetType();

                    var onDeserializedMethod = ReflectionUtils.GetMethod(componentType, "OnDeserialized");
                    if (onDeserializedMethod != null)
                    {
                        try
                        {
                            onDeserializedMethod.Invoke(component, new object[] { restoreProcess });
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }

        public static GameObject CreateObjectBin(SerializedGameObjectBin element, RestoreProcess restoreProcess)
        {
            var targetObjectPos = element.position;
            if (restoreProcess.version == 0)
            {
                targetObjectPos += new Vector3(50, 0, 50);
            }

            GameObject prefab = null;
            if (element.prefabRef != null)
                prefab = Service<PrefabManager>.Instance.GetPrefab(element.prefabRef);
            if (prefab != null && restoreProcess.bounds != default && !restoreProcess.bounds.Contains(targetObjectPos))
            {
                Debug.LogError($"Object is out of world bounds {restoreProcess.bounds}. Discard. Obj: {element.name} Pos: {targetObjectPos}");
                return null;
            }

            if (restoreProcess.ignoredComponentTypes != null)
            {
                foreach (var ignoredComponentType in restoreProcess.ignoredComponentTypes)
                {
                    if (element.HasComponent(ignoredComponentType))
                        return null;
                }
            }

            if (element.name == null && prefab == null)
            {
                // This is not a trigger-like object because it has no name. At same time, it has no assigned prefab. This means, the prefab is not existant.
                return null;
            }

            GameObject targetObject = null;
            if (prefab == null)
            {
                targetObject = new GameObject();
                targetObject.transform.SetParent(restoreProcess.parent, false);
                targetObject.transform.position = targetObjectPos;
                targetObject.transform.rotation = element.rotation;
                targetObject.name = element.name.Replace("(Clone)", "");
                var newUniqueId = targetObject.AddComponent<UniqueId>();
                newUniqueId.ResetId(element.id);
                restoreProcess.add(newUniqueId);

                foreach (var serializedComponent in element.serializedComponents)
                {
                    var componentType = ReflectionUtils.GetComponentById(serializedComponent.id);
                    if (componentType != null)
                    {
                        targetObject.AddComponent(componentType);
                    }
                    else
                    {
                        Debug.LogError("Can't create component because type can't be found " + serializedComponent.id);
                    }
                }
            }
            else
            {
                targetObject = restoreProcess.spawn(prefab, targetObjectPos, element.rotation, element.id);
                targetObject.name = prefab.name;
            }

            if (targetObject == null)
                return null;

            var uniqueId = targetObject.GetComponent<UniqueId>();
            try
            {
                restoreProcess.uniqueIdsOnScene[element.id] = uniqueId;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            uniqueId.ResetId(element.id);

            return targetObject;
        }

        public static List<GameObject> RestoreObjects(RestoreProcess restoreProcess)
        {
            var createdObjects = new List<GameObject>();

            foreach (var serializedGameObjectPair in restoreProcess.serializedGameObjectsBin)
            {
                var targetObject = FindObjectById(serializedGameObjectPair.Value.id, restoreProcess);

                if (targetObject == null)
                {
                    targetObject = CreateObjectBin(serializedGameObjectPair.Value, restoreProcess);
                    if (targetObject == null)
                        continue;
                }

                if (serializedGameObjectPair.Value.serializedComponents != null)
                    DeserializeComponentsBin(serializedGameObjectPair.Value, targetObject, restoreProcess);
                createdObjects.Add(targetObject);
            }

            return createdObjects;
        }

        public static void RestoreScene(RestoreProcess restoreProcess)
        {
            RestoreObjects(restoreProcess);

            foreach (var uniqueId in restoreProcess.uniqueIdsOnScene)
            {
                bool noAmongSerializedBin = restoreProcess.serializedGameObjectsBin == null || !restoreProcess.serializedGameObjectsBin.ContainsKey(uniqueId.Key);

                if (uniqueId.Value.transform.parent != null && uniqueId.Value.transform.parent == restoreProcess.parent && noAmongSerializedBin)
                    restoreProcess.destroy(uniqueId.Value.gameObject);
            }
        }

        public static GameObject FindObjectByIdDefault(Guid id, RestoreProcess restoreProcess)
        {
            UniqueId onSceneObject;
            if (restoreProcess.uniqueIdsOnScene != null && restoreProcess.uniqueIdsOnScene.TryGetValue(id, out onSceneObject))
                return onSceneObject ? onSceneObject.gameObject : null;
            SerializedGameObjectBin serializedObjectBin;
            if (restoreProcess.serializedGameObjectsBin != null && restoreProcess.serializedGameObjectsBin.TryGetValue(id, out serializedObjectBin))
                return CreateObjectBin(serializedObjectBin, restoreProcess);
            Debug.LogError($"Can't find or instantiate object {id}");
            return null;
        }

        public static GameObject FindObjectById(Guid id, RestoreProcess restoreProcess)
        {
            if (restoreProcess.findObjectById != null)
            {
                var foundObj = restoreProcess.findObjectById(id);
                if (foundObj != null)
                    return foundObj;
            }
            return FindObjectByIdDefault(id, restoreProcess);
        }
    }
}