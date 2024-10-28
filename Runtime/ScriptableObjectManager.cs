﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.Core
{
    [ExecuteInEditMode, CreateAssetMenu(menuName = "SiegeUp.Core/ScriptableObjectManager")]
    public class ScriptableObjectManager : ScriptableObject
    {
        [SerializeField]
        List<ScriptableObjectWithId> initialList;

        Dictionary<string, ScriptableObjectWithId> scriptableObjectsMap = new();
        public IEnumerable<ScriptableObjectWithId> AllScriptableObjects => scriptableObjectsMap.Values;

        void OnEnable()
        {
            UpdateMap();
        }

        public IReadOnlyDictionary<string, ScriptableObjectWithId> GetMap()
        {
            return scriptableObjectsMap;
        }

        public ScriptableObject GetScriptableObject(string id)
        {
            return !scriptableObjectsMap.TryGetValue(id, out var result) ? null : result;
        }

        [ContextMenu("Update Map")]
        public void UpdateMap()
        {
            scriptableObjectsMap.Clear();
            foreach (var scriptableObject in initialList)
            {
                if (scriptableObject == null)
                {
                    Debug.LogError($"Trying to add null ScriptableObject!");
                    continue;
                }
                if (scriptableObject.Id == null)
                {
                    Debug.LogError($"Trying to add ScriptableObject with null ID! {scriptableObject.name}");
                    continue;
                }
                scriptableObjectsMap.Add(scriptableObject.Id, scriptableObject);
            }
        }

        public IReadOnlyList<T> GetAllScriptableObjects<T>() where T : ScriptableObjectWithId
        {
            return (from pair in scriptableObjectsMap where pair.Value.GetType() == typeof(T) select pair.Value as T).ToList();
        }

#if UNITY_EDITOR
        List<T> FindAllScriptableObjects<T>() where T : ScriptableObject
        {
            var resources = Resources.FindObjectsOfTypeAll<T>();
            var initialPrefabs = resources.Where(resource => AssetDatabase.Contains(resource) && !AssetDatabase.IsSubAsset(resource)).Select(i => i).ToList();
            initialPrefabs.Sort((a, b) => String.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), StringComparison.Ordinal));
            return initialPrefabs;
        }

        public void UpdateList()
        {
            Debug.Log("Update scriptable objects");
            initialList = FindAllScriptableObjects<ScriptableObjectWithId>();
            foreach (var translation in initialList)
            {
                translation.UpdateId(); 
            }

            EditorUtility.SetDirty(this); 
            UpdateMap();
        }
#endif
    }
}