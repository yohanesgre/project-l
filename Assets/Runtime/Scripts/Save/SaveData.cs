using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// Represents the save data for a visual novel save slot.
    /// Contains all information needed to restore dialogue state.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        [Header("Dialogue State")]
        [Tooltip("Name of the current DialogueDatabase asset")]
        public string DatabaseName;

        [Tooltip("Current dialogue TextID")]
        public string CurrentTextID;

        [Tooltip("Current scene ID")]
        public string CurrentSceneID;

        [Header("Story State")]
        [Tooltip("Story flags and variables")]
        public List<StoryFlag> StoryFlags = new List<StoryFlag>();

        [Header("Metadata")]
        [Tooltip("When this save was created")]
        public string SavedAt;

        [Tooltip("Total playtime in seconds at save")]
        public float PlaytimeSeconds;

        [Tooltip("User-provided save name")]
        public string SaveName;

        [Tooltip("Preview text for UI display")]
        public string PreviewText;

        [Tooltip("Base64 encoded screenshot thumbnail")]
        public string ScreenshotBase64;

        /// <summary>
        /// Creates save data from current game state.
        /// </summary>
        public static SaveData CreateFromCurrentState(
            string databaseName,
            string currentTextID,
            string currentSceneID,
            Dictionary<string, object> flags = null,
            string previewText = null)
        {
            var saveData = new SaveData
            {
                DatabaseName = databaseName,
                CurrentTextID = currentTextID,
                CurrentSceneID = currentSceneID,
                SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PreviewText = previewText ?? currentTextID
            };

            // Convert flags dictionary to list
            if (flags != null)
            {
                foreach (var kvp in flags)
                {
                    saveData.StoryFlags.Add(new StoryFlag
                    {
                        Key = kvp.Key,
                        Value = kvp.Value?.ToString() ?? "",
                        Type = kvp.Value?.GetType().Name ?? "String"
                    });
                }
            }

            return saveData;
        }

        /// <summary>
        /// Reconstructs the story flags as a dictionary.
        /// </summary>
        public Dictionary<string, object> GetFlagsAsDictionary()
        {
            var dict = new Dictionary<string, object>();

            foreach (var flag in StoryFlags)
            {
                object value = flag.Type switch
                {
                    "Boolean" => bool.TryParse(flag.Value, out bool b) ? b : false,
                    "Int32" => int.TryParse(flag.Value, out int i) ? i : 0,
                    "Single" => float.TryParse(flag.Value, out float f) ? f : 0f,
                    "Double" => double.TryParse(flag.Value, out double d) ? d : 0.0,
                    _ => flag.Value
                };

                dict[flag.Key] = value;
            }

            return dict;
        }

        /// <summary>
        /// Gets a formatted timestamp string.
        /// </summary>
        public string GetFormattedDate()
        {
            if (DateTime.TryParse(SavedAt, out var date))
            {
                return date.ToString("MMM dd, yyyy HH:mm");
            }
            return SavedAt;
        }
    }

    /// <summary>
    /// Represents a single story flag/variable.
    /// </summary>
    [Serializable]
    public class StoryFlag
    {
        public string Key;
        public string Value;
        public string Type;
    }

    /// <summary>
    /// Metadata about a save slot (for displaying in UI without loading full save).
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public int SlotIndex;
        public bool HasSave;
        public string SavedAt;
        public string PreviewText;
        public string DatabaseName;
        public string SaveName;

        public static SaveSlotInfo Empty(int slotIndex)
        {
            return new SaveSlotInfo
            {
                SlotIndex = slotIndex,
                HasSave = false
            };
        }

        public static SaveSlotInfo FromSaveData(int slotIndex, SaveData data)
        {
            return new SaveSlotInfo
            {
                SlotIndex = slotIndex,
                HasSave = true,
                SavedAt = data.SavedAt,
                PreviewText = data.PreviewText,
                DatabaseName = data.DatabaseName,
                SaveName = data.SaveName
            };
        }
    }
}
