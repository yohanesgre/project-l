using System;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Runtime
{
    /// <summary>
    /// Central game controller managing the visual novel's game loop and state transitions.
    /// Coordinates between DialogueManager, SaveController, and UI systems.
    /// This component lives only in game scenes (not a persistent singleton).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Database References")]
        [Tooltip("Default dialogue database to use for new games")]
        [SerializeField] private DialogueDatabase defaultDatabase;

        [Header("References")]
        [Tooltip("Reference to DialogueManager in scene")]
        [SerializeField] private DialogueManager dialogueManager;

        [Header("Settings")]
        [Tooltip("Pause the game when dialogue ends")]
        [SerializeField] private bool pauseOnDialogueEnd = false;

        [Tooltip("Time scale when paused (0 = completely frozen)")]
        [SerializeField] private float pausedTimeScale = 0f;

        [Tooltip("Auto-start dialogue when scene loads")]
        [SerializeField] private bool autoStartOnAwake = true;

        [Header("Events")]
        [SerializeField] private UnityEvent<GameState> onStateChanged;
        [SerializeField] private UnityEvent onGameStarted;
        [SerializeField] private UnityEvent onGamePaused;
        [SerializeField] private UnityEvent onGameResumed;
        [SerializeField] private UnityEvent onReturnedToTitle;

        // C# Events for code subscriptions
        public event Action<GameState> OnGameStateChanged;
        public event Action OnGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnReturnedToTitle;

        // State
        private GameState _currentState = GameState.Title;
        private GameState _previousState = GameState.Title;
        private float _savedTimeScale = 1f;

        // Public accessors
        public GameState CurrentState => _currentState;
        public GameState PreviousState => _previousState;
        public bool IsPlaying => _currentState == GameState.Playing;
        public bool IsPaused => _currentState == GameState.Paused;
        public bool IsLoading => _currentState == GameState.Loading;
        public bool IsAtTitle => _currentState == GameState.Title;

        // Cached references accessors
        public DialogueManager DialogueManager => dialogueManager;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheReferences();
        }

        private void Start()
        {
            SubscribeToEvents();

            if (autoStartOnAwake && defaultDatabase != null)
            {
                StartNewGame();
            }
            else
            {
                SetState(GameState.Title);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            HandleInput();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts a new game with the default database.
        /// </summary>
        public void StartNewGame()
        {
            if (defaultDatabase == null)
            {
                Debug.LogError("[GameManager] Cannot start new game: no default database assigned");
                return;
            }

            StartNewGame(defaultDatabase);
        }

        /// <summary>
        /// Starts a new game with a specific database.
        /// </summary>
        /// <param name="database">The dialogue database to use.</param>
        public void StartNewGame(DialogueDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("[GameManager] Cannot start new game: database is null");
                return;
            }

            SetState(GameState.Loading);

            // Clear any existing save state for new game
            SaveController.ClearFlags();

            // Start dialogue
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(database);
            }

            SetState(GameState.Playing);
            OnGameStarted?.Invoke();
            onGameStarted?.Invoke();

            Debug.Log($"[GameManager] New game started with database: {database.name}");
        }

        /// <summary>
        /// Continues from a saved game slot.
        /// </summary>
        /// <param name="slotIndex">The save slot to load from.</param>
        /// <returns>True if load was successful.</returns>
        public bool ContinueGame(int slotIndex = 0)
        {
            if (!SaveController.HasSaveData(slotIndex))
            {
                Debug.LogWarning($"[GameManager] No save data in slot {slotIndex}");
                return false;
            }

            SetState(GameState.Loading);

            bool success = SaveController.LoadFromSlot(slotIndex, dialogueManager);

            if (success)
            {
                SetState(GameState.Playing);
                OnGameStarted?.Invoke();
                onGameStarted?.Invoke();
                Debug.Log($"[GameManager] Game continued from slot {slotIndex}");
            }
            else
            {
                SetState(GameState.Title);
                Debug.LogError($"[GameManager] Failed to load from slot {slotIndex}");
            }

            return success;
        }

        /// <summary>
        /// Pauses the game and shows the pause menu.
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing)
            {
                Debug.LogWarning("[GameManager] Can only pause while playing");
                return;
            }

            _savedTimeScale = Time.timeScale;
            Time.timeScale = pausedTimeScale;

            SetState(GameState.Paused);
            OnGamePaused?.Invoke();
            onGamePaused?.Invoke();

            Debug.Log("[GameManager] Game paused");
        }

        /// <summary>
        /// Resumes the game from pause.
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused)
            {
                Debug.LogWarning("[GameManager] Can only resume while paused");
                return;
            }

            Time.timeScale = _savedTimeScale;

            SetState(GameState.Playing);
            OnGameResumed?.Invoke();
            onGameResumed?.Invoke();

            Debug.Log("[GameManager] Game resumed");
        }

        /// <summary>
        /// Toggles between paused and playing states.
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (_currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        /// <summary>
        /// Returns to the title screen, ending the current game session.
        /// </summary>
        /// <param name="confirmSave">If true, prompts player to save before returning.</param>
        public void ReturnToTitle(bool confirmSave = false)
        {
            // TODO: Implement save confirmation dialog if confirmSave is true

            // Restore time scale
            Time.timeScale = 1f;

            // End current dialogue
            dialogueManager?.EndDialogue();

            SetState(GameState.Title);
            OnReturnedToTitle?.Invoke();
            onReturnedToTitle?.Invoke();

            Debug.Log("[GameManager] Returned to title");
        }

        /// <summary>
        /// Quick save to slot 0.
        /// </summary>
        public bool QuickSave()
        {
            if (_currentState != GameState.Playing && _currentState != GameState.Paused)
            {
                Debug.LogWarning("[GameManager] Can only save while playing or paused");
                return false;
            }

            return SaveController.QuickSave(dialogueManager);
        }

        /// <summary>
        /// Quick load from slot 0.
        /// </summary>
        public bool QuickLoad()
        {
            return ContinueGame(0);
        }

        /// <summary>
        /// Checks if a continue option is available (any save data exists).
        /// </summary>
        public bool CanContinue()
        {
            var slots = SaveController.GetAllSlotInfo();
            foreach (var slot in slots)
            {
                if (slot.HasSave) return true;
            }
            return false;
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            // Try to find references if not assigned in Inspector
            if (dialogueManager == null)
            {
                dialogueManager = FindFirstObjectByType<DialogueManager>();
            }
        }

        private void SubscribeToEvents()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueEnded += HandleDialogueEnded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        private void HandleDialogueEnded()
        {
            if (pauseOnDialogueEnd && _currentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Escape key toggles pause
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_currentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (_currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }

            // F5 = Quick Save, F9 = Quick Load (common VN conventions)
            if (_currentState == GameState.Playing || _currentState == GameState.Paused)
            {
                if (keyboard.f5Key.wasPressedThisFrame)
                {
                    QuickSave();
                }
                else if (keyboard.f9Key.wasPressedThisFrame)
                {
                    QuickLoad();
                }
            }
        }

        private void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;

            OnGameStateChanged?.Invoke(_currentState);
            onStateChanged?.Invoke(_currentState);

            Debug.Log($"[GameManager] State changed: {_previousState} -> {_currentState}");
        }

        #endregion
    }
}
