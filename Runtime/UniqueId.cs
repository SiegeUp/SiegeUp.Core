// Inspired by the code from https://answers.unity.com/questions/1249093/need-a-persistent-unique-id-for-gameobjects.html by Diarmid Campbell
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SiegeUp.Core
{
    [ExecuteInEditMode]
    public class UniqueId : MonoBehaviour
    {
        // global lookup of IDs to Components - we can esnure at edit time that no two 
        // components which are loaded at the same time have the same ID. 
        static Dictionary<string, UniqueId> allGuids = new();

        [SerializeField]
        string uniqueId;

        public string StringId => uniqueId;


        [ContextMenu("Generate Id")]
        public void GenerateId()
        {
            uniqueId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void ResetId(Guid newId)
        {
            uniqueId = newId.ToString();
        }

        public Guid GetGuid()
        {
            return ReflectionUtils.StrToGuid(uniqueId);
        }

        string GenRuntimeId()
        {
            return Guid.NewGuid().ToString();
        }

        void Awake()
        {
            if (!Application.isPlaying)
                return;
            if (uniqueId == null || uniqueId.Length == 0 || allGuids.ContainsKey(GetGuid().ToString()))
            {
                uniqueId = GenRuntimeId();
            }
            else
            {
                uniqueId = GetGuid().ToString();
            }

            allGuids.Add(uniqueId, this);
        }

        // When we get destroyed (which happens when unloading a level)
        // we must remove ourselves from the global list otherwise the
        // entry still hangs around when we reload the same level again
        // but now the THIS pointer has changed and end up changing 
        // our ID
        void OnDestroy()
        {
            if (uniqueId != null)
                allGuids.Remove(uniqueId);
        }

        // Only compile the code in an editor build
#if UNITY_EDITOR

        // Whenever something changes in the editor (note the [ExecuteInEditMode])
        void Update()
        {
            // Don't do anything when running the game
            if (Application.isPlaying)
                return;

            bool anotherComponentAlreadyHasThisID = (uniqueId != null &&
                                                     allGuids.ContainsKey(uniqueId) &&
                                                     allGuids[uniqueId] != this);

            if (anotherComponentAlreadyHasThisID || uniqueId == null || uniqueId.Length == 0)
            {
                uniqueId = Guid.NewGuid().ToString();
                EditorUtility.SetDirty(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }

            // We can be sure that the key is unique - now make sure we have 
            // it in our list
            if (!allGuids.ContainsKey(uniqueId))
            {
                allGuids.Add(uniqueId, this);
            }
        }

#endif
    }
}
