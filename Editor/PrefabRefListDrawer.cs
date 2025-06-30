using SiegeUp.Core;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.Core.Editor
{
    [CustomPropertyDrawer(typeof(PrefabRefListAttribute))]
    public class PrefabRefListDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Properly configure height for expanded contents.
            return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                var prefabRefProp = property.FindPropertyRelative("prefabRef");
                if (prefabRefProp == null)
                    prefabRefProp = property.FindPropertyRelative("gameObject");

                if (prefabRefProp.objectReferenceValue.NullCheck() is PrefabRef prefabRef)
                    label.text = prefabRef.gameObject.name;
                else if (prefabRefProp.objectReferenceValue.NullCheck() is GameObject gameObject)
                    label.text = gameObject.name;
            }
            catch (System.NullReferenceException)
            {
            }
            catch (System.InvalidCastException)
            {
            }

            EditorGUI.PropertyField(position, property, label, property.isExpanded);
        }
    }
}