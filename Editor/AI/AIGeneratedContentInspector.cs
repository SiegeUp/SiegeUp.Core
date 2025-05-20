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
            try
            {
                base.OnInspectorGUI();
            }
            catch { }

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
                Debug.Log(prompt.ToString());
                var messages = new List<ChatMessage>
                {
                    new ChatMessage(MessageRole.User, "You are a JSON generator. Respond *only* with valid JSON."),
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
                    var raw = response.Choices[0].Message.Content ?? "";
                    Debug.Log("AI returned (raw): " + raw);

                    var json = StripJsonFences(raw);
                    Debug.Log("AI returned (stripped): " + json);

                    content.Deserialize(json);
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

        private string StripJsonFences(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var pattern = @"^```(?:json)?\s*([\s\S]*?)\s*```$";
            var match = System.Text.RegularExpressions.Regex.Match(input.Trim(), pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return input.Trim();
        }
    }
}