using UnityEngine;

namespace SiegeUp.Core
{
    public static class Service<T> where T : Object
    {
        public static T Instance { get; private set; }

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