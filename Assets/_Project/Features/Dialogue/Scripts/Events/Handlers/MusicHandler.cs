using System;
using UnityEngine;
using MyGame.Core.Audio;

namespace MyGame.Features.Dialogue.Events
{
    public class MusicHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "bgm";

        [Header("Settings")]
        [Tooltip("Fade duration when changing music")]
        [SerializeField] private float fadeDuration = 1f;

        [Header("Legacy Fallback (when AudioManager not available)")]
        [Tooltip("Path in Resources folder where BGM are stored")]
        [SerializeField] private string resourcePath = "BGM";

        [Tooltip("Default volume for background music")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.7f;

        [Tooltip("Should music loop?")]
        [SerializeField] private bool loop = true;

        private AudioSource _legacyAudioSource;
        private Coroutine _fadeCoroutine;

        public void Execute(string value, Action onComplete)
        {
            if (AudioManager.Instance != null)
            {
                ExecuteWithAudioManager(value);
                onComplete?.Invoke();
                return;
            }

            ExecuteLegacy(value, onComplete);
        }

        private void ExecuteWithAudioManager(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                AudioManager.Instance.StopBGM(fadeDuration);
                return;
            }

            AudioManager.Instance.PlayBGM(value, fadeDuration);
        }

        #region Legacy Implementation (fallback when AudioManager not present)

        private void ExecuteLegacy(string value, Action onComplete)
        {
            EnsureLegacyAudioSource();

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            if (string.IsNullOrEmpty(value) || value.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                _fadeCoroutine = StartCoroutine(LegacyFadeOut(onComplete));
                return;
            }

            var path = string.IsNullOrEmpty(resourcePath) ? value : $"{resourcePath}/{value}";
            var clip = Resources.Load<AudioClip>(path);

            if (clip == null)
            {
                Debug.LogWarning($"[MusicHandler] BGM not found: {path}");
                onComplete?.Invoke();
                return;
            }

            if (_legacyAudioSource.clip == clip && _legacyAudioSource.isPlaying)
            {
                onComplete?.Invoke();
                return;
            }

            _fadeCoroutine = StartCoroutine(LegacyCrossFade(clip, onComplete));
        }

        private void EnsureLegacyAudioSource()
        {
            if (_legacyAudioSource != null) return;

            _legacyAudioSource = GetComponent<AudioSource>();
            if (_legacyAudioSource == null)
            {
                _legacyAudioSource = gameObject.AddComponent<AudioSource>();
            }

            _legacyAudioSource.playOnAwake = false;
            _legacyAudioSource.loop = loop;
        }

        private System.Collections.IEnumerator LegacyFadeOut(Action onComplete)
        {
            float startVolume = _legacyAudioSource.volume;
            float elapsed = 0;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _legacyAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            _legacyAudioSource.Stop();
            _legacyAudioSource.clip = null;
            _legacyAudioSource.volume = volume;

            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator LegacyCrossFade(AudioClip newClip, Action onComplete)
        {
            if (_legacyAudioSource.isPlaying)
            {
                float startVolume = _legacyAudioSource.volume;
                float elapsed = 0;

                while (elapsed < fadeDuration / 2)
                {
                    elapsed += Time.deltaTime;
                    _legacyAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2));
                    yield return null;
                }

                _legacyAudioSource.Stop();
            }

            _legacyAudioSource.clip = newClip;
            _legacyAudioSource.volume = 0f;
            _legacyAudioSource.loop = loop;
            _legacyAudioSource.Play();

            float fadeElapsed = 0;
            while (fadeElapsed < fadeDuration / 2)
            {
                fadeElapsed += Time.deltaTime;
                _legacyAudioSource.volume = Mathf.Lerp(0f, volume, fadeElapsed / (fadeDuration / 2));
                yield return null;
            }

            _legacyAudioSource.volume = volume;
            onComplete?.Invoke();
        }

        #endregion
    }

    public class MusicStopHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "bgm_stop";

        [SerializeField] private MusicHandler musicHandler;
        [SerializeField] private float fadeDuration = 1f;

        private void Awake()
        {
            if (musicHandler == null)
            {
                musicHandler = GetComponent<MusicHandler>();
            }
        }

        public void Execute(string value, Action onComplete)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM(fadeDuration);
                onComplete?.Invoke();
                return;
            }

            if (musicHandler != null)
            {
                musicHandler.Execute("stop", onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}
