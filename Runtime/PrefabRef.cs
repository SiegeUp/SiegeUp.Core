using System;
using UnityEngine;

namespace SiegeUp.Core
{
    public class PrefabRef : MonoBehaviour
    {
        [SerializeField] string prefabId;
        [SerializeField] bool ignore;

        public string PrefabId => prefabId;
        public string ShortPrefabId => prefabId.Substring(0, 10);
        public bool Ignore => ignore;

        public void ResetId(string newPrefabId)
        {
            prefabId = newPrefabId;
        }

        public Guid GetGuid()
        {
            return ReflectionUtils.StrToGuid(prefabId);
        }
    }
}