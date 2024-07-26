using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.Core
{
    [ExecuteInEditMode, CreateAssetMenu(menuName = "SiegeUp.Core/PrefabManager", fileName = "PrefabManager", order = 1)]
    public class PrefabManager : ScriptableObject
    {
        [SerializeField]
        List<GameObject> initialPrefabs;

        Dictionary<System.Guid, GameObject> prefabMap = new();

        public void AddPrefab(GameObject prefab)
        {
            prefabMap[prefab.GetComponent<PrefabRef>().GetGuid()] = prefab;
        }

        public IEnumerable<GameObject> AllPrefabs => prefabMap.Values.Where(i => i);
        public IEnumerable<PrefabRef> AllPrefabRefs => AllPrefabs.Select(i => i.GetComponent<PrefabRef>());

        public PrefabRef GetPrefabRef(System.Guid prefabId)
        {
            GameObject result;
            if (!prefabMap.TryGetValue(prefabId, out result))
            {
                //Debug.Log("Can't find prefab " + prefabId);
                return null;
            }

            return result.NullCheck()?.GetComponent<PrefabRef>();
        }

        public GameObject GetPrefab(System.Guid prefabId)
        {
            GameObject result;
            if (!prefabMap.TryGetValue(prefabId, out result))
            {
                //Debug.Log("Can't find prefab " + prefabId);
            }

            return result;
        }

        public GameObject GetPrefab(PrefabRef prefabRef)
        {
            return GetPrefab(prefabRef.GetGuid());
        }

        void OnEnable()
        {
            if (initialPrefabs != null)
                foreach (var prefab in initialPrefabs)
                {
                    if (prefab)
                        AddPrefab(prefab);
                }
        }

#if UNITY_EDITOR
        [ContextMenu("Reload")]
        public void UpdatePrefabManager()
        {
            Debug.Log("Update prefab manager");
            initialPrefabs.RemoveAll(item => item == null || item.GetComponent<PrefabRef>().Ignore);
            var prefabRefs = Resources.FindObjectsOfTypeAll<PrefabRef>();
            foreach (var prefabRef in prefabRefs)
            {
                if (AssetDatabase.Contains(prefabRef.gameObject) && !AssetDatabase.IsSubAsset(prefabRef.gameObject) && prefabRef.transform.parent == null && !prefabRef.Ignore)
                {
                    var newId = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefabRef.gameObject));
                    if (prefabRef.PrefabId != newId)
                    {
                        prefabRef.ResetId(newId);
                        EditorUtility.SetDirty(prefabRef.gameObject);
                    }

                    AddPrefab(prefabRef.gameObject);
                    if (initialPrefabs.FindIndex(item => item == prefabRef.gameObject) == -1)
                        initialPrefabs.Add(prefabRef.gameObject);
                }
            }

            EditorUtility.SetDirty(this);
        }

        [ContextMenu("CheckPrefabs")]
        public void CheckPrefabs()
        {
            foreach (var prefab in prefabMap.Values)
            {
                var boundingBoxes = prefab.GetComponentsInChildren<BoundingBox>();  
                foreach (var boundingBox in boundingBoxes)
                {
                    if (boundingBox.Size.x <= 0 || boundingBox.Size.z <= 0)
                    {
                        Debug.Log($"Wrong bounding box size in prefab {prefab.name}!", prefab);
                    }
                    if (boundingBox.gameObject.transform.position != Vector3.zero)
                    {
                        Debug.Log($"Wrong bounding box position in prefab {prefab.name}!", prefab);
                    }
                }
            }
        }

#endif
    }
}