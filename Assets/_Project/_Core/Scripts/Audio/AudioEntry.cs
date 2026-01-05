using System;
using UnityEngine;

namespace MyGame.Core.Audio
{
    /// <summary>
    /// Represents a single audio clip entry in an AudioDB.
    /// </summary>
    [Serializable]
    public class AudioEntry
    {
        [Tooltip("Unique identifier for this audio clip (e.g., 'main_theme', 'button_click')")]
        public string key;

        [Tooltip("The audio clip reference")]
        public AudioClip clip;

        [Tooltip("Default volume for this clip (0-1)")]
        [Range(0f, 1f)]
        public float defaultVolume = 1f;

        [Tooltip("Optional: Override loop setting for BGM (ignored for SFX)")]
        public bool loop = true;

        /// <summary>
        /// Validates if this entry is properly configured.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(key) && clip != null;
    }
}
