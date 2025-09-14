using System;
using UnityEngine;

namespace SiegeUp.Core
{
    public class PrefabRef : MonoBehaviour
    {
        [SerializeField] string prefabId;
        [SerializeField] bool ignore;

        public string PrefabId => prefabId;
        public string ShortPrefabId => prefabId.Substring(0, 8);
        public bool Ignore => ignore;

        Guid cachedGuid;

        public void ResetId(string newPrefabId)
        {
            prefabId = newPrefabId;
        }

        public Guid GetGuid()
        {
            if (cachedGuid == default)
                cachedGuid = ReflectionUtils.StrToGuid(prefabId);
            return cachedGuid;
        }
    }
}