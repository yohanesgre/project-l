using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using MyGame.Features.Dialogue;
using MyGame.Features.Dialogue.Models;

namespace MyGame.Features.Dialogue.UI
{
    /// <summary>
    /// Controls the dialogue panel UI using UI Toolkit.
    /// Handles dialogue display, typewriter effect, and control buttons.
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        [Header("UI Documents")]
        [SerializeField] private UIDocument dialogueDocument;
        [SerializeField] private UIDocument choiceDocument;
        [SerializeField] private UIDocument settingsDocument;

        [Header("References")]
        [SerializeField] private TypewriterEffect typewriterEffect;
        [SerializeField] private HistoryUIController _historyUI;

        [Header("Animation")]
        [Tooltip("Duration of show/hide animation in seconds")]
        [SerializeField] private float animationDuration = 0.3f;

        [Tooltip("Slide distance for animation (pixels)")]
        [SerializeField] private float slideDistance = 50f;

        // Events
        public event Action OnAutoToggled;
        public event Action OnSkipRequested;
        public event Action OnLogRequested;
        public event Action OnSettingsRequested;
        public event Action OnDialogueClicked;

        // UI Elements - Dialogue Panel
        private VisualElement _root;
        private VisualElement _dialogueContainer;
        private Label _speakerName;
        private Label _dialogueText;
        private Label _continueIndicator;

        // Control Bar
        private Button _btnAuto;
        private Button _btnSkip;
        private Button _btnLog;
        private Button _btnSettings;

        // UI Elements - History Panel
        // Removed internal history elements

        // State
        private bool _isAutoEnabled;
        private bool _isVisible;
        private bool _isAnimating;
        private DialogueManager _dialogueManager;
        private Coroutine _animationCoroutine;

        private void Awake()
        {
            if (_historyUI == null)
            {
                _historyUI = FindObjectOfType<HistoryUIController>();
                if (_historyUI == null)
                {
                   Debug.LogWarning("[DialogueUIController] HistoryUIController not found in scene. Log button will not work.");
                }
            }

            if (typewriterEffect == null)
            {
                typewriterEffect = GetComponent<TypewriterEffect>();
                if (typewriterEffect == null)
                {
                    typewriterEffect = gameObject.AddComponent<TypewriterEffect>();
                }
            }
        }

        private void Start()
        {
            InitializeUI();
            SubscribeToDialogueManager();
            SubscribeToHistoryUI();
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDialogueManager();
            UnsubscribeFromHistoryUI();
            UnregisterCallbacks();
        }

        private void InitializeUI()
        {
            if (dialogueDocument == null)
            {
                Debug.LogError("[DialogueUIController] UIDocument not assigned");
                return;
            }

            _root = dialogueDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[DialogueUIController] Root visual element is null");
                return;
            }

            // Get dialogue elements
            _dialogueContainer = _root.Q<VisualElement>("dialogue-container");
            _speakerName = _root.Q<Label>("speaker-name");
            _dialogueText = _root.Q<Label>("dialogue-text");
            _continueIndicator = _root.Q<Label>("continue-indicator");

            // Get control buttons
            _btnAuto = _root.Q<Button>("btn-auto");
            _btnSkip = _root.Q<Button>("btn-skip");
            _btnLog = _root.Q<Button>("btn-log");
            _btnSettings = _root.Q<Button>("btn-settings");

            RegisterCallbacks();
            SubscribeToTypewriter();

            // Start hidden
            HideContinueIndicator();
        }

        private void RegisterCallbacks()
        {
            // Root click to advance dialogue (click anywhere on screen)
            _root?.RegisterCallback<ClickEvent>(OnRootClick);

            // Control buttons
            _btnAuto?.RegisterCallback<ClickEvent>(OnAutoClick);
            _btnSkip?.RegisterCallback<ClickEvent>(OnSkipClick);
            _btnLog?.RegisterCallback<ClickEvent>(OnLogClick);
            _btnSettings?.RegisterCallback<ClickEvent>(OnSettingsClick);
        }

        private void UnregisterCallbacks()
        {
            _root?.UnregisterCallback<ClickEvent>(OnRootClick);
            _btnAuto?.UnregisterCallback<ClickEvent>(OnAutoClick);
            _btnSkip?.UnregisterCallback<ClickEvent>(OnSkipClick);
            _btnLog?.UnregisterCallback<ClickEvent>(OnLogClick);
            _btnSettings?.UnregisterCallback<ClickEvent>(OnSettingsClick);
        }

        private void SubscribeToTypewriter()
        {
            if (typewriterEffect != null)
            {
                typewriterEffect.OnTypewriterStarted += OnTypewriterStarted;
                typewriterEffect.OnTypewriterCompleted += OnTypewriterCompleted;
            }
        }

        private void SubscribeToDialogueManager()
        {
            _dialogueManager = DialogueManager.Instance;
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueStarted += OnDialogueStarted;
                _dialogueManager.OnDialogueChanged += OnDialogueChanged;
                _dialogueManager.OnDialogueEnded += OnDialogueEnded;
                _dialogueManager.OnSpeakerChanged += OnSpeakerChanged;
                _dialogueManager.OnSceneTransitionStart += OnSceneTransitionStart;

                // If dialogue is already active (e.g., GameManager started before us), show UI now
                if (_dialogueManager.IsDialogueActive && _dialogueManager.CurrentEntry != null)
                {
                    Show();
                    DisplayDialogue(_dialogueManager.CurrentEntry.Speaker, _dialogueManager.CurrentEntry.DialogueText);
                }
            }
            else
            {
                Debug.LogWarning("[DialogueUIController] DialogueManager.Instance is null");
            }
        }

        private void UnsubscribeFromDialogueManager()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueStarted -= OnDialogueStarted;
                _dialogueManager.OnDialogueChanged -= OnDialogueChanged;
                _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
                _dialogueManager.OnSpeakerChanged -= OnSpeakerChanged;
                _dialogueManager.OnSceneTransitionStart -= OnSceneTransitionStart;
            }

            if (typewriterEffect != null)
            {
                typewriterEffect.OnTypewriterStarted -= OnTypewriterStarted;
                typewriterEffect.OnTypewriterCompleted -= OnTypewriterCompleted;
            }
        }

        private void SubscribeToHistoryUI()
        {
            if (_historyUI != null)
            {
                _historyUI.OnHistoryOpened += OnHistoryOpened;
                _historyUI.OnHistoryClosed += OnHistoryClosed;
            }
        }

        private void UnsubscribeFromHistoryUI()
        {
            if (_historyUI != null)
            {
                _historyUI.OnHistoryOpened -= OnHistoryOpened;
                _historyUI.OnHistoryClosed -= OnHistoryClosed;
            }
        }

        private void OnHistoryOpened()
        {
            Hide(true); // Hide immediately when history opens
        }

        private void OnHistoryClosed()
        {
            if (_dialogueManager != null && _dialogueManager.IsDialogueActive)
            {
                Show();
            }
        }

        #region DialogueManager Event Handlers

        private void OnDialogueStarted(DialogueEntry entry)
        {
            Show();
        }

        private void OnDialogueChanged(DialogueEntry entry)
        {
            Show(); // Ensure UI is visible (e.g., after scene transition)
            DisplayDialogue(entry.Speaker, entry.DialogueText);
        }

        private void OnDialogueEnded()
        {
            Hide();
        }

        private void OnSpeakerChanged(string speaker)
        {
            SetSpeaker(speaker);
        }

        private void OnSceneTransitionStart()
        {
            Hide();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the dialogue panel with animation.
        /// </summary>
        public void Show()
        {
            if (_dialogueContainer == null || _isVisible) return;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(AnimateShow());
        }


        /// <summary>
        /// Hides the dialogue panel with optional animation.
        /// </summary>
        /// <param name="immediate">If true, skips animation.</param>
        public void Hide(bool immediate = false)
        {
            
            if (_dialogueContainer == null) return;

            // Stop any existing animation
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _isVisible = false;

            if (immediate)
            {
                _dialogueContainer.RemoveFromClassList("visible");
                _dialogueContainer.AddToClassList("hidden");
                _dialogueContainer.style.opacity = 0f; // Ensure hidden
            }
            else
            {
                _animationCoroutine = StartCoroutine(AnimateHide());
            }
        }

        private System.Collections.IEnumerator AnimateShow()
        {
            _isAnimating = true;
            _isVisible = true;

            _dialogueContainer.RemoveFromClassList("hidden");
            _dialogueContainer.AddToClassList("visible");

            // Start from below and fade in
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float eased = EaseOutCubic(t);

                float yOffset = Mathf.Lerp(slideDistance, 0f, eased);
                _dialogueContainer.style.translate = new Translate(0, yOffset);
                _dialogueContainer.style.opacity = eased;

                yield return null;
            }

            _dialogueContainer.style.translate = new Translate(0, 0);
            _dialogueContainer.style.opacity = 1f;
            _isAnimating = false;
        }

        private System.Collections.IEnumerator AnimateHide()
        {
            _isAnimating = true;

            // Slide down and fade out
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float eased = EaseInCubic(t);

                float yOffset = Mathf.Lerp(0f, slideDistance, eased);
                _dialogueContainer.style.translate = new Translate(0, yOffset);
                _dialogueContainer.style.opacity = 1f - eased;

                yield return null;
            }

            _dialogueContainer.RemoveFromClassList("visible");
            _dialogueContainer.AddToClassList("hidden");
            _dialogueContainer.style.translate = new Translate(0, 0);
            _dialogueContainer.style.opacity = 1f;

            _isVisible = false;
            _isAnimating = false;
        }

        private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private float EaseInCubic(float t) => t * t * t;

        /// <summary>
        /// Displays dialogue with typewriter effect.
        /// </summary>
        /// <param name="speaker">Speaker name</param>
        /// <param name="text">Dialogue text</param>
        public void DisplayDialogue(string speaker, string text)
        {
            SetSpeaker(speaker);
            HideContinueIndicator();

            if (typewriterEffect != null && _dialogueText != null)
            {
                typewriterEffect.StartTyping(_dialogueText, text);
            }
            else if (_dialogueText != null)
            {
                _dialogueText.text = text;
                ShowContinueIndicator();
            }
        }

        /// <summary>
        /// Sets the speaker name and updates styling.
        /// </summary>
        /// <param name="speaker">Speaker name (empty for narrator)</param>
        public void SetSpeaker(string speaker)
        {
            if (_speakerName == null) return;

            bool isNarrator = string.IsNullOrEmpty(speaker) || 
                              speaker.Equals("Narrator", StringComparison.OrdinalIgnoreCase);

            if (isNarrator)
            {
                _speakerName.text = "";
                _speakerName.AddToClassList("speaker-name--narrator");
                _speakerName.style.display = DisplayStyle.None;
            }
            else
            {
                _speakerName.text = speaker;
                _speakerName.RemoveFromClassList("speaker-name--narrator");
                _speakerName.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Skips the typewriter effect to show full text immediately.
        /// </summary>
        public void SkipTypewriter()
        {
            if (typewriterEffect != null && typewriterEffect.IsTyping)
            {
                typewriterEffect.ShowAllText();
            }
        }

        /// <summary>
        /// Sets the text speed for the typewriter effect.
        /// </summary>
        /// <param name="normalizedSpeed">Speed from 0 (slow) to 1 (fast)</param>
        public void SetTextSpeed(float normalizedSpeed)
        {
            if (typewriterEffect != null)
            {
                typewriterEffect.SetSpeedNormalized(normalizedSpeed);
            }
        }

        /// <summary>
        /// Toggles auto-play mode.
        /// </summary>
        public void ToggleAuto()
        {
            _isAutoEnabled = !_isAutoEnabled;
            UpdateAutoButtonState();
            OnAutoToggled?.Invoke();
        }

        /// <summary>
        /// Gets whether auto-play is enabled.
        /// </summary>
        public bool IsAutoEnabled => _isAutoEnabled;

        #endregion

        #region UI Event Handlers

        private void OnRootClick(ClickEvent evt)
        {
            // Don't advance if clicking on buttons (they handle their own events)
            if (evt.target is Button) return;

            // If typing, skip to end
            if (typewriterEffect != null && typewriterEffect.IsTyping)
            {
                typewriterEffect.ShowAllText();
                return;
            }

            // Otherwise advance dialogue
            OnDialogueClicked?.Invoke();
            _dialogueManager?.AdvanceDialogue();
        }

        private void OnAutoClick(ClickEvent evt)
        {
            evt.StopPropagation();
            ToggleAuto();
        }

        private void OnSkipClick(ClickEvent evt)
        {
            evt.StopPropagation();
            OnSkipRequested?.Invoke();
        }

        private void OnLogClick(ClickEvent evt)
        {
            evt.StopPropagation();
            OnLogRequested?.Invoke();
            
            if (_historyUI != null)
            {
                _historyUI.Toggle();
            }
        }

        private void OnSettingsClick(ClickEvent evt)
        {
            evt.StopPropagation();
            OnSettingsRequested?.Invoke();
        }

        #endregion

        #region Typewriter Event Handlers

        private void OnTypewriterStarted()
        {
            HideContinueIndicator();
        }

        private void OnTypewriterCompleted()
        {
            ShowContinueIndicator();

            // Auto-advance if enabled
            if (_isAutoEnabled && _dialogueManager != null)
            {
                // Use coroutine for delay before auto-advance
                StartCoroutine(AutoAdvanceCoroutine());
            }
        }

        private System.Collections.IEnumerator AutoAdvanceCoroutine()
        {
            yield return new WaitForSeconds(1.5f); // Auto-advance delay
            if (_isAutoEnabled && _dialogueManager != null && _dialogueManager.IsWaitingForInput)
            {
                _dialogueManager.AdvanceDialogue();
            }
        }

        #endregion

        #region Private Helpers

        private void ShowContinueIndicator()
        {
            if (_continueIndicator != null)
            {
                _continueIndicator.RemoveFromClassList("continue-indicator--hidden");
                _continueIndicator.AddToClassList("continue-indicator--visible");
            }
        }

        private void HideContinueIndicator()
        {
            if (_continueIndicator != null)
            {
                _continueIndicator.RemoveFromClassList("continue-indicator--visible");
                _continueIndicator.AddToClassList("continue-indicator--hidden");
            }
        }

        private void UpdateAutoButtonState()
        {
            if (_btnAuto == null) return;

            if (_isAutoEnabled)
            {
                _btnAuto.AddToClassList("control-button--active");
            }
            else
            {
                _btnAuto.RemoveFromClassList("control-button--active");
            }
        }

        private void HandleInput()
        {
            if (!_isVisible || _dialogueManager == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Space or Enter to advance dialogue
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                // If typing, skip to end
                if (typewriterEffect != null && typewriterEffect.IsTyping)
                {
                    typewriterEffect.ShowAllText();
                }
                else
                {
                    // Advance to next dialogue
                    _dialogueManager.AdvanceDialogue();
                }
            }
        }

        #endregion
    }
}
