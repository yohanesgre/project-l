using UnityEngine;

namespace MyGame.Core.Audio
{
    /// <summary>
    /// Static utility class for persisting audio volume settings via PlayerPrefs.
    /// Provides global audio settings that persist across game sessions.
    /// </summary>
    public static class AudioSettingsPersistence
    {
        private const string KeyPrefix = "Audio_";
        private const float DefaultVolume = 1f;

        /// <summary>
        /// Gets the PlayerPrefs key for a specific audio channel.
        /// </summary>
        private static string GetKey(AudioChannel channel) => $"{KeyPrefix}{channel}";

        /// <summary>
        /// Gets the saved volume for a specific audio channel.
        /// </summary>
        /// <param name="channel">The audio channel.</param>
        /// <param name="defaultValue">Default value if no saved setting exists.</param>
        /// <returns>The volume value (0-1).</returns>
        public static float GetVolume(AudioChannel channel, float defaultValue = DefaultVolume)
        {
            return PlayerPrefs.GetFloat(GetKey(channel), defaultValue);
        }

        /// <summary>
        /// Sets the volume for a specific audio channel.
        /// </summary>
        /// <param name="channel">The audio channel.</param>
        /// <param name="value">The volume value (0-1). Will be clamped.</param>
        public static void SetVolume(AudioChannel channel, float value)
        {
            PlayerPrefs.SetFloat(GetKey(channel), Mathf.Clamp01(value));
        }

        /// <summary>
        /// Gets all volume settings at once.
        /// </summary>
        /// <returns>Tuple of (master, bgm, sfx) volumes.</returns>
        public static (float master, float bgm, float sfx) GetAllVolumes()
        {
            return (
                GetVolume(AudioChannel.Master),
                GetVolume(AudioChannel.BGM),
                GetVolume(AudioChannel.SFX)
            );
        }

        /// <summary>
        /// Sets all volume settings at once.
        /// </summary>
        public static void SetAllVolumes(float master, float bgm, float sfx)
        {
            SetVolume(AudioChannel.Master, master);
            SetVolume(AudioChannel.BGM, bgm);
            SetVolume(AudioChannel.SFX, sfx);
        }

        /// <summary>
        /// Immediately saves all PlayerPrefs to disk.
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resets all audio volumes to default values.
        /// </summary>
        public static void ResetToDefaults()
        {
            SetVolume(AudioChannel.Master, DefaultVolume);
            SetVolume(AudioChannel.BGM, DefaultVolume);
            SetVolume(AudioChannel.SFX, DefaultVolume);
            Save();
        }

        /// <summary>
        /// Deletes all saved audio settings.
        /// </summary>
        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(GetKey(AudioChannel.Master));
            PlayerPrefs.DeleteKey(GetKey(AudioChannel.BGM));
            PlayerPrefs.DeleteKey(GetKey(AudioChannel.SFX));
            Save();
        }

        /// <summary>
        /// Checks if any audio settings have been saved.
        /// </summary>
        /// <returns>True if at least one audio setting exists.</returns>
        public static bool HasSavedSettings()
        {
            return PlayerPrefs.HasKey(GetKey(AudioChannel.Master)) ||
                   PlayerPrefs.HasKey(GetKey(AudioChannel.BGM)) ||
                   PlayerPrefs.HasKey(GetKey(AudioChannel.SFX));
        }
    }
}
