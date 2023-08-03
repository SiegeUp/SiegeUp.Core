using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SiegeUp.Core
{
    public class RestoreProcess
    {
        public delegate GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation = default, Guid? newUniqueId = null);
        public delegate void Destroy(GameObject gameObject);

        public IReadOnlyDictionary<Guid, SerializedGameObjectBin> serializedGameObjectsBin;
        public Dictionary<Guid, UniqueId> uniqueIdsOnScene;
        public Transform parent;
        public int version;
        public int formatVersion;
        public ObjectContext.FindObjectById findObjectById;
        public List<Type> ignoredComponentTypes;
        public Bounds bounds;
        public Spawn spawn;
        public Destroy destroy;

        public RestoreProcess(IReadOnlyDictionary<Guid, SerializedGameObjectBin> serializedObjectsBin, IReadOnlyDictionary<Guid, UniqueId> unitsIdsOnScene, Transform parent = null, int version = 0)
        {
            if (serializedObjectsBin != null)
                serializedGameObjectsBin = serializedObjectsBin;
            ValidateUniqueIds(serializedGameObjectsBin);
            if (unitsIdsOnScene != null)
                uniqueIdsOnScene = new Dictionary<Guid, UniqueId>(unitsIdsOnScene);
            this.parent = parent;
            this.version = version;
        }
         
        public RestoreProcess(IEnumerable<SerializedGameObjectBin> serializedObjectsBin = null, IEnumerable<UniqueId> unitsIdsOnScene = null, Transform parent = null, int version = 0)
        {
            if (serializedObjectsBin != null)
                serializedGameObjectsBin = MapSerializedObjectsBin(serializedObjectsBin);
            ValidateUniqueIds(serializedGameObjectsBin);
            if (unitsIdsOnScene != null)
                uniqueIdsOnScene = unitsIdsOnScene.ToDictionary(x => ReflectionUtils.StrToGuid(x.StringId), y => y);

            this.parent = parent;
            this.version = version;
        }

        public static Dictionary<Guid, SerializedGameObjectBin> MapSerializedObjectsBin(IEnumerable<SerializedGameObjectBin> serializedObjectsBin)
        {
            var serializedGameObjectsBin = new Dictionary<Guid, SerializedGameObjectBin>();

            foreach (var serializedObject in serializedObjectsBin)
            {
                try
                {
                    serializedGameObjectsBin.Add(serializedObject.id, serializedObject);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Object duplicate. New: {serializedObject.name} Old: {serializedGameObjectsBin[serializedObject.id].name}");
                    if (serializedObject.prefabRef != default)
                    {
                        var prefab = Service<PrefabManager>.Instance.GetPrefab(serializedObject.prefabRef);
                        Debug.LogError($"Object duplicate. Prefab: {prefab.name}");
                    }

                    Debug.LogException(e);
                }
            }

            return serializedGameObjectsBin;
        }

        void ValidateUniqueIds(IReadOnlyDictionary<Guid, SerializedGameObjectBin> serializedObjectsBin)
        {
            if (serializedObjectsBin == null)
                return;
            foreach (var pair in serializedObjectsBin)
            {
                if (pair.Value.id == System.Guid.Empty)
                    pair.Value.id = System.Guid.NewGuid();
            }
        }
    }
}