using System;
using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// Handles sound effect events (sfx:sound_name).
    /// Loads audio clips from Resources/SFX/ folder.
    /// </summary>
    public class SoundEffectHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "sfx";

        [Header("References")]
        [Tooltip("AudioSource for playing sound effects")]
        [SerializeField] private AudioSource audioSource;

        [Header("Settings")]
        [Tooltip("Path in Resources folder where SFX are stored")]
        [SerializeField] private string resourcePath = "SFX";

        [Tooltip("Default volume for sound effects")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }

        public void Execute(string value, Action onComplete)
        {
            var path = string.IsNullOrEmpty(resourcePath) 
                ? value 
                : $"{resourcePath}/{value}";

            var clip = Resources.Load<AudioClip>(path);

            if (clip == null)
            {
                Debug.LogWarning($"[SoundEffectHandler] SFX not found: {path}");
                onComplete?.Invoke();
                return;
            }

            audioSource.PlayOneShot(clip, volume);
            
            // Complete immediately - sound plays in background
            onComplete?.Invoke();
        }
    }
}
