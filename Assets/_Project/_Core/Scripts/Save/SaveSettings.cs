using UnityEngine;

namespace MyGame.Core.Save
{
    /// <summary>
    /// ScriptableObject containing save system configuration.
    /// Create via: Assets > Create > Data > Save Settings
    /// </summary>
    [CreateAssetMenu(fileName = "SaveSettings", menuName = "Data/Save Settings")]
    public class SaveSettings : ScriptableObject
    {
        private const string SettingsPath = "SaveSettings";

        [Header("Save Slots")]
        [Tooltip("Maximum number of save slots")]
        [Range(1, 100)]
        public int MaxSlots = 10;

        [Tooltip("Include quicksave slot (slot 0)")]
        public bool EnableQuickSave = true;

        [Header("Screenshots")]
        [Tooltip("Take screenshot for save thumbnail")]
        public bool CaptureScreenshot = false;

        [Tooltip("Screenshot thumbnail size")]
        public Vector2Int ThumbnailSize = new Vector2Int(320, 180);

        // Cached instance
        private static SaveSettings _instance;

        /// <summary>
        /// Gets the save settings from Resources folder.
        /// Creates default settings if none exist.
        /// </summary>
        public static SaveSettings GetSettings()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SaveSettings>(SettingsPath);

                if (_instance == null)
                {
                    Debug.LogWarning($"[SaveSettings] No settings found at Resources/{SettingsPath}. Using defaults.");
                    _instance = CreateInstance<SaveSettings>();
                }
            }

            return _instance;
        }
    }
}
