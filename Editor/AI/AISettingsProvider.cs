using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SiegeUp.Core.AI;
using System.Net.Http;

namespace SiegeUp.Core.Editor
{
    public class AiSettingsProvider : SettingsProvider
    {
        const string PrefsApiKey = "AiSettings_ApiKey";
        const string PrefsModelName = "AiSettings_ModelName";

        string apiKey = "";
        string modelName = "gemini-2.0-flash";

        string testResult = "";
        bool isTesting = false;
        string customPrompt = "Write a short story about a brave knight.";
        Vector2 scrollPosition;

        public AiSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope)
        {
            LoadSettings();
        }

        [SettingsProvider]
        public static SettingsProvider CreateAiSettingsProvider()
        {
            var provider = new AiSettingsProvider("Preferences/External Tools/AI Settings", SettingsScope.User);

            provider.keywords = new HashSet<string>(new[] { "AI", "Language Model", "Gemini", "API Key", "SiegeUp" });

            return provider;
        }

        void LoadSettings()
        {
            apiKey = EditorPrefs.GetString(PrefsApiKey, "");
            modelName = EditorPrefs.GetString(PrefsModelName, "gemini-1.5-flash-latest");
        }

        void SaveSettings()
        {
            EditorPrefs.SetString(PrefsApiKey, apiKey);
            EditorPrefs.SetString(PrefsModelName, modelName);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Backend Configuration", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Backend", "Google Gemini");
            }

            apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
            EditorGUILayout.HelpBox("API Key is stored locally in EditorPrefs (not encrypted).", MessageType.Info);

            modelName = EditorGUILayout.TextField("Model Name", modelName);
            EditorGUILayout.HelpBox("e.g., gemini-1.5-flash-latest, gemini-pro", MessageType.Info);

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Connection Test", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(isTesting || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelName)))
            {
                if (GUILayout.Button("Test Connection (Default Prompt: 'Say hello.')"))
                {
                    RunTestAsync("Say hello.");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Prompt Test", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Enter Custom Prompt:");
            customPrompt = EditorGUILayout.TextArea(customPrompt, GUILayout.Height(60));

            using (new EditorGUI.DisabledScope(isTesting || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(customPrompt)))
            {
                if (GUILayout.Button("Test Custom Prompt"))
                {
                    RunTestAsync(customPrompt);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Result", EditorStyles.boldLabel);

            if (isTesting)
            {
                EditorGUILayout.HelpBox("Testing in progress...", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(testResult))
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
                EditorGUILayout.TextArea(testResult, EditorStyles.textArea, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Click a test button to see results.", MessageType.None);
            }
        }

        async void RunTestAsync(string promptText)
        {
            if (isTesting)
                return;

            isTesting = true;
            testResult = "Running test...";
            Repaint();

            LanguageModel? model = null;

            try
            {
                model = new LanguageModelGoogle(apiKey, modelName);

                var prompt = new List<ChatMessage> { new ChatMessage(MessageRole.User, promptText) };

                LanguageModelResponse response = await model.GenerateContentAsync(prompt);

                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    testResult = $"ERROR: {response.ErrorMessage}";
                }
                else if (response.Choices.Count > 0)
                {
                    var choice = response.Choices[0];
                    string resultPrefix = $"SUCCESS (Finish Reason: {choice.FinishReason})\n---\n";

                    if (choice.FinishReason == FinishReason.ToolCalls && choice.Message.ToolCalls != null)
                    {
                        testResult = resultPrefix + "Model requested tool calls:\n";
                        foreach (var toolCall in choice.Message.ToolCalls)
                        {
                            testResult += $"- ID: {toolCall.Id}, Function: {toolCall.Function.Name}, Args: {toolCall.Function.Arguments}\n";
                        }
                    }
                    else
                    {
                        testResult = resultPrefix + (choice.Message.Content ?? "[No text content received]");
                    }
                }
                else
                {
                    testResult = "WARNING: Test completed but received no response choices.";
                }
            }
            catch (ArgumentException argEx)
            {
                testResult = $"Configuration ERROR: {argEx.Message}";
            }
            catch (HttpRequestException httpEx)
            {
                testResult = $"Network ERROR: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                testResult = $"Unexpected ERROR: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}";
                Debug.LogError($"AI Settings Test Error: {ex}");
            }
            finally
            {
                isTesting = false;
                model?.Dispose();
                Repaint();
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
        }
    }
}