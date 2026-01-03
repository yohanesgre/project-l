using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Runtime
{
    /// <summary>
    /// Controls the choice panel UI using UI Toolkit.
    /// Handles displaying and selecting dialogue choices.
    /// </summary>
    public class ChoiceUIController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument choiceDocument;

        [Header("Settings")]
        [SerializeField] private bool showTitle = false;
        [SerializeField] private string defaultTitle = "Make a choice";

        // Events
        public event Action<int> OnChoiceSelected;

        // UI Elements
        private VisualElement _root;
        private VisualElement _choiceContainer;
        private Label _choiceTitle;
        private VisualElement _choiceList;

        // State
        private List<Button> _choiceButtons = new List<Button>();
        private bool _isVisible;
        private DialogueManager _dialogueManager;

        private void Start()
        {
            InitializeUI();
            SubscribeToDialogueManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDialogueManager();
            ClearChoices();
        }

        private void InitializeUI()
        {
            if (choiceDocument == null)
            {
                Debug.LogError("[ChoiceUIController] UIDocument not assigned");
                return;
            }

            _root = choiceDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[ChoiceUIController] Root visual element is null");
                return;
            }

            _choiceContainer = _root.Q<VisualElement>("choice-container");
            _choiceTitle = _root.Q<Label>("choice-title");
            _choiceList = _root.Q<VisualElement>("choice-list");

            // Start hidden
            Hide();
        }

        private void SubscribeToDialogueManager()
        {
            _dialogueManager = DialogueManager.Instance;
            if (_dialogueManager != null)
            {
                _dialogueManager.OnChoicesPresented += OnChoicesPresented;
                _dialogueManager.OnDialogueEnded += OnDialogueEnded;
            }
        }

        private void UnsubscribeFromDialogueManager()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.OnChoicesPresented -= OnChoicesPresented;
                _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
            }
        }

        #region DialogueManager Event Handlers

        private void OnChoicesPresented(List<ChoiceOption> choices)
        {
            DisplayChoices(choices);
        }

        private void OnDialogueEnded()
        {
            Hide();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the choice panel.
        /// </summary>
        public void Show()
        {
            if (_choiceContainer != null)
            {
                _choiceContainer.RemoveFromClassList("hidden");
                _choiceContainer.AddToClassList("visible");
                _isVisible = true;
            }
        }

        /// <summary>
        /// Hides the choice panel.
        /// </summary>
        public void Hide()
        {
            if (_choiceContainer != null)
            {
                _choiceContainer.RemoveFromClassList("visible");
                _choiceContainer.AddToClassList("hidden");
                _isVisible = false;
            }
            ClearChoices();
        }

        /// <summary>
        /// Displays a list of choices.
        /// </summary>
        /// <param name="choices">List of choice options to display</param>
        public void DisplayChoices(List<ChoiceOption> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning("[ChoiceUIController] No choices to display");
                Hide();
                return;
            }

            ClearChoices();

            // Set title
            if (_choiceTitle != null)
            {
                if (showTitle)
                {
                    _choiceTitle.text = defaultTitle;
                    _choiceTitle.RemoveFromClassList("hidden");
                }
                else
                {
                    _choiceTitle.AddToClassList("hidden");
                }
            }

            // Create choice buttons
            for (int i = 0; i < choices.Count; i++)
            {
                CreateChoiceButton(i, choices[i].DisplayText);
            }

            Show();
        }

        /// <summary>
        /// Displays choices from string array (simpler API).
        /// </summary>
        /// <param name="choiceTexts">Array of choice text strings</param>
        public void DisplayChoices(string[] choiceTexts)
        {
            if (choiceTexts == null || choiceTexts.Length == 0)
            {
                Hide();
                return;
            }

            var choices = new List<ChoiceOption>();
            for (int i = 0; i < choiceTexts.Length; i++)
            {
                choices.Add(new ChoiceOption
                {
                    TextID = i.ToString(),
                    DisplayText = choiceTexts[i]
                });
            }

            DisplayChoices(choices);
        }

        /// <summary>
        /// Sets the title text for the choice panel.
        /// </summary>
        /// <param name="title">Title text to display</param>
        public void SetTitle(string title)
        {
            if (_choiceTitle != null)
            {
                _choiceTitle.text = title;
                showTitle = !string.IsNullOrEmpty(title);
            }
        }

        /// <summary>
        /// Gets whether the choice panel is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Private Methods

        private void CreateChoiceButton(int index, string text)
        {
            if (_choiceList == null) return;

            var button = new Button();
            button.text = text;
            button.AddToClassList("choice-button");
            button.userData = index;

            // Register click callback
            int capturedIndex = index; // Capture for closure
            button.RegisterCallback<ClickEvent>(evt => OnChoiceButtonClick(capturedIndex));

            // Add hover effect feedback
            button.RegisterCallback<MouseEnterEvent>(evt => OnButtonHover(button, true));
            button.RegisterCallback<MouseLeaveEvent>(evt => OnButtonHover(button, false));

            _choiceList.Add(button);
            _choiceButtons.Add(button);
        }

        private void OnChoiceButtonClick(int index)
        {
            // Invoke event for external listeners
            OnChoiceSelected?.Invoke(index);

            // Notify DialogueManager
            _dialogueManager?.SelectChoice(index);

            // Hide after selection
            Hide();
        }

        private void OnButtonHover(Button button, bool isHovering)
        {
            // Additional hover effects can be added here if needed
            // The CSS already handles basic hover states
        }

        private void ClearChoices()
        {
            foreach (var button in _choiceButtons)
            {
                button.RemoveFromHierarchy();
            }
            _choiceButtons.Clear();
        }

        #endregion

        #region Keyboard Navigation (Optional)

        private void Update()
        {
            if (!_isVisible || _choiceButtons.Count == 0) return;

            // Number key selection (1-9)
            for (int i = 0; i < Mathf.Min(_choiceButtons.Count, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    OnChoiceButtonClick(i);
                    break;
                }
            }
        }

        #endregion
    }
}
