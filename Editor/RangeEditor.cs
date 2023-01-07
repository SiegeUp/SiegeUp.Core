using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SiegeUp.Core.Editor
{
    [CustomEditor(typeof(Range))]
    public class RangeEditor : UnityEditor.Editor
    {
        BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

        void OnSceneGUI()
        {
            Range t = (target as Range);

            EditorGUI.BeginChangeCheck();
            if (t.RangeType == Range.RangeShapeType.Sphere || t.RangeType == Range.RangeShapeType.Circle)
            {
                float range = Handles.RadiusHandle(Quaternion.identity, t.transform.position, t.Radius);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changed range");
                    t.Radius = range;
                }
            }

            if (t.RangeType == Range.RangeShapeType.Box)
            {
                boundsHandle.center = Vector3.zero;
                boundsHandle.size = t.Bounds.size;

                EditorGUI.BeginChangeCheck();
                var matrix = Handles.matrix * t.transform.localToWorldMatrix;

                var newBounds = t.Bounds;
                using (new Handles.DrawingScope(matrix))
                {
                    boundsHandle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Changed size");
                        newBounds.size = boundsHandle.size;
                    }
                }

                newBounds.center = Vector3.zero;
                t.Bounds = newBounds;
            }
        }
    }
}