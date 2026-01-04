using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Core.Save
{
    /// <summary>
    /// Generic save state data container.
    /// Can be used by any feature to store its state.
    /// </summary>
    [Serializable]
    public class SaveStateData
    {
        [Header("State Identification")]
        [Tooltip("Name of the source/module (e.g., database name, chapter name)")]
        public string SourceName;

        [Tooltip("Current position identifier (e.g., text ID, checkpoint ID)")]
        public string CurrentPosition;

        [Tooltip("Current scene/chapter identifier")]
        public string SceneId;

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

        [Header("Custom Data")]
        [Tooltip("Additional feature-specific data as key-value pairs")]
        public List<CustomDataEntry> CustomData = new List<CustomDataEntry>();

        /// <summary>
        /// Creates save state data with the given parameters.
        /// </summary>
        public static SaveStateData Create(
            string sourceName,
            string currentPosition,
            string sceneId,
            Dictionary<string, object> flags = null,
            string previewText = null)
        {
            var data = new SaveStateData
            {
                SourceName = sourceName,
                CurrentPosition = currentPosition,
                SceneId = sceneId,
                SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PreviewText = previewText ?? currentPosition
            };

            if (flags != null)
            {
                foreach (var kvp in flags)
                {
                    data.StoryFlags.Add(new StoryFlag
                    {
                        Key = kvp.Key,
                        Value = kvp.Value?.ToString() ?? "",
                        Type = kvp.Value?.GetType().Name ?? "String"
                    });
                }
            }

            return data;
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

        /// <summary>
        /// Sets a custom data value.
        /// </summary>
        public void SetCustomData(string key, string value)
        {
            var existing = CustomData.Find(x => x.Key == key);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                CustomData.Add(new CustomDataEntry { Key = key, Value = value });
            }
        }

        /// <summary>
        /// Gets a custom data value.
        /// </summary>
        public string GetCustomData(string key, string defaultValue = null)
        {
            var entry = CustomData.Find(x => x.Key == key);
            return entry?.Value ?? defaultValue;
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
    /// Represents a custom data entry for extensibility.
    /// </summary>
    [Serializable]
    public class CustomDataEntry
    {
        public string Key;
        public string Value;
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
        public string SourceName;
        public string SaveName;

        public static SaveSlotInfo Empty(int slotIndex)
        {
            return new SaveSlotInfo
            {
                SlotIndex = slotIndex,
                HasSave = false
            };
        }

        public static SaveSlotInfo FromSaveData(int slotIndex, SaveStateData data)
        {
            return new SaveSlotInfo
            {
                SlotIndex = slotIndex,
                HasSave = true,
                SavedAt = data.SavedAt,
                PreviewText = data.PreviewText,
                SourceName = data.SourceName,
                SaveName = data.SaveName
            };
        }
    }
}
