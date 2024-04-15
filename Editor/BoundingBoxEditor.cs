using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using SiegeUp.Core;

namespace SiegeUp.Core.Editor
{
    [CustomEditor(typeof(BoundingBox))]
    public class BoundingBoxEditor : UnityEditor.Editor
    {
        BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

        void OnSceneGUI()
        {
            BoundingBox boundingBox = (target as BoundingBox);

            var boxCollider = boundingBox.GetComponent<BoxCollider>();
            if (boxCollider && boxCollider.enabled)
            {
                boundingBox.Size = boxCollider.bounds.size;
                boundingBox.transform.position += boxCollider.center;
                boundingBox.transform.localScale = Vector3.one;
                boxCollider.enabled = false;
                EditorUtility.SetDirty(boundingBox.gameObject);
            }

            boundsHandle.center = Vector2.zero;
            boundsHandle.size = Vector3.Max(boundingBox.Size, Vector3.one);

            EditorGUI.BeginChangeCheck();
            using (new Handles.DrawingScope(boundingBox.transform.localToWorldMatrix))
                boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed size");
                boundingBox.Size = boundsHandle.size;
            }

            boundingBox.Size = new Vector3(boundingBox.Size.x, 0, boundingBox.Size.z);
        }
    }
}