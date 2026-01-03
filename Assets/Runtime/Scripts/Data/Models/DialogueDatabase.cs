using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// ScriptableObject container for dialogue entries imported from Google Sheets.
    /// Each database typically represents one sheet/tab.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Data/Dialogue Database")]
    public class DialogueDatabase : ScriptableObject
    {
        [Header("Import Metadata")]
        [Tooltip("Name of the source Google Sheet tab")]
        public string SourceSheetName;

        [Tooltip("URL of the source Google Sheet (for reimporting)")]
        public string SourceURL;

        [Tooltip("When this data was last imported")]
        public string ImportedAt;

        [Header("Dialogue Data")]
        [Tooltip("All dialogue entries in this database")]
        public List<DialogueEntry> Entries = new List<DialogueEntry>();

        // Runtime lookup dictionary (built on first access)
        private Dictionary<string, DialogueEntry> _lookupByTextID;
        private Dictionary<string, List<DialogueEntry>> _lookupBySceneID;
        private bool _isInitialized;

        /// <summary>
        /// Builds the lookup dictionaries for fast access. Called automatically on first query.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _lookupByTextID = new Dictionary<string, DialogueEntry>();
            _lookupBySceneID = new Dictionary<string, List<DialogueEntry>>();

            foreach (var entry in Entries)
            {
                if (!string.IsNullOrEmpty(entry.TextID))
                {
                    _lookupByTextID[entry.TextID] = entry;
                }

                if (!string.IsNullOrEmpty(entry.SceneID))
                {
                    if (!_lookupBySceneID.ContainsKey(entry.SceneID))
                    {
                        _lookupBySceneID[entry.SceneID] = new List<DialogueEntry>();
                    }
                    _lookupBySceneID[entry.SceneID].Add(entry);
                }
            }

            // Sort scene entries by Order
            foreach (var sceneEntries in _lookupBySceneID.Values)
            {
                sceneEntries.Sort((a, b) => a.Order.CompareTo(b.Order));
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Gets a dialogue entry by its TextID.
        /// </summary>
        /// <param name="textId">The unique text identifier.</param>
        /// <returns>The dialogue entry, or null if not found.</returns>
        public DialogueEntry GetEntry(string textId)
        {
            Initialize();
            return _lookupByTextID.TryGetValue(textId, out var entry) ? entry : null;
        }

        /// <summary>
        /// Gets all dialogue entries for a specific scene, ordered by Order field.
        /// </summary>
        /// <param name="sceneId">The scene identifier.</param>
        /// <returns>List of entries for the scene, or empty list if scene not found.</returns>
        public List<DialogueEntry> GetEntriesByScene(string sceneId)
        {
            Initialize();
            return _lookupBySceneID.TryGetValue(sceneId, out var entries) 
                ? entries 
                : new List<DialogueEntry>();
        }

        /// <summary>
        /// Gets the first entry of a scene (usually the starting point).
        /// </summary>
        /// <param name="sceneId">The scene identifier.</param>
        /// <returns>The first entry, or null if scene not found.</returns>
        public DialogueEntry GetFirstEntryOfScene(string sceneId)
        {
            var entries = GetEntriesByScene(sceneId);
            return entries.Count > 0 ? entries[0] : null;
        }

        /// <summary>
        /// Gets all unique scene IDs in this database.
        /// </summary>
        /// <returns>Array of scene IDs.</returns>
        public string[] GetAllSceneIDs()
        {
            Initialize();
            return _lookupBySceneID.Keys.ToArray();
        }

        /// <summary>
        /// Gets all unique speaker names in this database.
        /// </summary>
        /// <returns>Array of speaker names.</returns>
        public string[] GetAllSpeakers()
        {
            return Entries
                .Where(e => !string.IsNullOrEmpty(e.Speaker))
                .Select(e => e.Speaker)
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Validates the database and returns any issues found.
        /// </summary>
        /// <returns>List of validation error messages.</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var textIds = new HashSet<string>();

            foreach (var entry in Entries)
            {
                // Check for duplicate TextIDs
                if (!string.IsNullOrEmpty(entry.TextID))
                {
                    if (textIds.Contains(entry.TextID))
                    {
                        errors.Add($"Duplicate TextID: {entry.TextID}");
                    }
                    else
                    {
                        textIds.Add(entry.TextID);
                    }
                }
                else
                {
                    errors.Add($"Entry at Order {entry.Order} in Scene {entry.SceneID} has empty TextID");
                }

                // Check for broken NextID references
                if (!string.IsNullOrEmpty(entry.NextID) && !entry.HasChoices && !entry.IsEndOfDialogue)
                {
                    if (!textIds.Contains(entry.NextID) && !Entries.Any(e => e.TextID == entry.NextID))
                    {
                        errors.Add($"Entry {entry.TextID} references non-existent NextID: {entry.NextID}");
                    }
                }

                // Check choice references
                if (entry.HasChoices)
                {
                    foreach (var choiceId in entry.GetChoiceIDs())
                    {
                        if (!Entries.Any(e => e.TextID == choiceId.Trim()))
                        {
                            errors.Add($"Entry {entry.TextID} references non-existent choice: {choiceId}");
                        }
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Forces rebuild of lookup dictionaries. Call after modifying Entries.
        /// </summary>
        public void RebuildLookup()
        {
            _isInitialized = false;
            Initialize();
        }

        private void OnEnable()
        {
            // Reset initialization flag when asset is loaded
            _isInitialized = false;
        }
    }
}
