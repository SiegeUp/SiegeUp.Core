﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.Core
{
    public class ComponentId : Attribute
    {
        public ComponentId(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }
    }

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
        public abstract string Id { get; }

        public abstract void ResetId(string id);

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
    public struct AutoSerialisedField
    {
        public string name;
        public string value;
    }

    [Serializable]
    public class AutoSerialisedObjectBin
    {
        [AutoSerialize(1)]
        public List<AutoSerialisedFieldBin> fields = new();
    }

    [Serializable]
    public struct AutoSerialisedFieldBin : ISerializationCallbackReceiver
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
    public class AutoSerialisedObject
    {
        public List<AutoSerialisedField> autoSerialiedFields = new();
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

        struct TestSubStruct
        {
            [AutoSerialize(0)]
            public int fieldInt;
        }

        class TestSubClass
        {
            [AutoSerialize(0)]
            public int fieldInt;
        }

        class TestStruct
        {
            [AutoSerialize(0)]
            public int fieldInt;

            [AutoSerialize(1)]
            public string fieldString;

            [AutoSerialize(2)]
            public TestSubStruct subStruct;

            [AutoSerialize(3)]
            public List<string> listOfStrs;

            [AutoSerialize(4)]
            public List<int> listOfInts;

            [AutoSerialize(5)]
            public List<TestSubStruct> listOfTestSubStruct;

            [AutoSerialize(6)]
            public float fieldFloat;

            [AutoSerialize(7)]
            public bool fieldBool;

            [AutoSerialize(8)]
            public short fieldShort;

            [AutoSerialize(9)]
            public byte[] fieldByteArray;

            [AutoSerialize(10)]
            public byte fieldByte;

            [AutoSerialize(11)]
            public TestSubClass testSubClass;

            [AutoSerialize(12)]
            public TestSubClass testSubClassNull;

            [AutoSerialize(13)]
            public string testRuStr;

            [AutoSerialize(14)]
            public int num;
        }

        public const int currentFormatVersion = 3;

        static Dictionary<string, ClassReflectionCache> classReflectionCaches = new();

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

            var fieldsToSerialize = new List<FieldInfo>(type.GetRuntimeFields());
            fieldsToSerialize.RemoveAll(item => item.GetCustomAttribute<AutoSerializeAttribute>() == null);
            var fields = fieldsToSerialize.ConvertAll(item => new ClassReflectionCache.FieldCache
            {
                id = item.GetCustomAttribute<AutoSerializeAttribute>().Id,
                fieldInfo = item,
                alwaysSerialize = item.GetCustomAttribute<AlwaysSerializeAttribute>() != null
            });
            cache = new ClassReflectionCache { fields = fields };
            classReflectionCaches.Add(type.FullName, cache);
            return cache;
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
                ;
                pos += strLength;
                BinaryUtil.WriteInt32(ref dest, lengthPos, strLength);
            }
            else if (type == typeof(byte[]))
            {
                var bytes = (byte[])val;
                pos += BinaryUtil.WriteInt32(ref dest, pos, bytes.Length);
                pos += BinaryUtil.WriteBytes(ref dest, pos, bytes);
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

        public static List<AutoSerialisedFieldBin> SerializeObjectFields(object obj, object baseObj)
        {
            var serializedFields = new List<AutoSerialisedFieldBin>();

            var type = obj.GetType();
            var cache = GetClassReflectionCache(type);
            foreach (var field in cache.fields)
            {
                var value = field.fieldInfo.GetValue(obj);
                if (value != null)
                {
                    var baseValue = baseObj != null ? field.fieldInfo.GetValue(baseObj) : null;
                    if (field.alwaysSerialize || !value.Equals(baseValue))
                        serializedFields.Add(new AutoSerialisedFieldBin { id = field.id, data = Serialize(value, field.fieldInfo.FieldType) });
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

        public static void DeserializeObjectFields(List<AutoSerialisedFieldBin> serializedFields, ObjectContext context)
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

#if UNITY_EDITOR
        [MenuItem("Tests/Auto Serialize")]
        public static void TestAutoSerialize()
        {
            var testObj = new TestStruct
            {
                fieldInt = 100,
                fieldString = "200",
                subStruct = new TestSubStruct
                {
                    fieldInt = 300
                },
                listOfStrs = new List<string> { "A", "B", null, "D", null, "F" },
                listOfInts = new List<int> { 1, 2, 3, 4, 5 },
                listOfTestSubStruct = new List<TestSubStruct> { new() { fieldInt = 1 }, new() { fieldInt = 2 }, new() { fieldInt = 3 } },
                fieldFloat = 10.010f,
                fieldBool = true,
                fieldShort = 1000,
                fieldByte = 10,
                fieldByteArray = new byte[] { 1, 2, 3, 4 },
                testSubClass = new TestSubClass { fieldInt = 400 },
                testSubClassNull = null,
                testRuStr = "Тест",
                num = 10
            };

            var bytes = new byte[1024];
            int serializePos = 0;
            WriteObject(ref bytes, ref serializePos, testObj);
            Array.Resize(ref bytes, serializePos + 1);

            Debug.Log(BitConverter.ToString(bytes).Replace("-", ""));

            var newTestObj = new TestStruct();

            int deserializePos = 0;
            var objContext = new ObjectContext(null, null, currentFormatVersion, null, newTestObj, "Test", typeof(TestStruct));
            ReadObject(ref bytes, ref deserializePos, objContext);

            Debug.Assert(newTestObj.fieldInt == testObj.fieldInt);
            Debug.Assert(newTestObj.fieldString == testObj.fieldString);
            Debug.Assert(newTestObj.subStruct.fieldInt == testObj.subStruct.fieldInt);
            Debug.Assert(newTestObj.listOfStrs.Count == testObj.listOfStrs.Count);
            Debug.Assert(newTestObj.listOfInts.Count == testObj.listOfInts.Count);
            Debug.Assert(newTestObj.listOfTestSubStruct.Count == testObj.listOfTestSubStruct.Count);
            Debug.Assert(newTestObj.testRuStr == testObj.testRuStr);
            Debug.Assert(newTestObj.num == testObj.num);
        }
#endif
    }
}