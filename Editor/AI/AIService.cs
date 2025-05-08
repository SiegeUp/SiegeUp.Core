using UnityEditor;
using UnityEngine;
using SiegeUp.Core.AI;

namespace SiegeUp.Core.Editor
{
    [InitializeOnLoad]
    public static class AIService
    {
        private const string PrefsApiKey = "AiSettings_ApiKey";
        private const string PrefsModelName = "AiSettings_ModelName";
        private const string DefaultModel = "gemini-1.5-flash-latest";

        private static string cachedApiKey = "";
        private static string cachedModelName = "";
        private static LanguageModelGoogle client;

        static AIService()
        {
            UpdateClientIfNeeded();
            EditorApplication.update += OnEditorUpdate;
        }

        public static LanguageModelGoogle Client
        {
            get
            {
                UpdateClientIfNeeded();
                return client;
            }
        }

        private static void OnEditorUpdate()
        {
            UpdateClientIfNeeded();
        }

        private static void UpdateClientIfNeeded()
        {
            var apiKey = EditorPrefs.GetString(PrefsApiKey, "");
            var modelName = EditorPrefs.GetString(PrefsModelName, DefaultModel);

            if (client != null && apiKey == cachedApiKey && modelName == cachedModelName)
                return;

            cachedApiKey = apiKey;
            cachedModelName = modelName;

            client?.Dispose();
            client = new LanguageModelGoogle(apiKey, modelName);

            Debug.Log($"[AIService] Client recreated: Key='{(string.IsNullOrEmpty(apiKey) ? "—empty—" : "***")}', Model='{modelName}'");
        }
    }
}