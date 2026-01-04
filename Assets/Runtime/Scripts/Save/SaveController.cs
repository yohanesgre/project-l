using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// Static utility class for save/load functionality.
    /// Saves are stored in Application.persistentDataPath.
    /// </summary>
    public static class SaveController
    {
        private const string SaveFilePrefix = "vnsave_";
        private const string SaveFileExtension = ".json";

        // Story flags (shared across save system)
        private static Dictionary<string, object> _storyFlags = new Dictionary<string, object>();

        // Events
        public static event Action<int, SaveData> OnSaveCompleted;
        public static event Action<int, SaveData> OnLoadCompleted;
        public static event Action<int> OnSlotDeleted;

        // Settings accessor
        private static SaveSettings Settings => SaveSettings.GetSettings();

        #region Save Operations

        /// <summary>
        /// Saves the current game state to a slot.
        /// </summary>
        /// <param name="slotIndex">Slot index (0 = quicksave if enabled).</param>
        /// <param name="saveName">Optional user-provided name.</param>
        /// <param name="dialogueManager">Reference to active DialogueManager.</param>
        public static bool SaveToSlot(int slotIndex, DialogueManager dialogueManager, string saveName = null)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            if (dialogueManager == null || !dialogueManager.IsDialogueActive)
            {
                Debug.LogWarning("[SaveController] Cannot save: no active dialogue");
                return false;
            }

            var currentEntry = dialogueManager.CurrentEntry;
            var currentDatabase = dialogueManager.CurrentDatabase;

            if (currentEntry == null || currentDatabase == null)
            {
                Debug.LogWarning("[SaveController] Cannot save: invalid state");
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

            return WriteSaveFile(slotIndex, saveData);
        }

        /// <summary>
        /// Quick save to slot 0.
        /// </summary>
        public static bool QuickSave(DialogueManager dialogueManager)
        {
            if (!Settings.EnableQuickSave)
            {
                Debug.LogWarning("[SaveController] QuickSave is disabled");
                return false;
            }
            return SaveToSlot(0, dialogueManager, "Quick Save");
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Loads game state from a slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to load.</param>
        /// <param name="dialogueManager">Reference to DialogueManager to apply state to.</param>
        /// <returns>True if load was successful.</returns>
        public static bool LoadFromSlot(int slotIndex, DialogueManager dialogueManager)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            var saveData = ReadSaveFile(slotIndex);
            if (saveData == null)
            {
                Debug.LogWarning($"[SaveController] No save data in slot {slotIndex}");
                return false;
            }

            return ApplySaveData(slotIndex, saveData, dialogueManager);
        }

        /// <summary>
        /// Quick load from slot 0.
        /// </summary>
        public static bool QuickLoad(DialogueManager dialogueManager)
        {
            if (!Settings.EnableQuickSave)
            {
                Debug.LogWarning("[SaveController] QuickSave/Load is disabled");
                return false;
            }
            return LoadFromSlot(0, dialogueManager);
        }

        /// <summary>
        /// Applies loaded save data to restore game state.
        /// </summary>
        private static bool ApplySaveData(int slotIndex, SaveData saveData, DialogueManager dialogueManager)
        {
            // Find the database asset
            var database = LoadDatabase(saveData.DatabaseName);
            if (database == null)
            {
                Debug.LogError($"[SaveController] Database not found: {saveData.DatabaseName}");
                return false;
            }

            // Restore story flags
            _storyFlags = saveData.GetFlagsAsDictionary();

            // Start dialogue from saved position
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(database, saveData.CurrentTextID);
            }

            OnLoadCompleted?.Invoke(slotIndex, saveData);
            Debug.Log($"[SaveController] Loaded from slot {slotIndex}");
            return true;
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// Gets info about all save slots.
        /// </summary>
        public static SaveSlotInfo[] GetAllSlotInfo()
        {
            var settings = Settings;
            int startSlot = settings.EnableQuickSave ? 0 : 1;
            int totalSlots = settings.EnableQuickSave ? settings.MaxSlots + 1 : settings.MaxSlots;
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
        public static SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            var saveData = ReadSaveFile(slotIndex);
            return saveData != null
                ? SaveSlotInfo.FromSaveData(slotIndex, saveData)
                : SaveSlotInfo.Empty(slotIndex);
        }

        /// <summary>
        /// Deletes a save slot.
        /// </summary>
        public static bool DeleteSlot(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            var path = GetSaveFilePath(slotIndex);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    OnSlotDeleted?.Invoke(slotIndex);
                    Debug.Log($"[SaveController] Deleted slot {slotIndex}");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveController] Failed to delete slot {slotIndex}: {e.Message}");
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a slot has save data.
        /// </summary>
        public static bool HasSaveData(int slotIndex)
        {
            return File.Exists(GetSaveFilePath(slotIndex));
        }

        #endregion

        #region Story Flags

        /// <summary>
        /// Sets a story flag.
        /// </summary>
        public static void SetFlag(string key, object value)
        {
            _storyFlags[key] = value;
        }

        /// <summary>
        /// Gets a story flag value.
        /// </summary>
        public static T GetFlag<T>(string key, T defaultValue = default)
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
        public static bool HasFlag(string key)
        {
            return _storyFlags.ContainsKey(key);
        }

        /// <summary>
        /// Clears all story flags.
        /// </summary>
        public static void ClearFlags()
        {
            _storyFlags.Clear();
        }

        #endregion

        #region File Operations

        private static string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{slotIndex}{SaveFileExtension}");
        }

        private static bool WriteSaveFile(int slotIndex, SaveData saveData)
        {
            try
            {
                var path = GetSaveFilePath(slotIndex);
                var json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(path, json);

                OnSaveCompleted?.Invoke(slotIndex, saveData);
                Debug.Log($"[SaveController] Saved to slot {slotIndex}: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveController] Failed to save: {e.Message}");
                return false;
            }
        }

        private static SaveData ReadSaveFile(int slotIndex)
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
                Debug.LogError($"[SaveController] Failed to read save: {e.Message}");
                return null;
            }
        }

        private static bool ValidateSlotIndex(int slotIndex)
        {
            var settings = Settings;
            int minSlot = settings.EnableQuickSave ? 0 : 1;
            int maxSlot = settings.MaxSlots;

            if (slotIndex < minSlot || slotIndex > maxSlot)
            {
                Debug.LogError($"[SaveController] Invalid slot index: {slotIndex}. Valid range: {minSlot}-{maxSlot}");
                return false;
            }
            return true;
        }

        private static DialogueDatabase LoadDatabase(string databaseName)
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

        #endregion
    }
}
