using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SiegeUp.Core.Editor
{
    [CustomEditor(typeof(RectZone))]
    public class RectZoneEditor : UnityEditor.Editor
    {
        BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

        void OnSceneGUI()
        {
            RectZone t = (target as RectZone);

            boundsHandle.center = t.transform.position;
            boundsHandle.size = t.Bounds.size;

            EditorGUI.BeginChangeCheck();
            boundsHandle.DrawHandle();

            var newBounds = t.Bounds;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed size");
                newBounds.size = boundsHandle.size;
            }

            newBounds.center = Vector3.zero;
            t.Bounds = newBounds;
        }
    }
}