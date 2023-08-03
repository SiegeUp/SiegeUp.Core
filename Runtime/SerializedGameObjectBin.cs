using System;
using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core
{
    [Serializable]
    public class SerializedGameObjectBin : ISerializationCallbackReceiver
    {
        [AutoSerialize(1)]
        public Guid id;

        [SerializeField]
        string _id; // for editor

        [QuickEdit, AutoSerialize(2)]
        public PrefabRef prefabRef;

        [AutoSerialize(3)]
        public string name;

        [AutoSerialize(4)]
        public Vector3 position;

        [AutoSerialize(5)]
        public Quaternion rotation;

        [AutoSerialize(6)]
        public List<SerializedComponentBin> serializedComponents;

        public void OnBeforeSerialize()
        {
            _id = id.ToString();
        }

        public void OnAfterDeserialize()
        {
            id = System.Guid.Parse(_id);
        }

        public SerializedGameObjectBin Clone()
        {
            var obj = new SerializedGameObjectBin {
                id = id,
                prefabRef = prefabRef,
                name = name,
                position = position,
                rotation = rotation,
                serializedComponents = new List<SerializedComponentBin>(serializedComponents)
            };
            return obj;
        }

        public bool HasComponent(Type component)
        {
            int id = ReflectionUtils.GetComponentId(component);
            return serializedComponents.Exists(item => item.id == id);
        }

        public int GetSerializedComponentIndex(Type component)
        {
            if (serializedComponents == null)
                return -1;
            int id = ReflectionUtils.GetComponentId(component);
            int index = serializedComponents.FindIndex(item => item.id == id);
            return index;
        }

        public SerializedComponentBin GetSerializedComponent(Type component)
        {
            int index = GetSerializedComponentIndex(component);
            if (index == -1)
                throw new Exception($"No such component {component.Name}");
            return serializedComponents[index];
        }
    }
}