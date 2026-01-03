using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Runtime
{
    /// <summary>
    /// Controls the history/log panel UI using UI Toolkit.
    /// Displays a scrollable list of past dialogue entries.
    /// </summary>
    public class HistoryUIController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument historyDocument;

        [Header("Settings")]
        [SerializeField] private int maxHistoryEntries = 100;
        [SerializeField] private bool scrollToBottomOnAdd = true;

        // Events
        public event Action OnHistoryOpened;
        public event Action OnHistoryClosed;
        public event Action<int> OnHistoryEntryClicked;

        // UI Elements
        private VisualElement _root;
        private VisualElement _historyContainer;
        private Label _historyTitle;
        private Button _closeButton;
        private ScrollView _historyScroll;
        private VisualElement _historyList;

        // State
        private List<HistoryEntry> _historyEntries = new List<HistoryEntry>();
        private bool _isVisible;
        private DialogueManager _dialogueManager;

        /// <summary>
        /// Represents a single history entry.
        /// </summary>
        [Serializable]
        public class HistoryEntry
        {
            public string Speaker;
            public string Text;
            public string TextID;
            public DateTime Timestamp;
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
            if (historyDocument == null)
            {
                Debug.LogError("[HistoryUIController] UIDocument not assigned");
                return;
            }

            _root = historyDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[HistoryUIController] Root visual element is null");
                return;
            }

            _historyContainer = _root.Q<VisualElement>("history-container");
            _historyTitle = _root.Q<Label>("history-title");
            _closeButton = _root.Q<Button>("history-close");
            _historyScroll = _root.Q<ScrollView>("history-scroll");
            _historyList = _root.Q<VisualElement>("history-list");

            RegisterCallbacks();

            // Start hidden
            Hide();
        }

        private void RegisterCallbacks()
        {
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseClick);
            
            // Close on background click
            _historyContainer?.RegisterCallback<ClickEvent>(OnContainerClick);
            
            // Keyboard escape to close
            _historyContainer?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void UnregisterCallbacks()
        {
            _closeButton?.UnregisterCallback<ClickEvent>(OnCloseClick);
            _historyContainer?.UnregisterCallback<ClickEvent>(OnContainerClick);
            _historyContainer?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void SubscribeToDialogueManager()
        {
            _dialogueManager = DialogueManager.Instance;
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueChanged += OnDialogueChanged;
                _dialogueManager.OnDialogueEnded += OnDialogueEnded;
            }
        }

        private void UnsubscribeFromDialogueManager()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueChanged -= OnDialogueChanged;
                _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
            }
        }

        #region DialogueManager Event Handlers

        private void OnDialogueChanged(DialogueEntry entry)
        {
            AddEntry(entry.Speaker, entry.DialogueText, entry.TextID);
        }

        private void OnDialogueEnded()
        {
            // Optionally clear history when dialogue ends
            // ClearHistory();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the history panel.
        /// </summary>
        public void Show()
        {
            if (_historyContainer != null)
            {
                _historyContainer.RemoveFromClassList("hidden");
                _historyContainer.AddToClassList("visible");
                _historyContainer.Focus();
                _isVisible = true;

                RefreshDisplay();

                if (scrollToBottomOnAdd)
                {
                    ScrollToBottom();
                }

                OnHistoryOpened?.Invoke();
            }
        }

        /// <summary>
        /// Hides the history panel.
        /// </summary>
        public void Hide()
        {
            if (_historyContainer != null)
            {
                _historyContainer.RemoveFromClassList("visible");
                _historyContainer.AddToClassList("hidden");
                _isVisible = false;

                OnHistoryClosed?.Invoke();
            }
        }

        /// <summary>
        /// Toggles the history panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Adds a new entry to the history.
        /// </summary>
        /// <param name="speaker">Speaker name</param>
        /// <param name="text">Dialogue text</param>
        /// <param name="textId">Optional text ID for reference</param>
        public void AddEntry(string speaker, string text, string textId = null)
        {
            var entry = new HistoryEntry
            {
                Speaker = speaker,
                Text = text,
                TextID = textId,
                Timestamp = DateTime.Now
            };

            _historyEntries.Add(entry);

            // Trim if exceeding max
            while (_historyEntries.Count > maxHistoryEntries)
            {
                _historyEntries.RemoveAt(0);
            }

            // Update UI if visible
            if (_isVisible)
            {
                AddEntryToUI(entry, _historyEntries.Count - 1);

                if (scrollToBottomOnAdd)
                {
                    ScrollToBottom();
                }
            }
        }

        /// <summary>
        /// Clears all history entries.
        /// </summary>
        public void ClearHistory()
        {
            _historyEntries.Clear();
            ClearUI();
        }

        /// <summary>
        /// Gets all history entries.
        /// </summary>
        public List<HistoryEntry> GetHistory()
        {
            return new List<HistoryEntry>(_historyEntries);
        }

        /// <summary>
        /// Gets the number of history entries.
        /// </summary>
        public int EntryCount => _historyEntries.Count;

        /// <summary>
        /// Gets whether the history panel is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Sets the title text.
        /// </summary>
        /// <param name="title">Title text</param>
        public void SetTitle(string title)
        {
            if (_historyTitle != null)
            {
                _historyTitle.text = title;
            }
        }

        #endregion

        #region Private Methods

        private void RefreshDisplay()
        {
            ClearUI();

            for (int i = 0; i < _historyEntries.Count; i++)
            {
                AddEntryToUI(_historyEntries[i], i);
            }
        }

        private void AddEntryToUI(HistoryEntry entry, int index)
        {
            if (_historyList == null) return;

            var entryElement = new VisualElement();
            entryElement.AddToClassList("history-entry");
            entryElement.userData = index;

            // Speaker label
            var speakerLabel = new Label();
            speakerLabel.AddToClassList("history-entry-speaker");

            if (string.IsNullOrEmpty(entry.Speaker) || 
                entry.Speaker.Equals("Narrator", StringComparison.OrdinalIgnoreCase))
            {
                speakerLabel.text = "[Narrator]";
                speakerLabel.style.color = new StyleColor(new Color(0.47f, 0.47f, 0.47f)); // Muted color
            }
            else
            {
                speakerLabel.text = entry.Speaker;
            }

            // Text label
            var textLabel = new Label();
            textLabel.AddToClassList("history-entry-text");
            textLabel.text = entry.Text;

            entryElement.Add(speakerLabel);
            entryElement.Add(textLabel);

            // Click to jump to entry (optional feature)
            int capturedIndex = index;
            entryElement.RegisterCallback<ClickEvent>(evt =>
            {
                OnHistoryEntryClicked?.Invoke(capturedIndex);
            });

            _historyList.Add(entryElement);
        }

        private void ClearUI()
        {
            if (_historyList != null)
            {
                _historyList.Clear();
            }
        }

        private void ScrollToBottom()
        {
            // Schedule scroll to ensure layout is complete
            _historyScroll?.schedule.Execute(() =>
            {
                _historyScroll.scrollOffset = new Vector2(0, float.MaxValue);
            }).ExecuteLater(50);
        }

        #endregion

        #region UI Event Handlers

        private void OnCloseClick(ClickEvent evt)
        {
            evt.StopPropagation();
            Hide();
        }

        private void OnContainerClick(ClickEvent evt)
        {
            // Only close if clicking the background, not content
            if (evt.target == _historyContainer)
            {
                Hide();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Hide();
                evt.StopPropagation();
            }
        }

        #endregion

        #region Keyboard Shortcut

        private void Update()
        {
            // L key to toggle history (when not visible, handled by DialogueUIController)
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        #endregion
    }
}
