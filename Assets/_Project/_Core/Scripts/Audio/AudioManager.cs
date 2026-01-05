using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Databases")]
        [SerializeField] private AudioDB bgmDatabase;
        [SerializeField] private AudioDB sfxDatabase;

        [Header("BGM Settings")]
        [SerializeField] private float defaultFadeDuration = 1f;

        [Header("SFX Pool Settings")]
        [SerializeField] private int sfxPoolSize = 8;

        public event Action<AudioChannel, float> OnVolumeChanged;

        private AudioSource _bgmSourceA;
        private AudioSource _bgmSourceB;
        private AudioSource _activeBgmSource;
        private List<AudioSource> _sfxPool;
        private int _sfxPoolIndex;

        // Atmosphere (continuous ambient audio)
        private AudioSource _atmosphereSource;
        private Coroutine _atmosphereFadeCoroutine;
        private string _currentAtmosphereKey;

        // Looping SFX tracking
        private List<AudioSource> _loopingSfxSources;
        private Dictionary<AudioSource, Coroutine> _loopingSfxFadeCoroutines;

        private Dictionary<AudioChannel, float> _volumes;
        private Coroutine _bgmFadeCoroutine;
        private string _currentBgmKey;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeVolumes();
            InitializeAudioSources();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Initialization

        private void InitializeVolumes()
        {
            _volumes = new Dictionary<AudioChannel, float>
            {
                { AudioChannel.Master, AudioSettingsPersistence.GetVolume(AudioChannel.Master) },
                { AudioChannel.BGM, AudioSettingsPersistence.GetVolume(AudioChannel.BGM) },
                { AudioChannel.SFX, AudioSettingsPersistence.GetVolume(AudioChannel.SFX) }
            };
        }

        private void InitializeAudioSources()
        {
            var bgmContainer = new GameObject("BGM").transform;
            bgmContainer.SetParent(transform);

            _bgmSourceA = CreateAudioSource(bgmContainer, "BGM_A", true);
            _bgmSourceB = CreateAudioSource(bgmContainer, "BGM_B", true);
            _activeBgmSource = _bgmSourceA;

            var sfxContainer = new GameObject("SFX").transform;
            sfxContainer.SetParent(transform);

            _sfxPool = new List<AudioSource>(sfxPoolSize);
            for (int i = 0; i < sfxPoolSize; i++)
            {
                _sfxPool.Add(CreateAudioSource(sfxContainer, $"SFX_{i}", false));
            }

            // Atmosphere source (separate from BGM for independent control)
            var atmosphereContainer = new GameObject("Atmosphere").transform;
            atmosphereContainer.SetParent(transform);
            _atmosphereSource = CreateAudioSource(atmosphereContainer, "Atmosphere_Main", true);

            // Looping SFX tracking
            _loopingSfxSources = new List<AudioSource>();
            _loopingSfxFadeCoroutines = new Dictionary<AudioSource, Coroutine>();
        }

        private AudioSource CreateAudioSource(Transform parent, string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(parent);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;

            return source;
        }

        #endregion

        #region Volume Control

        public float GetVolume(AudioChannel channel)
        {
            return _volumes.TryGetValue(channel, out var volume) ? volume : 1f;
        }

        public void SetVolume(AudioChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            _volumes[channel] = value;

            AudioSettingsPersistence.SetVolume(channel, value);
            AudioSettingsPersistence.Save();

            ApplyVolumeToActiveSources();
            OnVolumeChanged?.Invoke(channel, value);
        }

        private float GetEffectiveVolume(AudioChannel channel)
        {
            return GetVolume(AudioChannel.Master) * GetVolume(channel);
        }

        private void ApplyVolumeToActiveSources()
        {
            if (_activeBgmSource != null && _activeBgmSource.isPlaying)
            {
                var entry = bgmDatabase?.GetEntry(_currentBgmKey);
                float entryVolume = entry?.defaultVolume ?? 1f;
                _activeBgmSource.volume = GetEffectiveVolume(AudioChannel.BGM) * entryVolume;
            }
        }

        #endregion

        #region BGM Playback

        public void PlayBGM(string key)
        {
            PlayBGM(key, defaultFadeDuration);
        }

        public void PlayBGM(string key, float fadeTime)
        {
            if (string.IsNullOrEmpty(key))
            {
                StopBGM(fadeTime);
                return;
            }

            if (bgmDatabase == null)
            {
                Debug.LogWarning("[AudioManager] BGM database not assigned");
                return;
            }

            var entry = bgmDatabase.GetEntry(key);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM not found: {key}");
                return;
            }

            if (_currentBgmKey == key && _activeBgmSource.isPlaying)
            {
                return;
            }

            _currentBgmKey = key;

            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            _bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(entry, fadeTime));
        }

        public void StopBGM()
        {
            StopBGM(defaultFadeDuration);
        }

        public void StopBGM(float fadeTime)
        {
            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            _bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeTime));
        }

        public void PauseBGM()
        {
            if (_activeBgmSource != null && _activeBgmSource.isPlaying)
            {
                _activeBgmSource.Pause();
            }
        }

        public void ResumeBGM()
        {
            if (_activeBgmSource != null && !_activeBgmSource.isPlaying && _activeBgmSource.clip != null)
            {
                _activeBgmSource.UnPause();
            }
        }

        public bool IsBGMPlaying => _activeBgmSource != null && _activeBgmSource.isPlaying;
        public string CurrentBGMKey => _currentBgmKey;

        private IEnumerator CrossfadeBGM(AudioEntry entry, float fadeTime)
        {
            var newSource = _activeBgmSource == _bgmSourceA ? _bgmSourceB : _bgmSourceA;
            var oldSource = _activeBgmSource;

            float targetVolume = GetEffectiveVolume(AudioChannel.BGM) * entry.defaultVolume;

            newSource.clip = entry.clip;
            newSource.loop = entry.loop;
            newSource.volume = 0f;
            newSource.Play();

            float halfFade = fadeTime / 2f;
            float elapsed = 0f;

            while (elapsed < halfFade)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfFade;

                if (oldSource.isPlaying)
                {
                    oldSource.volume = Mathf.Lerp(oldSource.volume, 0f, t);
                }
                newSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            oldSource.Stop();
            oldSource.clip = null;
            newSource.volume = targetVolume;

            _activeBgmSource = newSource;
            _bgmFadeCoroutine = null;
        }

        private IEnumerator FadeOutBGM(float fadeTime)
        {
            if (_activeBgmSource == null || !_activeBgmSource.isPlaying)
            {
                _bgmFadeCoroutine = null;
                yield break;
            }

            float startVolume = _activeBgmSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                _activeBgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }

            _activeBgmSource.Stop();
            _activeBgmSource.clip = null;
            _activeBgmSource.volume = 0f;
            _currentBgmKey = null;
            _bgmFadeCoroutine = null;
        }

        #endregion

        #region SFX Playback

        public void PlaySFX(string key)
        {
            PlaySFX(key, 1f);
        }

        public void PlaySFX(string key, float volumeScale)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (sfxDatabase == null)
            {
                Debug.LogWarning("[AudioManager] SFX database not assigned");
                return;
            }

            var entry = sfxDatabase.GetEntry(key);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX not found: {key}");
                return;
            }

            var source = GetNextSFXSource();
            float volume = GetEffectiveVolume(AudioChannel.SFX) * entry.defaultVolume * volumeScale;
            source.PlayOneShot(entry.clip, volume);
        }

        public void PlaySFXAtPosition(string key, Vector3 position)
        {
            PlaySFXAtPosition(key, position, 1f);
        }

        public void PlaySFXAtPosition(string key, Vector3 position, float volumeScale)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (sfxDatabase == null)
            {
                Debug.LogWarning("[AudioManager] SFX database not assigned");
                return;
            }

            var entry = sfxDatabase.GetEntry(key);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX not found: {key}");
                return;
            }

            float volume = GetEffectiveVolume(AudioChannel.SFX) * entry.defaultVolume * volumeScale;
            AudioSource.PlayClipAtPoint(entry.clip, position, volume);
        }

        private AudioSource GetNextSFXSource()
        {
            var source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
            return source;
        }

        #endregion

        #region Direct Clip Playback

        public void PlayBGMClip(AudioClip clip, float fadeTime = -1f)
        {
            if (clip == null) return;

            if (fadeTime < 0) fadeTime = defaultFadeDuration;

            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            var entry = new AudioEntry { clip = clip, defaultVolume = 1f, loop = true };
            _currentBgmKey = clip.name;
            _bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(entry, fadeTime));
        }

        public void PlaySFXClip(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            var source = GetNextSFXSource();
            float volume = GetEffectiveVolume(AudioChannel.SFX) * volumeScale;
            source.PlayOneShot(clip, volume);
        }

        #endregion

        #region Atmosphere Playback

        /// <summary>
        /// Whether atmosphere audio is currently playing.
        /// </summary>
        public bool IsAtmospherePlaying => _atmosphereSource != null && _atmosphereSource.isPlaying;

        /// <summary>
        /// Current atmosphere key being played.
        /// </summary>
        public string CurrentAtmosphereKey => _currentAtmosphereKey;

        /// <summary>
        /// Plays atmosphere audio by key with optional fade in.
        /// Atmosphere runs independently of BGM.
        /// </summary>
        public void PlayAtmosphere(string key, float fadeTime = 1f)
        {
            if (string.IsNullOrEmpty(key))
            {
                StopAtmosphere(fadeTime);
                return;
            }

            if (bgmDatabase == null)
            {
                Debug.LogWarning("[AudioManager] BGM database not assigned (atmosphere uses BGM database)");
                return;
            }

            var entry = bgmDatabase.GetEntry(key);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] Atmosphere not found: {key}");
                return;
            }

            if (_currentAtmosphereKey == key && _atmosphereSource.isPlaying)
            {
                return;
            }

            _currentAtmosphereKey = key;

            if (_atmosphereFadeCoroutine != null)
            {
                StopCoroutine(_atmosphereFadeCoroutine);
            }

            _atmosphereFadeCoroutine = StartCoroutine(FadeInAtmosphere(entry, fadeTime));
        }

        /// <summary>
        /// Stops atmosphere audio with optional fade out.
        /// </summary>
        public void StopAtmosphere(float fadeTime = 1f)
        {
            if (_atmosphereFadeCoroutine != null)
            {
                StopCoroutine(_atmosphereFadeCoroutine);
            }

            _atmosphereFadeCoroutine = StartCoroutine(FadeOutAtmosphere(fadeTime));
        }

        private IEnumerator FadeInAtmosphere(AudioEntry entry, float fadeTime)
        {
            float targetVolume = GetEffectiveVolume(AudioChannel.BGM) * entry.defaultVolume;

            _atmosphereSource.clip = entry.clip;
            _atmosphereSource.loop = entry.loop;
            _atmosphereSource.volume = 0f;
            _atmosphereSource.Play();

            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                _atmosphereSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeTime);
                yield return null;
            }

            _atmosphereSource.volume = targetVolume;
            _atmosphereFadeCoroutine = null;
        }

        private IEnumerator FadeOutAtmosphere(float fadeTime)
        {
            if (_atmosphereSource == null || !_atmosphereSource.isPlaying)
            {
                _atmosphereFadeCoroutine = null;
                yield break;
            }

            float startVolume = _atmosphereSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                _atmosphereSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }

            _atmosphereSource.Stop();
            _atmosphereSource.clip = null;
            _atmosphereSource.volume = 0f;
            _currentAtmosphereKey = null;
            _atmosphereFadeCoroutine = null;
        }

        #endregion

        #region Looping SFX with Fade

        /// <summary>
        /// Plays a looping SFX with optional fade in. Returns the AudioSource for later control.
        /// </summary>
        public AudioSource PlayLoopingSFX(string key, float fadeInTime = 0f, float volumeScale = 1f)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (sfxDatabase == null)
            {
                Debug.LogWarning("[AudioManager] SFX database not assigned");
                return null;
            }

            var entry = sfxDatabase.GetEntry(key);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] Looping SFX not found: {key}");
                return null;
            }

            // Create a dedicated source for this looping SFX
            var sfxContainer = transform.Find("SFX");
            if (sfxContainer == null)
            {
                sfxContainer = new GameObject("SFX").transform;
                sfxContainer.SetParent(transform);
            }

            var source = CreateAudioSource(sfxContainer, $"LoopingSFX_{key}_{Time.time}", true);
            source.clip = entry.clip;

            float targetVolume = GetEffectiveVolume(AudioChannel.SFX) * entry.defaultVolume * volumeScale;

            _loopingSfxSources.Add(source);

            if (fadeInTime > 0f)
            {
                source.volume = 0f;
                source.Play();
                var fadeCoroutine = StartCoroutine(FadeLoopingSFX(source, 0f, targetVolume, fadeInTime, false));
                _loopingSfxFadeCoroutines[source] = fadeCoroutine;
            }
            else
            {
                source.volume = targetVolume;
                source.Play();
            }

            return source;
        }

        /// <summary>
        /// Stops a looping SFX with optional fade out.
        /// </summary>
        public void StopLoopingSFX(AudioSource source, float fadeOutTime = 0f)
        {
            if (source == null) return;

            // Cancel any existing fade
            if (_loopingSfxFadeCoroutines.TryGetValue(source, out var existingCoroutine))
            {
                if (existingCoroutine != null)
                {
                    StopCoroutine(existingCoroutine);
                }
                _loopingSfxFadeCoroutines.Remove(source);
            }

            if (fadeOutTime > 0f && source.isPlaying)
            {
                var fadeCoroutine = StartCoroutine(FadeLoopingSFX(source, source.volume, 0f, fadeOutTime, true));
                _loopingSfxFadeCoroutines[source] = fadeCoroutine;
            }
            else
            {
                CleanupLoopingSFXSource(source);
            }
        }

        private IEnumerator FadeLoopingSFX(AudioSource source, float startVolume, float endVolume, float duration, bool destroyOnComplete)
        {
            float elapsed = 0f;

            while (elapsed < duration && source != null)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, endVolume, elapsed / duration);
                yield return null;
            }

            if (source != null)
            {
                source.volume = endVolume;

                if (destroyOnComplete)
                {
                    CleanupLoopingSFXSource(source);
                }
                else
                {
                    _loopingSfxFadeCoroutines.Remove(source);
                }
            }
        }

        private void CleanupLoopingSFXSource(AudioSource source)
        {
            if (source == null) return;

            source.Stop();
            _loopingSfxSources.Remove(source);
            _loopingSfxFadeCoroutines.Remove(source);

            if (source.gameObject != null)
            {
                Destroy(source.gameObject);
            }
        }

        #endregion
    }
}
