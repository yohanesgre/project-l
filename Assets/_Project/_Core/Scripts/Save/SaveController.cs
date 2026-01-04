using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyGame.Core.Save
{
    /// <summary>
    /// Static utility class for save/load functionality.
    /// Uses ISaveableState interface to work with any saveable system.
    /// Saves are stored in Application.persistentDataPath.
    /// </summary>
    public static class SaveController
    {
        private const string SaveFilePrefix = "save_";
        private const string SaveFileExtension = ".json";

        // Story flags (shared across save system)
        private static Dictionary<string, object> _storyFlags = new Dictionary<string, object>();

        // Events
        public static event Action<int, SaveStateData> OnSaveCompleted;
        public static event Action<int, SaveStateData> OnLoadCompleted;
        public static event Action<int> OnSlotDeleted;

        // Settings accessor
        private static SaveSettings Settings => SaveSettings.GetSettings();

        #region Save Operations

        /// <summary>
        /// Saves the current game state to a slot.
        /// </summary>
        /// <param name="slotIndex">Slot index (0 = quicksave if enabled).</param>
        /// <param name="saveable">The saveable state provider.</param>
        /// <param name="saveName">Optional user-provided name.</param>
        public static bool SaveToSlot(int slotIndex, ISaveableState saveable, string saveName = null)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            if (saveable == null || !saveable.CanSave)
            {
                Debug.LogWarning("[SaveController] Cannot save: saveable is null or cannot save");
                return false;
            }

            var stateData = saveable.GetCurrentState();
            if (stateData == null)
            {
                Debug.LogWarning("[SaveController] Cannot save: failed to get current state");
                return false;
            }

            // Merge story flags into save data
            foreach (var kvp in _storyFlags)
            {
                bool exists = stateData.StoryFlags.Exists(f => f.Key == kvp.Key);
                if (!exists)
                {
                    stateData.StoryFlags.Add(new StoryFlag
                    {
                        Key = kvp.Key,
                        Value = kvp.Value?.ToString() ?? "",
                        Type = kvp.Value?.GetType().Name ?? "String"
                    });
                }
            }

            stateData.SaveName = saveName ?? stateData.SaveName;

            return WriteSaveFile(slotIndex, stateData);
        }

        /// <summary>
        /// Quick save to slot 0.
        /// </summary>
        public static bool QuickSave(ISaveableState saveable)
        {
            if (!Settings.EnableQuickSave)
            {
                Debug.LogWarning("[SaveController] QuickSave is disabled");
                return false;
            }
            return SaveToSlot(0, saveable, "Quick Save");
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Loads game state from a slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to load.</param>
        /// <param name="saveable">The saveable state to restore to.</param>
        /// <returns>True if load was successful.</returns>
        public static bool LoadFromSlot(int slotIndex, ISaveableState saveable)
        {
            if (!ValidateSlotIndex(slotIndex)) return false;

            var stateData = ReadSaveFile(slotIndex);
            if (stateData == null)
            {
                Debug.LogWarning($"[SaveController] No save data in slot {slotIndex}");
                return false;
            }

            // Restore story flags
            _storyFlags = stateData.GetFlagsAsDictionary();

            // Delegate restoration to the saveable implementation
            if (saveable != null)
            {
                bool success = saveable.RestoreState(stateData);
                if (success)
                {
                    OnLoadCompleted?.Invoke(slotIndex, stateData);
                    Debug.Log($"[SaveController] Loaded from slot {slotIndex}");
                    return true;
                }
                else
                {
                    Debug.LogError($"[SaveController] Failed to restore state from slot {slotIndex}");
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Quick load from slot 0.
        /// </summary>
        public static bool QuickLoad(ISaveableState saveable)
        {
            if (!Settings.EnableQuickSave)
            {
                Debug.LogWarning("[SaveController] QuickSave/Load is disabled");
                return false;
            }
            return LoadFromSlot(0, saveable);
        }

        /// <summary>
        /// Gets the save data from a slot without restoring it.
        /// </summary>
        public static SaveStateData GetSaveData(int slotIndex)
        {
            return ReadSaveFile(slotIndex);
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

        private static bool WriteSaveFile(int slotIndex, SaveStateData stateData)
        {
            try
            {
                var path = GetSaveFilePath(slotIndex);
                var json = JsonUtility.ToJson(stateData, true);
                File.WriteAllText(path, json);

                OnSaveCompleted?.Invoke(slotIndex, stateData);
                Debug.Log($"[SaveController] Saved to slot {slotIndex}: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveController] Failed to save: {e.Message}");
                return false;
            }
        }

        private static SaveStateData ReadSaveFile(int slotIndex)
        {
            try
            {
                var path = GetSaveFilePath(slotIndex);
                if (!File.Exists(path)) return null;

                var json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveStateData>(json);
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

        #endregion
    }
}
