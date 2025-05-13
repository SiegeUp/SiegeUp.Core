using System;
using UnityEngine;

namespace SiegeUp.Core
{
    public class PrefabRef : MonoBehaviour, IReferenceable
    {
        [SerializeField]
        string prefabId;

        [SerializeField]
        bool ignore;

        public string PrefabId => prefabId;
        public bool Ignore => ignore;

        public string Name => gameObject.name;

        public string Id => prefabId;

        public string Reference => $"Object id - {Id}. Can be used in eventFormats: player_picked_up_[id], player_dropped_[id], player_used_[id]";

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