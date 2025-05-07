 using UnityEngine;
using UnityEditor;
using SiegeUp.Core.AI;

namespace SiegeUp.Core.Editor
{
    [CustomEditor(typeof(BaseGeneratedContent), true, isFallback = true)]
    public class GeneratedContentInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            BaseGeneratedContent content = (BaseGeneratedContent)target;

            if (GUILayout.Button("Generate"))
            {
                content.Json = RunPrompt(content.GetPrompt());
                EditorUtility.SetDirty(content);
            }
        }

        private string RunPrompt(string prompt)
        {
            // Run AI prompt from here and return json
            return "{}";
        }
    }
}