using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using SiegeUp.Core;

namespace SiegeUp.Core.Editor
{
    [CustomEditor(typeof(BoundingBoxComponent))]
    public class BoundingBoxEditor : UnityEditor.Editor
    {
        BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

        void OnSceneGUI()
        {
            BoundingBoxComponent t = (target as BoundingBoxComponent);

            var boxCollider = t.GetComponent<BoxCollider>();
            if (boxCollider && boxCollider.enabled)
            {
                t.Size = boxCollider.bounds.size;
                t.transform.position += boxCollider.center;
                t.transform.localScale = Vector3.one;
                boxCollider.enabled = false;
                EditorUtility.SetDirty(t.gameObject);
            }

            boundsHandle.center = t.transform.position;
            boundsHandle.size = t.Size;

            EditorGUI.BeginChangeCheck();
            boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed size");
                t.Size = boundsHandle.size;
            }

            t.Size = new Vector3(t.Size.x, 0, t.Size.z);
        }
    }
}