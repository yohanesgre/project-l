using System;
using UnityEngine;
using MyGame.Core.Audio;

namespace MyGame.Features.Dialogue.Events
{
    public class SoundEffectHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "sfx";

        [Header("Legacy Fallback (when AudioManager not available)")]
        [Tooltip("Path in Resources folder where SFX are stored")]
        [SerializeField] private string resourcePath = "SFX";

        [Tooltip("Default volume for sound effects")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        private AudioSource _legacyAudioSource;

        public void Execute(string value, Action onComplete)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(value);
                onComplete?.Invoke();
                return;
            }

            ExecuteLegacy(value, onComplete);
        }

        private void ExecuteLegacy(string value, Action onComplete)
        {
            EnsureLegacyAudioSource();

            var path = string.IsNullOrEmpty(resourcePath) ? value : $"{resourcePath}/{value}";
            var clip = Resources.Load<AudioClip>(path);

            if (clip == null)
            {
                Debug.LogWarning($"[SoundEffectHandler] SFX not found: {path}");
                onComplete?.Invoke();
                return;
            }

            _legacyAudioSource.PlayOneShot(clip, volume);
            onComplete?.Invoke();
        }

        private void EnsureLegacyAudioSource()
        {
            if (_legacyAudioSource != null) return;

            _legacyAudioSource = GetComponent<AudioSource>();
            if (_legacyAudioSource == null)
            {
                _legacyAudioSource = gameObject.AddComponent<AudioSource>();
                _legacyAudioSource.playOnAwake = false;
            }
        }
    }
}
