using System;
using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core
{
    public static class Extentions
    {
        public static void InvokeSafe(this Delegate action, params object[] parameters)
        {
            foreach (var invocation in action.GetInvocationList())
            {
                try
                {
                    invocation.DynamicInvoke(parameters);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static int FindIndex<T>(this IReadOnlyList<T> self, Func<T, bool> predicate)
        {
            for (int i = 0; i < self.Count; i++)
            {
                if (predicate(self[i]))
                    return i;
            }

            return -1;
        }
    }
}