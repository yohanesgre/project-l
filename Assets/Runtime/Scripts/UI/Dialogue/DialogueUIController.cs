using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Runtime
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
        [SerializeField] private UIDocument historyDocument;
        [SerializeField] private UIDocument settingsDocument;

        [Header("References")]
        [SerializeField] private TypewriterEffect typewriterEffect;

        [Header("Settings")]
        [SerializeField] private bool showPointerOnLeft = true;

        // Events
        public event Action OnAutoToggled;
        public event Action OnSkipRequested;
        public event Action OnLogRequested;
        public event Action OnSettingsRequested;
        public event Action OnDialogueClicked;

        // UI Elements - Dialogue Panel
        private VisualElement _root;
        private VisualElement _dialogueContainer;
        private VisualElement _dialoguePointer;
        private Label _speakerName;
        private Label _dialogueText;
        private Label _continueIndicator;

        // Control Bar
        private Button _btnAuto;
        private Button _btnSkip;
        private Button _btnLog;
        private Button _btnSettings;

        // State
        private bool _isAutoEnabled;
        private bool _isVisible;
        private DialogueManager _dialogueManager;

        private void Awake()
        {
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
        }

        private void OnDestroy()
        {
            UnsubscribeFromDialogueManager();
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
            _dialoguePointer = _root.Q<VisualElement>("dialogue-pointer");
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
            // Dialogue container click to advance
            _dialogueContainer?.RegisterCallback<ClickEvent>(OnDialogueContainerClick);

            // Control buttons
            _btnAuto?.RegisterCallback<ClickEvent>(OnAutoClick);
            _btnSkip?.RegisterCallback<ClickEvent>(OnSkipClick);
            _btnLog?.RegisterCallback<ClickEvent>(OnLogClick);
            _btnSettings?.RegisterCallback<ClickEvent>(OnSettingsClick);
        }

        private void UnregisterCallbacks()
        {
            _dialogueContainer?.UnregisterCallback<ClickEvent>(OnDialogueContainerClick);
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
            }

            if (typewriterEffect != null)
            {
                typewriterEffect.OnTypewriterStarted -= OnTypewriterStarted;
                typewriterEffect.OnTypewriterCompleted -= OnTypewriterCompleted;
            }
        }

        #region DialogueManager Event Handlers

        private void OnDialogueStarted(DialogueEntry entry)
        {
            Show();
        }

        private void OnDialogueChanged(DialogueEntry entry)
        {
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the dialogue panel.
        /// </summary>
        public void Show()
        {
            if (_dialogueContainer != null)
            {
                _dialogueContainer.RemoveFromClassList("hidden");
                _dialogueContainer.AddToClassList("visible");
                _isVisible = true;
            }
        }

        /// <summary>
        /// Hides the dialogue panel.
        /// </summary>
        public void Hide()
        {
            if (_dialogueContainer != null)
            {
                _dialogueContainer.RemoveFromClassList("visible");
                _dialogueContainer.AddToClassList("hidden");
                _isVisible = false;
            }
        }

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
        /// Sets the pointer position (left or right).
        /// </summary>
        /// <param name="isLeft">True for left position</param>
        public void SetPointerPosition(bool isLeft)
        {
            if (_dialoguePointer == null) return;

            if (isLeft)
            {
                _dialoguePointer.RemoveFromClassList("dialogue-pointer--right");
            }
            else
            {
                _dialoguePointer.AddToClassList("dialogue-pointer--right");
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

        private void OnDialogueContainerClick(ClickEvent evt)
        {
            evt.StopPropagation();

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

        #endregion
    }
}
