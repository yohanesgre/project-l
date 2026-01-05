using System.Collections;
using UnityEngine;
using MyGame.Core.Audio;
using MyGame.Features.Character;

namespace MyGame.Features.Dialogue
{
    /// <summary>
    /// Orchestrates all game audio based on game states and driving behavior.
    /// Handles BGM transitions, atmosphere audio, and random wind SFX.
    /// Uses singleton pattern so CharacterPathFollower can notify it of state changes.
    /// </summary>
    public class GameAudioController : MonoBehaviour
    {
        #region Singleton

        public static GameAudioController Instance { get; private set; }

        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #endregion

        #region Serialized Fields

        [Header("References")]
        [Tooltip("Reference to GameManager. Will auto-find if not assigned.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Reference to CharacterPathFollower for driving state events. Will auto-find if not assigned.")]
        [SerializeField] private CharacterPathFollower pathFollower;

        [Header("BGM Keys")]
        [Tooltip("BGM key for main menu state")]
        [SerializeField] private string mainMenuBGM = "main_menu";

        [Tooltip("BGM keys for driving state (randomly alternates between these)")]
        [SerializeField] private string[] drivingBGMs = { "driving_1", "driving_2" };

        [Tooltip("BGM key for when stopping/idle")]
        [SerializeField] private string stoppingBGM = "stopping";

        [Header("Atmosphere Keys")]
        [Tooltip("Key for continuous atmosphere (birds/waves)")]
        [SerializeField] private string atmosphereKey = "bird_waves";

        [Tooltip("Key for random wind SFX")]
        [SerializeField] private string windKey = "wind";

        [Header("Driving BGM Settings")]
        [Tooltip("Minimum duration before switching driving BGM")]
        [SerializeField] private float minPlayDuration = 15f;

        [Tooltip("Maximum duration before switching driving BGM")]
        [SerializeField] private float maxPlayDuration = 40f;

        [Tooltip("Minimum silence duration between driving BGM tracks")]
        [SerializeField] private float minSilenceDuration = 5f;

        [Tooltip("Maximum silence duration between driving BGM tracks")]
        [SerializeField] private float maxSilenceDuration = 12f;

        [Tooltip("Fade time for BGM transitions")]
        [SerializeField] private float bgmFadeTime = 2f;

        [Header("Wind Settings")]
        [Tooltip("Minimum interval between wind sounds")]
        [SerializeField] private float minWindInterval = 20f;

        [Tooltip("Maximum interval between wind sounds")]
        [SerializeField] private float maxWindInterval = 60f;

        [Tooltip("Fade in time for wind")]
        [SerializeField] private float windFadeInTime = 2f;

        [Tooltip("Fade out time for wind")]
        [SerializeField] private float windFadeOutTime = 3f;

        [Tooltip("How long wind plays before fading out")]
        [SerializeField] private float windDuration = 8f;

        [Tooltip("Volume scale for wind (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float windVolume = 0.6f;

        [Header("Engine SFX Settings")]
        [Tooltip("Key for engine loop SFX")]
        [SerializeField] private string engineKey = "engine";

        [Tooltip("Fade in time for engine start")]
        [SerializeField] private float engineFadeInTime = 0.5f;

        [Tooltip("Fade out time for engine stop")]
        [SerializeField] private float engineFadeOutTime = 1f;

        [Tooltip("Volume scale for engine (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float engineVolume = 0.7f;

        #endregion

        #region Private Fields

        private GameState _currentGameState = GameState.Title;
        private bool _isDriving;

        // Driving BGM state
        private Coroutine _drivingBGMCoroutine;
        private int _currentDrivingBGMIndex;
        private bool _inSilencePeriod;

        // Wind state
        private Coroutine _windCoroutine;
        private AudioSource _currentWindSource;

        // Engine state
        private AudioSource _engineSource;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSingleton();
            CacheReferences();
        }

        private void Start()
        {
            SubscribeToEvents();
            StartAtmosphere();
            StartWindLoop();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            UnsubscribeFromEvents();
            StopAllCoroutines();
        }

        #endregion

        #region Initialization

        private void CacheReferences()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            if (pathFollower == null)
            {
                pathFollower = FindFirstObjectByType<CharacterPathFollower>();
            }
        }

        private void SubscribeToEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += HandleGameStateChanged;
                // Initialize with current state
                HandleGameStateChanged(gameManager.CurrentState);
            }

            if (pathFollower != null)
            {
                pathFollower.OnFollowingStarted.AddListener(NotifyDrivingStarted);
                pathFollower.OnFollowingStopped.AddListener(NotifyDrivingStopped);

                // Initialize with current driving state
                if (pathFollower.IsFollowing)
                {
                    NotifyDrivingStarted();
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= HandleGameStateChanged;
            }

            if (pathFollower != null)
            {
                pathFollower.OnFollowingStarted.RemoveListener(NotifyDrivingStarted);
                pathFollower.OnFollowingStopped.RemoveListener(NotifyDrivingStopped);
            }
        }

        #endregion

        #region Game State Handling

        private void HandleGameStateChanged(GameState newState)
        {
            var previousState = _currentGameState;
            _currentGameState = newState;

            Debug.Log($"[GameAudioController] State changed: {previousState} -> {newState}");

            switch (newState)
            {
                case GameState.Title:
                    HandleTitleState();
                    break;

                case GameState.Playing:
                    HandlePlayingState();
                    break;

                case GameState.Paused:
                    // Keep current audio, maybe lower volume?
                    break;

                case GameState.Loading:
                    // Keep atmosphere, maybe fade BGM?
                    break;
            }
        }

        private void HandleTitleState()
        {
            // Stop any driving BGM logic
            StopDrivingBGMLogic();

            // Play main menu BGM
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(mainMenuBGM, bgmFadeTime);
            }
        }

        private void HandlePlayingState()
        {
            // Audio will be controlled by driving state in Update
            // Initial state: not driving, so play stopping BGM
            if (!_isDriving)
            {
                PlayStoppingBGM();
            }
        }

        #endregion

        #region Driving State (Called by CharacterPathFollower)

        /// <summary>
        /// Called by CharacterPathFollower when driving starts.
        /// </summary>
        public void NotifyDrivingStarted()
        {
            if (_currentGameState != GameState.Playing) return;
            if (_isDriving) return; // Already driving

            _isDriving = true;
            OnStartDriving();
        }

        /// <summary>
        /// Called by CharacterPathFollower when driving stops.
        /// </summary>
        public void NotifyDrivingStopped()
        {
            if (!_isDriving) return; // Already stopped

            _isDriving = false;
            OnStopDriving();
        }

        private void OnStartDriving()
        {
            Debug.Log("[GameAudioController] Started driving - switching to driving BGM");
            StartDrivingBGMLogic();
            StartEngineSFX();
        }

        private void OnStopDriving()
        {
            Debug.Log("[GameAudioController] Stopped driving - switching to stopping BGM");
            StopDrivingBGMLogic();
            StopEngineSFX();
            PlayStoppingBGM();
        }

        private void PlayStoppingBGM()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(stoppingBGM, bgmFadeTime);
            }
        }

        #endregion

        #region Driving BGM Logic (Random Switching + Silence)

        private void StartDrivingBGMLogic()
        {
            StopDrivingBGMLogic();

            if (drivingBGMs == null || drivingBGMs.Length == 0)
            {
                Debug.LogWarning("[GameAudioController] No driving BGMs configured");
                return;
            }

            // Start with a random track
            _currentDrivingBGMIndex = Random.Range(0, drivingBGMs.Length);
            _drivingBGMCoroutine = StartCoroutine(DrivingBGMLoop());
        }

        private void StopDrivingBGMLogic()
        {
            if (_drivingBGMCoroutine != null)
            {
                StopCoroutine(_drivingBGMCoroutine);
                _drivingBGMCoroutine = null;
            }
            _inSilencePeriod = false;
        }

        private IEnumerator DrivingBGMLoop()
        {
            while (_isDriving)
            {
                // Play current track
                string currentTrack = drivingBGMs[_currentDrivingBGMIndex];
                _inSilencePeriod = false;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBGM(currentTrack, bgmFadeTime);
                }

                Debug.Log($"[GameAudioController] Playing driving BGM: {currentTrack}");

                // Wait for random play duration
                float playDuration = Random.Range(minPlayDuration, maxPlayDuration);
                yield return new WaitForSeconds(playDuration);

                // Check if still driving
                if (!_isDriving) yield break;

                // Fade out for silence period
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM(bgmFadeTime);
                }

                _inSilencePeriod = true;

                // Random silence duration
                float silenceDuration = Random.Range(minSilenceDuration, maxSilenceDuration);
                Debug.Log($"[GameAudioController] Silence period: {silenceDuration:F1}s");
                yield return new WaitForSeconds(silenceDuration);

                // Check if still driving
                if (!_isDriving) yield break;

                // Switch to next track (alternate or random)
                _currentDrivingBGMIndex = (_currentDrivingBGMIndex + 1) % drivingBGMs.Length;
            }
        }

        #endregion

        #region Atmosphere

        private void StartAtmosphere()
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(atmosphereKey))
            {
                AudioManager.Instance.PlayAtmosphere(atmosphereKey, bgmFadeTime);
                Debug.Log($"[GameAudioController] Started atmosphere: {atmosphereKey}");
            }
        }

        #endregion

        #region Wind Random SFX

        private void StartWindLoop()
        {
            if (string.IsNullOrEmpty(windKey)) return;

            _windCoroutine = StartCoroutine(WindLoop());
        }

        private IEnumerator WindLoop()
        {
            // Initial random delay before first wind
            yield return new WaitForSeconds(Random.Range(minWindInterval * 0.5f, maxWindInterval * 0.5f));

            while (true)
            {
                // Play wind with fade in
                if (AudioManager.Instance != null)
                {
                    _currentWindSource = AudioManager.Instance.PlayLoopingSFX(windKey, windFadeInTime, windVolume);

                    if (_currentWindSource != null)
                    {
                        Debug.Log("[GameAudioController] Wind started");

                        // Wait for wind duration
                        yield return new WaitForSeconds(windDuration);

                        // Fade out wind
                        AudioManager.Instance.StopLoopingSFX(_currentWindSource, windFadeOutTime);
                        _currentWindSource = null;

                        Debug.Log("[GameAudioController] Wind ended");
                    }
                }

                // Wait for next wind
                float waitTime = Random.Range(minWindInterval, maxWindInterval);
                yield return new WaitForSeconds(waitTime);
            }
        }

        #endregion

        #region Engine SFX

        private void StartEngineSFX()
        {
            if (string.IsNullOrEmpty(engineKey)) return;
            if (AudioManager.Instance == null) return;

            // Stop any existing engine sound
            if (_engineSource != null)
            {
                AudioManager.Instance.StopLoopingSFX(_engineSource, 0f);
            }

            _engineSource = AudioManager.Instance.PlayLoopingSFX(engineKey, engineFadeInTime, engineVolume);
            Debug.Log("[GameAudioController] Engine started");
        }

        private void StopEngineSFX()
        {
            if (_engineSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoopingSFX(_engineSource, engineFadeOutTime);
                _engineSource = null;
                Debug.Log("[GameAudioController] Engine stopped");
            }
        }

        #endregion

        #region Editor
#if UNITY_EDITOR
        [ContextMenu("Test: Play Main Menu BGM")]
        private void TestPlayMainMenuBGM()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(mainMenuBGM, bgmFadeTime);
            }
        }

        [ContextMenu("Test: Play Driving BGM")]
        private void TestPlayDrivingBGM()
        {
            if (AudioManager.Instance != null && drivingBGMs.Length > 0)
            {
                AudioManager.Instance.PlayBGM(drivingBGMs[0], bgmFadeTime);
            }
        }

        [ContextMenu("Test: Play Stopping BGM")]
        private void TestPlayStoppingBGM()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(stoppingBGM, bgmFadeTime);
            }
        }

        [ContextMenu("Test: Trigger Wind")]
        private void TestTriggerWind()
        {
            if (AudioManager.Instance != null)
            {
                _currentWindSource = AudioManager.Instance.PlayLoopingSFX(windKey, windFadeInTime, windVolume);
                StartCoroutine(TestWindFadeOut());
            }
        }

        private IEnumerator TestWindFadeOut()
        {
            yield return new WaitForSeconds(windDuration);
            if (_currentWindSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoopingSFX(_currentWindSource, windFadeOutTime);
            }
        }

        [ContextMenu("Test: Start Engine")]
        private void TestStartEngine()
        {
            StartEngineSFX();
        }

        [ContextMenu("Test: Stop Engine")]
        private void TestStopEngine()
        {
            StopEngineSFX();
        }

        [ContextMenu("Test: Button Click")]
        private void TestButtonClick()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("button_click");
            }
        }

        [ContextMenu("Test: Typewriter")]
        private void TestTypewriter()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("typewriter");
            }
        }
#endif
        #endregion
    }
}
