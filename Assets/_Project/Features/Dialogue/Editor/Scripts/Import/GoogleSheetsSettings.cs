using System.IO;

using UnityEditor;
using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// ScriptableObject storing Google Sheets API settings.
    /// This file should be added to .gitignore - each developer sets up their own API key.
    /// </summary>
    public class GoogleSheetsSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/Settings/GoogleSheetsSettings.asset";

        [Tooltip("Your Google Cloud API Key with Sheets API enabled")]
        public string ApiKey = "";

        [Tooltip("Last used spreadsheet URL (for convenience)")]
        public string LastSpreadsheetURL = "";

        [Tooltip("Last used database name")]
        public string LastDatabaseName = "DialogueDatabase";

        /// <summary>
        /// Gets the existing settings asset or creates a new one.
        /// </summary>
        public static GoogleSheetsSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GoogleSheetsSettings>(SettingsPath);

            if (settings == null)
            {
                settings = CreateInstance<GoogleSheetsSettings>();

                // Ensure directory exists
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"[GoogleSheetsSettings] Created new settings asset at {SettingsPath}");
            }

            return settings;
        }

        /// <summary>
        /// Checks if a valid API key is configured.
        /// </summary>
        public bool HasValidApiKey => !string.IsNullOrWhiteSpace(ApiKey) && ApiKey.Length > 10;

        /// <summary>
        /// Saves changes to the settings asset.
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
