﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

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

        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, bool> condition)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            var keysToRemove = dictionary.Where(condition).Select(kvp => kvp.Key).ToList();

            foreach (var key in keysToRemove)
            {
                dictionary.Remove(key);
            }
        }

        public static T NullCheck<T>(this T unityObject) where T : UnityEngine.Object
        {
            return unityObject ? unityObject : null;
        }

        public static GameObject GetOriginalObject(this PrefabRef prefabRef)
        {
            return Service<PrefabManager>.Instance.GetPrefab(prefabRef);
        }

        public static List<Vector2> GetXZ(this List<Vector3> vectors)
        {
            return vectors.Select(v => v.GetXZ()).ToList();
        }

        public static Vector2 GetXZ(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }

        public static Vector2Int GetXZ(this Vector3Int v3)
        {
            return new Vector2Int(v3.x, v3.z);
        }

        public static Vector2 GetXY(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.y);
        }

        public static List<Vector3> GetX0Y(this List<Vector2> vectors)
        {
            return vectors.Select(v => new Vector3(v.x, 0, v.y)).ToList();
        }

        public static Vector3 GetX0Y(this Vector2 v2)
        {
            return new Vector3(v2.x, 0, v2.y);
        }

        public static Vector3 GetX0Y(this Vector2Int v2)
        {
            return new Vector3(v2.x, 0, v2.y);
        }

        public static Vector2Int Round(this Vector2 v)
        {
            return new Vector2Int((int)Math.Round(v.x), (int)Math.Round(v.y));
        }

        public static Vector3Int Round(this Vector3 v)
        {
            return new Vector3Int((int)Math.Round(v.x), (int)Math.Round(v.y), (int)Math.Round(v.z));
        }

        public static Vector2Int Ceil(this Vector2 v)
        {
            return new Vector2Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
        }

        public static Vector3Int Ceil(this Vector3 v)
        {
            return new Vector3Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y), Mathf.CeilToInt(v.z));
        }

        public static Vector2Int Floor(this Vector2 v)
        {
            return new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        }

        public static Vector3Int Floor(this Vector3 v)
        {
            return new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));
        }

        public static float Length(this NavMeshPath path)
        {
            if (path == null || path.corners == null || path.corners.Length < 2)
                return 0f;

            var corners = path.corners;
            var length = 0f;

            for (int i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
            }
            return length;
        }

        public static string ToShortString(this object obj)
        {
            if (obj == null)
                return string.Empty;

            string original = obj.ToString();
            int index = original.LastIndexOf('(');
            string namePart = index >= 0 ? original.Substring(0, index).Trim() : original;
            string typeName = obj.GetType().Name;
            return $"{namePart} ({typeName})";
        }
    }
}