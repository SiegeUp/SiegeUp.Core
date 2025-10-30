using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SiegeUp.Core
{
    public abstract class ScriptableObjectWithId : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("uid")]
        string id;

        public string Id => id;
        public string ShortId => Id.Substring(0, 8);

        public void ResetId(string id) => this.id = id;

        public void UpdateId()
        {
#if UNITY_EDITOR
            var newId = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
            if (!string.IsNullOrEmpty(newId) && !Id.StartsWith(newId))
            {
                ResetId(newId);
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}