using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Core.Audio
{
    /// <summary>
    /// ScriptableObject database for audio clips.
    /// Create separate instances for BGM and SFX.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioDB", menuName = "Audio/Audio Database", order = 0)]
    public class AudioDB : ScriptableObject
    {
        [Tooltip("List of audio entries in this database")]
        [SerializeField] private List<AudioEntry> entries = new List<AudioEntry>();

        // Cached lookup dictionary for fast access
        private Dictionary<string, AudioEntry> _lookup;
        private bool _isInitialized;

        /// <summary>
        /// Gets all entries in this database.
        /// </summary>
        public IReadOnlyList<AudioEntry> Entries => entries;

        /// <summary>
        /// Gets the number of entries in this database.
        /// </summary>
        public int Count => entries.Count;

        /// <summary>
        /// Initializes the lookup dictionary. Called automatically on first access.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _lookup = new Dictionary<string, AudioEntry>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key)) continue;

                if (_lookup.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[AudioDB] Duplicate key '{entry.key}' in {name}. Skipping duplicate.");
                    continue;
                }

                _lookup[entry.key] = entry;
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Gets an audio entry by key.
        /// </summary>
        /// <param name="key">The unique identifier for the audio clip.</param>
        /// <returns>The AudioEntry if found, null otherwise.</returns>
        public AudioEntry GetEntry(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            Initialize();

            return _lookup.TryGetValue(key, out var entry) ? entry : null;
        }

        /// <summary>
        /// Gets an audio clip by key.
        /// </summary>
        /// <param name="key">The unique identifier for the audio clip.</param>
        /// <returns>The AudioClip if found, null otherwise.</returns>
        public AudioClip GetClip(string key)
        {
            return GetEntry(key)?.clip;
        }

        /// <summary>
        /// Checks if a key exists in this database.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists.</returns>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            Initialize();

            return _lookup.ContainsKey(key);
        }

        /// <summary>
        /// Gets all keys in this database.
        /// </summary>
        /// <returns>Collection of all keys.</returns>
        public IEnumerable<string> GetAllKeys()
        {
            Initialize();

            return _lookup.Keys;
        }

        /// <summary>
        /// Forces re-initialization of the lookup dictionary.
        /// Call this after modifying entries at runtime.
        /// </summary>
        public void Refresh()
        {
            _isInitialized = false;
            _lookup?.Clear();
            Initialize();
        }

        private void OnValidate()
        {
            // Reset cache when modified in editor
            _isInitialized = false;
            _lookup?.Clear();
        }
    }
}
