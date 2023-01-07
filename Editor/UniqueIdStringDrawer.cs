using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SiegeUp.Core.Editor
{
    [CustomPropertyDrawer(typeof(UniqueIdStringAttribute))]
    public class UniqueIdStringDrawer : PropertyDrawer
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
                label.text = property.name;
            }
            catch (System.NullReferenceException)
            {
            }
            catch (System.InvalidCastException)
            {
            }

            var uniqueIds = StageUtility.GetCurrentStageHandle().FindComponentsOfType<UniqueId>();
            var uniqueId = System.Array.Find(uniqueIds, item => item.StringId == property.stringValue);

            var newObject = EditorGUI.ObjectField(position, property.name, uniqueId, typeof(UniqueId), true);
            if (newObject != null)
                property.stringValue = (newObject as UniqueId).StringId;
            //EditorGUI.PropertyField(position, property, label, property.isExpanded);
        }
    }
}