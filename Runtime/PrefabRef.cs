﻿using System;
using UnityEngine;

namespace SiegeUp.Core
{
    public class PrefabRef : MonoBehaviour
    {
        [SerializeField]
        string prefabId;

        [SerializeField]
        bool ignore;

        public string PrefabId => prefabId;
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