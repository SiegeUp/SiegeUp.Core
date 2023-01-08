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

        void OnDestroy()
        {
            if (uniqueId != null)
                allGuids.Remove(uniqueId);
        }

#if UNITY_EDITOR
        void Update()
        {
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

            if (!allGuids.ContainsKey(uniqueId))
            {
                allGuids.Add(uniqueId, this);
            }
        }
#endif
    }
}
