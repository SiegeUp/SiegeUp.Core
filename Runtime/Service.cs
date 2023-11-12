using System.Diagnostics;
using UnityEngine;

namespace SiegeUp.Core
{
    [DebuggerStepThrough]
    public static class Service<T> where T : Object
    {
        static T instance;
        public static T Instance { get => instance.NullCheck(); private set => instance = value; }

        public static void RegisterInstance(T instance)
        {
            Instance = instance;
        }

        public static void RegisterInstanceForEditor(T instance)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Instance = instance;
#endif
        }
    }
}