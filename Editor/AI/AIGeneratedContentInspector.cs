using UnityEditor;
using SiegeUp.Core.AI;
using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core.Editor
{
    [CustomEditor(typeof(BaseAIGeneratedContent), true, isFallback = true)]
    public class GeneratedContentInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var content = (BaseAIGeneratedContent)target;

            if (GUILayout.Button("Generate"))
            {
                RunPromptAsync(content);
            }
        }

        private async void RunPromptAsync(BaseAIGeneratedContent content)
        {
            try
            {
                var prompt = content.GetPrompt();
                var messages = new List<ChatMessage>
                {
                    new ChatMessage(MessageRole.User, "You are a JSON generator. Respond *only* with valid JSON, do not wrap in markdown fences."),
                    new ChatMessage(MessageRole.User, prompt)
                };

                var response = await AIService.Client.GenerateContentAsync(messages);

                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    Debug.LogError($"AI Error: {response.ErrorMessage}");
                    return;
                }

                if (response.Choices.Count > 0)
                {
                    Debug.Log("AI returned: " + response.Choices[0].Message.Content);
                    var generatedJsonString = response.Choices[0].Message.Content ?? "";
                    content.Deserialize(generatedJsonString);
                }
                else
                {
                    Debug.LogWarning("AI returned no choices.");
                }

                EditorUtility.SetDirty(content);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error during AI generation: {ex}");
            }
        }
    }
}