using System;
using System.Collections;
using System.Collections.Generic;
using MyGame.Features.Dialogue.Events;
using MyGame.Features.Dialogue.Models;
using UnityEngine;

namespace MyGame.Features.Dialogue
{
    /// <summary>
    /// Core controller for the visual novel dialogue system.
    /// Manages dialogue flow, events, and state.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Auto-advance to next dialogue after delay (0 = manual advance only)")]
        [SerializeField] private float autoAdvanceDelay = 0f;

        [Header("References")]
        [SerializeField] private DialogueEventProcessor eventProcessor;

        // Events for UI and other systems to subscribe to
        public event Action<DialogueEntry> OnDialogueStarted;
        public event Action<DialogueEntry> OnDialogueChanged;
        public event Action<List<ChoiceOption>> OnChoicesPresented;
        public event Action OnDialogueEnded;
        public event Action<string> OnSpeakerChanged;
        public event Action OnSceneTransitionStart;

        // Current state
        private DialogueDatabase _currentDatabase;
        private DialogueEntry _currentEntry;
        private bool _isDialogueActive;
        private bool _isWaitingForInput;
        private bool _isProcessingEvents;
        private Coroutine _autoAdvanceCoroutine;

        // Public state access (for save/load)
        public DialogueDatabase CurrentDatabase => _currentDatabase;
        public DialogueEntry CurrentEntry => _currentEntry;
        public bool IsDialogueActive => _isDialogueActive;
        public bool IsWaitingForInput => _isWaitingForInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize event processor if not assigned
            if (eventProcessor == null)
            {
                eventProcessor = GetComponent<DialogueEventProcessor>();
                if (eventProcessor == null)
                {
                    eventProcessor = gameObject.AddComponent<DialogueEventProcessor>();
                }
            }
        }

        /// <summary>
        /// Starts dialogue from a database, optionally at a specific entry.
        /// </summary>
        /// <param name="database">The dialogue database to use.</param>
        /// <param name="startTextId">Optional starting entry ID. If null, uses first entry.</param>
        public void StartDialogue(DialogueDatabase database, string startTextId = null)
        {
            if (database == null)
            {
                Debug.LogError("[DialogueManager] Cannot start dialogue: database is null");
                return;
            }

            if (_isDialogueActive)
            {
                Debug.LogWarning("[DialogueManager] Ending current dialogue before starting new one");
                EndDialogue();
            }

            _currentDatabase = database;
            _currentDatabase.Initialize();
            _isDialogueActive = true;

            // Find starting entry
            DialogueEntry startEntry;
            if (!string.IsNullOrEmpty(startTextId))
            {
                startEntry = database.GetEntry(startTextId);
                if (startEntry == null)
                {
                    Debug.LogError($"[DialogueManager] Start entry '{startTextId}' not found");
                    return;
                }
            }
            else
            {
                // Use first entry (by order)
                var scenes = database.GetAllSceneIDs();
                if (scenes.Length == 0)
                {
                    Debug.LogError("[DialogueManager] Database has no scenes");
                    return;
                }
                startEntry = database.GetFirstEntryOfScene(scenes[0]);
            }

            _currentEntry = startEntry;
            OnDialogueStarted?.Invoke(_currentEntry);
            ProcessCurrentEntry();
        }

        /// <summary>
        /// Starts dialogue from a specific scene.
        /// </summary>
        /// <param name="database">The dialogue database to use.</param>
        /// <param name="sceneId">The scene ID to start from.</param>
        public void StartDialogueFromScene(DialogueDatabase database, string sceneId)
        {
            if (database == null)
            {
                Debug.LogError("[DialogueManager] Cannot start dialogue: database is null");
                return;
            }

            database.Initialize();
            var firstEntry = database.GetFirstEntryOfScene(sceneId);

            if (firstEntry == null)
            {
                Debug.LogError($"[DialogueManager] Scene '{sceneId}' not found or empty");
                return;
            }

            StartDialogue(database, firstEntry.TextID);
        }

        /// <summary>
        /// Advances to the next dialogue entry. Called when player clicks/taps.
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!_isDialogueActive || !_isWaitingForInput)
            {
                return;
            }

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            // Check for end of dialogue
            if (_currentEntry.IsEndOfDialogue)
            {
                EndDialogue();
                return;
            }

            // Check for choices
            if (_currentEntry.HasChoices)
            {
                PresentChoices();
                return;
            }

            // Advance to next entry
            var nextEntry = _currentDatabase.GetEntry(_currentEntry.NextID);
            if (nextEntry == null)
            {
                Debug.LogError($"[DialogueManager] Next entry '{_currentEntry.NextID}' not found");
                EndDialogue();
                return;
            }

            _currentEntry = nextEntry;
            ProcessCurrentEntry();
        }

        /// <summary>
        /// Selects a choice option. Called when player clicks a choice button.
        /// </summary>
        /// <param name="choiceIndex">Index of the selected choice.</param>
        public void SelectChoice(int choiceIndex)
        {
            if (!_isDialogueActive || !_currentEntry.HasChoices)
            {
                return;
            }

            var choiceIds = _currentEntry.GetChoiceIDs();
            if (choiceIndex < 0 || choiceIndex >= choiceIds.Length)
            {
                Debug.LogError($"[DialogueManager] Invalid choice index: {choiceIndex}");
                return;
            }

            var choiceId = choiceIds[choiceIndex].Trim();
            var choiceEntry = _currentDatabase.GetEntry(choiceId);

            if (choiceEntry == null)
            {
                Debug.LogError($"[DialogueManager] Choice entry '{choiceId}' not found");
                return;
            }

            _currentEntry = choiceEntry;
            ProcessCurrentEntry();
        }

        /// <summary>
        /// Ends the current dialogue.
        /// </summary>
        public void EndDialogue()
        {
            if (!_isDialogueActive) return;

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            _isDialogueActive = false;
            _isWaitingForInput = false;
            _currentEntry = null;
            _currentDatabase = null;

            OnDialogueEnded?.Invoke();
            Debug.Log("[DialogueManager] Dialogue ended");
        }

        /// <summary>
        /// Jumps to a specific entry by TextID. Useful for save/load.
        /// </summary>
        /// <param name="textId">The entry ID to jump to.</param>
        public void JumpToEntry(string textId)
        {
            if (_currentDatabase == null)
            {
                Debug.LogError("[DialogueManager] Cannot jump: no active database");
                return;
            }

            var entry = _currentDatabase.GetEntry(textId);
            if (entry == null)
            {
                Debug.LogError($"[DialogueManager] Entry '{textId}' not found");
                return;
            }

            _currentEntry = entry;
            ProcessCurrentEntry();
        }

        /// <summary>
        /// Processes the current dialogue entry (events, UI update).
        /// </summary>
        private void ProcessCurrentEntry()
        {
            if (_currentEntry == null) return;

            _isWaitingForInput = false;
            _isProcessingEvents = false;

            // Process events first
            if (_currentEntry.HasEvents && eventProcessor != null)
            {
                _isProcessingEvents = true;
                eventProcessor.ProcessEventTag(_currentEntry.EventTag, OnEventsComplete);
            }
            else
            {
                FinishProcessingEntry();
            }
        }

        private void OnEventsComplete()
        {
            _isProcessingEvents = false;
            FinishProcessingEntry();
        }

        private void FinishProcessingEntry()
        {
            // Notify speaker change
            OnSpeakerChanged?.Invoke(_currentEntry.Speaker);

            // Notify UI of new dialogue
            OnDialogueChanged?.Invoke(_currentEntry);

            // Set waiting for input
            _isWaitingForInput = true;

            // Auto-advance if configured
            if (autoAdvanceDelay > 0 && !_currentEntry.HasChoices && !_currentEntry.IsEndOfDialogue)
            {
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay());
            }
        }

        private IEnumerator AutoAdvanceAfterDelay()
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            AdvanceDialogue();
        }

        /// <summary>
        /// Presents choice options to the player.
        /// </summary>
        private void PresentChoices()
        {
            var choiceIds = _currentEntry.GetChoiceIDs();
            var choices = new List<ChoiceOption>();

            foreach (var choiceId in choiceIds)
            {
                var choiceEntry = _currentDatabase.GetEntry(choiceId.Trim());
                if (choiceEntry != null)
                {
                    choices.Add(new ChoiceOption
                    {
                        TextID = choiceEntry.TextID,
                        DisplayText = choiceEntry.DialogueText
                    });
                }
                else
                {
                    Debug.LogWarning($"[DialogueManager] Choice entry '{choiceId}' not found");
                }
            }

            if (choices.Count > 0)
            {
                _isWaitingForInput = false; // Wait for choice selection instead
                OnChoicesPresented?.Invoke(choices);
            }
            else
            {
                Debug.LogError("[DialogueManager] No valid choices found");
                EndDialogue();
            }
        }
    }

    /// <summary>
    /// Represents a choice option presented to the player.
    /// </summary>
    [Serializable]
    public class ChoiceOption
    {
        public string TextID;
        public string DisplayText;
    }
}
