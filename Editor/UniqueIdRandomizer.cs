using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SiegeUp.Core.Editor
{
    static class UniqueIdRandomizer
    {
        const string MenuItemPath = "GameObject/Randomize UniqueIds", MenuPriority = "CONTEXT/Transform";

        [MenuItem(MenuItemPath, false, 49)]
        static void RandomizeSelected()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject go)
                    ApplyRecursive(go);
                else if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                    ApplyToPrefabRoot(obj);
            }
        }

        [MenuItem(MenuItemPath, true)]
        static bool Validate() =>
            Selection.objects != null && Selection.objects.Length > 0;

        static void ApplyToPrefabRoot(UnityEngine.Object obj)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(obj));
            if (!root) return;

            ApplyRecursive(root);

            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
        }

        static void ApplyRecursive(GameObject root)
        {
            foreach (var uid in root.GetComponentsInChildren<UniqueId>(true))
            {
                uid.GenerateId();
                EditorUtility.SetDirty(uid);
                var go = uid.gameObject;
                if (PrefabUtility.IsPartOfPrefabInstance(go) || PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    var prefab = PrefabUtility.GetNearestPrefabInstanceRoot(go) ?? go;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(uid);
                    EditorUtility.SetDirty(prefab);
                }
            }

            if (!Application.isPlaying && !EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
