using System;
using System.Collections.Generic;
using System.IO;


using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// Manages save/load functionality for the visual novel.
    /// Saves are stored in Application.persistentDataPath.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Maximum number of save slots")]
        [SerializeField] private int maxSlots = 10;

        [Tooltip("Include quicksave slot (slot 0)")]
        [SerializeField] private bool enableQuickSave = true;

        [Tooltip("Take screenshot for save thumbnail")]
        [SerializeField] private bool captureScreenshot = false;

        [Tooltip("Screenshot thumbnail size")]
        [SerializeField] private Vector2Int thumbnailSize = new Vector2Int(320, 180);

        // Events
        public event Action<int, SaveData> OnSaveCompleted;
        public event Action<int, SaveData> OnLoadCompleted;
        public event Action<int> OnSlotDeleted;

        // Story flags (shared across save system)
        private Dictionary<string, object> _storyFlags = new Dictionary<string, object>();

        private const string SaveFilePrefix = "vnsave_";
        private const string SaveFileExtension = ".json";

        public int MaxSlots => maxSlots;
        public bool QuickSaveEnabled => enableQuickSave;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Save Operations

        /// <summary>
        /// Saves the current game state to a slot.
        /// </summary>
        /// <param name="slotIndex">Slot index (0 = quicksave if enabled).</param>
        /// <param name="saveName">Optional user-provided name.</param>
        public bool SaveToSlot(int slotIndex, string saveName = null)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            var dialogueManager = DialogueManager.Instance;
            if (dialogueManager == null || !dialogueManager.IsDialogueActive)
            {
                Debug.LogWarning("[SaveManager] Cannot save: no active dialogue");
                return false;
            }

            var currentEntry = dialogueManager.CurrentEntry;
            var currentDatabase = dialogueManager.CurrentDatabase;

            if (currentEntry == null || currentDatabase == null)
            {
                Debug.LogWarning("[SaveManager] Cannot save: invalid state");
                return false;
            }

            var saveData = SaveData.CreateFromCurrentState(
                currentDatabase.name,
                currentEntry.TextID,
                currentEntry.SceneID,
                _storyFlags,
                currentEntry.DialogueText?.Substring(0, Math.Min(50, currentEntry.DialogueText?.Length ?? 0))
            );

            saveData.SaveName = saveName;

            // Capture screenshot if enabled
            if (captureScreenshot)
            {
                saveData.ScreenshotBase64 = CaptureScreenshot();
            }

            return WriteSaveFile(slotIndex, saveData);
        }

        /// <summary>
        /// Quick save to slot 0.
        /// </summary>
        public bool QuickSave()
        {
            if (!enableQuickSave)
            {
                Debug.LogWarning("[SaveManager] QuickSave is disabled");
                return false;
            }
            return SaveToSlot(0, "Quick Save");
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Loads game state from a slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to load.</param>
        /// <returns>True if load was successful.</returns>
        public bool LoadFromSlot(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            var saveData = ReadSaveFile(slotIndex);
            if (saveData == null)
            {
                Debug.LogWarning($"[SaveManager] No save data in slot {slotIndex}");
                return false;
            }

            return ApplySaveData(slotIndex, saveData);
        }

        /// <summary>
        /// Quick load from slot 0.
        /// </summary>
        public bool QuickLoad()
        {
            if (!enableQuickSave)
            {
                Debug.LogWarning("[SaveManager] QuickSave/Load is disabled");
                return false;
            }
            return LoadFromSlot(0);
        }

        /// <summary>
        /// Applies loaded save data to restore game state.
        /// </summary>
        private bool ApplySaveData(int slotIndex, SaveData saveData)
        {
            // Find the database asset
            var database = LoadDatabase(saveData.DatabaseName);
            if (database == null)
            {
                Debug.LogError($"[SaveManager] Database not found: {saveData.DatabaseName}");
                return false;
            }

            // Restore story flags
            _storyFlags = saveData.GetFlagsAsDictionary();

            // Start dialogue from saved position
            var dialogueManager = DialogueManager.Instance;
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(database, saveData.CurrentTextID);
            }

            OnLoadCompleted?.Invoke(slotIndex, saveData);
            Debug.Log($"[SaveManager] Loaded from slot {slotIndex}");
            return true;
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// Gets info about all save slots.
        /// </summary>
        public SaveSlotInfo[] GetAllSlotInfo()
        {
            int startSlot = enableQuickSave ? 0 : 1;
            int totalSlots = enableQuickSave ? maxSlots + 1 : maxSlots;
            var slots = new SaveSlotInfo[totalSlots];

            for (int i = 0; i < totalSlots; i++)
            {
                int slotIndex = startSlot + i;
                var saveData = ReadSaveFile(slotIndex);

                slots[i] = saveData != null
                    ? SaveSlotInfo.FromSaveData(slotIndex, saveData)
                    : SaveSlotInfo.Empty(slotIndex);
            }

            return slots;
        }

        /// <summary>
        /// Gets info about a specific slot.
        /// </summary>
        public SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            var saveData = ReadSaveFile(slotIndex);
            return saveData != null
                ? SaveSlotInfo.FromSaveData(slotIndex, saveData)
                : SaveSlotInfo.Empty(slotIndex);
        }

        /// <summary>
        /// Deletes a save slot.
        /// </summary>
        public bool DeleteSlot(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            var path = GetSaveFilePath(slotIndex);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    OnSlotDeleted?.Invoke(slotIndex);
                    Debug.Log($"[SaveManager] Deleted slot {slotIndex}");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] Failed to delete slot {slotIndex}: {e.Message}");
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a slot has save data.
        /// </summary>
        public bool HasSaveData(int slotIndex)
        {
            return File.Exists(GetSaveFilePath(slotIndex));
        }

        #endregion

        #region Story Flags

        /// <summary>
        /// Sets a story flag.
        /// </summary>
        public void SetFlag(string key, object value)
        {
            _storyFlags[key] = value;
        }

        /// <summary>
        /// Gets a story flag value.
        /// </summary>
        public T GetFlag<T>(string key, T defaultValue = default)
        {
            if (_storyFlags.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks if a flag exists.
        /// </summary>
        public bool HasFlag(string key)
        {
            return _storyFlags.ContainsKey(key);
        }

        /// <summary>
        /// Clears all story flags.
        /// </summary>
        public void ClearFlags()
        {
            _storyFlags.Clear();
        }

        #endregion

        #region File Operations

        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{slotIndex}{SaveFileExtension}");
        }

        private bool WriteSaveFile(int slotIndex, SaveData saveData)
        {
            try
            {
                var path = GetSaveFilePath(slotIndex);
                var json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(path, json);
                
                OnSaveCompleted?.Invoke(slotIndex, saveData);
                Debug.Log($"[SaveManager] Saved to slot {slotIndex}: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
                return false;
            }
        }

        private SaveData ReadSaveFile(int slotIndex)
        {
            try
            {
                var path = GetSaveFilePath(slotIndex);
                if (!File.Exists(path)) return null;

                var json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to read save: {e.Message}");
                return null;
            }
        }

        private bool ValidateSlotIndex(int slotIndex)
        {
            int minSlot = enableQuickSave ? 0 : 1;
            int maxSlot = maxSlots;

            if (slotIndex < minSlot || slotIndex > maxSlot)
            {
                Debug.LogError($"[SaveManager] Invalid slot index: {slotIndex}. Valid range: {minSlot}-{maxSlot}");
                return false;
            }
            return true;
        }

        private DialogueDatabase LoadDatabase(string databaseName)
        {
            // Try loading from Resources
            var database = Resources.Load<DialogueDatabase>($"Dialogues/{databaseName}");
            if (database != null) return database;

            // Try finding in loaded assets
            var databases = Resources.FindObjectsOfTypeAll<DialogueDatabase>();
            foreach (var db in databases)
            {
                if (db.name == databaseName)
                {
                    return db;
                }
            }

            return null;
        }

        private string CaptureScreenshot()
        {
            try
            {
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();

                // Scale down for thumbnail
                var thumbnail = ScaleTexture(texture, thumbnailSize.x, thumbnailSize.y);
                var bytes = thumbnail.EncodeToJPG(75);

                Destroy(texture);
                Destroy(thumbnail);

                return Convert.ToBase64String(bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Screenshot failed: {e.Message}");
                return null;
            }
        }

        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    float u = (float)x / targetWidth;
                    float v = (float)y / targetHeight;
                    result.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }
            
            result.Apply();
            return result;
        }

        #endregion
    }
}
