using System.Diagnostics;
using UnityEngine;

namespace SiegeUp.Core
{
    [DebuggerStepThrough]
    public static class Service<T> where T : Object
    {
        public static T instance;

        public static void RegisterInstance(T newInstance)
        {
            instance = newInstance;
        }

        public static void RegisterInstanceForEditor(T newInstance)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                instance = newInstance;
#endif
        }
    }
}