using System;
using System.Collections;
using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// Handles background music events (bgm:music_name, bgm_stop).
    /// Loads audio clips from Resources/BGM/ folder.
    /// </summary>
    public class MusicHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "bgm";

        [Header("References")]
        [Tooltip("AudioSource for playing background music")]
        [SerializeField] private AudioSource audioSource;

        [Header("Settings")]
        [Tooltip("Path in Resources folder where BGM are stored")]
        [SerializeField] private string resourcePath = "BGM";

        [Tooltip("Default volume for background music")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.7f;

        [Tooltip("Fade duration when changing music")]
        [SerializeField] private float fadeDuration = 1f;

        [Tooltip("Should music loop?")]
        [SerializeField] private bool loop = true;

        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            audioSource.playOnAwake = false;
            audioSource.loop = loop;
        }

        public void Execute(string value, Action onComplete)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            // Handle stop command
            if (string.IsNullOrEmpty(value) || value.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                _fadeCoroutine = StartCoroutine(FadeOut(onComplete));
                return;
            }

            var path = string.IsNullOrEmpty(resourcePath) 
                ? value 
                : $"{resourcePath}/{value}";

            var clip = Resources.Load<AudioClip>(path);

            if (clip == null)
            {
                Debug.LogWarning($"[MusicHandler] BGM not found: {path}");
                onComplete?.Invoke();
                return;
            }

            // If same clip, don't restart
            if (audioSource.clip == clip && audioSource.isPlaying)
            {
                onComplete?.Invoke();
                return;
            }

            _fadeCoroutine = StartCoroutine(CrossFade(clip, onComplete));
        }

        private IEnumerator FadeOut(Action onComplete)
        {
            float startVolume = audioSource.volume;
            float elapsed = 0;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = volume;

            onComplete?.Invoke();
        }

        private IEnumerator CrossFade(AudioClip newClip, Action onComplete)
        {
            // Fade out current
            if (audioSource.isPlaying)
            {
                float startVolume = audioSource.volume;
                float elapsed = 0;

                while (elapsed < fadeDuration / 2)
                {
                    elapsed += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2));
                    yield return null;
                }

                audioSource.Stop();
            }

            // Set and play new clip
            audioSource.clip = newClip;
            audioSource.volume = 0f;
            audioSource.loop = loop;
            audioSource.Play();

            // Fade in
            float fadeElapsed = 0;
            while (fadeElapsed < fadeDuration / 2)
            {
                fadeElapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0f, volume, fadeElapsed / (fadeDuration / 2));
                yield return null;
            }

            audioSource.volume = volume;
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Handles bgm_stop event specifically.
    /// </summary>
    public class MusicStopHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "bgm_stop";

        [SerializeField] private MusicHandler musicHandler;

        private void Awake()
        {
            if (musicHandler == null)
            {
                musicHandler = GetComponent<MusicHandler>();
            }
        }

        public void Execute(string value, Action onComplete)
        {
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
