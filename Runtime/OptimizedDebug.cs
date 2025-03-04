using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptimizedDebug : MonoBehaviour
{
    public static Action<string> onOptimizedDebugLog;

    public static void Log(string message)
    {
        onOptimizedDebugLog(message);
    }
}
