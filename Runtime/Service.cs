using UnityEngine;

public static class Service<T>
{
    public static T Instance { get; private set; }

    public static void SetInstance(T instance)
    {
        Instance = instance;
    }

    public static void SetInstanceForEditor(T instance)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Instance = instance;
#endif
    }
}