using System;
using UnityEngine;

namespace SiegeUp.Core
{
    [Serializable]
    public struct SerializedComponentBin : ISerializationCallbackReceiver
    {
        [AutoSerialize(1)]
        public int id;

        [AutoSerialize(2)]
        public byte[] data;

        [AutoSerialize(3)]
        public AutoSerializedObjectBin autoSerialize;

        [SerializeField]
        string _data;

        public void OnBeforeSerialize()
        {
            if (data == null || data.Length == 0)
                _data = "";
            else
                _data = System.Convert.ToBase64String(data);
        }

        public void OnAfterDeserialize()
        {
            if (_data != null)
                data = System.Convert.FromBase64String(_data);
        }

        public T GetField<T>(int fieldId, int formatVersion)
        {
            int index = autoSerialize.fields.FindIndex(item => item.id == fieldId);
            if (index == -1)
                throw new Exception($"No such field {id} in {ReflectionUtils.GetComponentById(id)}");

            var field = autoSerialize.fields[index];
            var objectContext = new ObjectContext(Service<PrefabManager>.Instance, Service<ScriptableObjectManager>.Instance, formatVersion, null, null, null, null);
            var deserialized = AutoSerializeTool.Deserialize(field.data, typeof(T), objectContext);

            return (T)deserialized;
        }

        public void SetField<T>(int fieldId, T value, int formatVersion)
        {
            int index = autoSerialize.fields.FindIndex(item => item.id == fieldId);
            if (index == -1)
                throw new Exception($"No such field {id} in {ReflectionUtils.GetComponentById(id)}");

            autoSerialize.fields[index] = new AutoSerializedFieldBin { id = fieldId, data = AutoSerializeTool.Serialize(value, typeof(T)) };
        }
    }
}