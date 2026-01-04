using MyGame.Core.Save;
using MyGame.Features.Dialogue.Models;
using UnityEngine;

namespace MyGame.Features.Dialogue.Save
{
    /// <summary>
    /// Adapter that implements ISaveableState for the Dialogue system.
    /// Bridges between DialogueManager and the generic SaveController.
    /// </summary>
    public class DialogueSaveAdapter : ISaveableState
    {
        private readonly DialogueManager _dialogueManager;

        public DialogueSaveAdapter(DialogueManager dialogueManager)
        {
            _dialogueManager = dialogueManager;
        }

        /// <summary>
        /// Whether the dialogue state can be saved.
        /// </summary>
        public bool CanSave => _dialogueManager != null && 
                               _dialogueManager.IsDialogueActive && 
                               _dialogueManager.CurrentEntry != null &&
                               _dialogueManager.CurrentDatabase != null;

        /// <summary>
        /// Captures the current dialogue state.
        /// </summary>
        public SaveStateData GetCurrentState()
        {
            if (!CanSave) return null;

            var entry = _dialogueManager.CurrentEntry;
            var database = _dialogueManager.CurrentDatabase;

            var state = SaveStateData.Create(
                sourceName: database.name,
                currentPosition: entry.TextID,
                sceneId: entry.SceneID,
                flags: null, // Flags are handled by SaveController
                previewText: GetPreviewText(entry.DialogueText)
            );

            return state;
        }

        /// <summary>
        /// Restores dialogue state from save data.
        /// </summary>
        public bool RestoreState(SaveStateData data)
        {
            if (_dialogueManager == null || data == null) return false;

            // Find the database asset
            var database = LoadDatabase(data.SourceName);
            if (database == null)
            {
                Debug.LogError($"[DialogueSaveAdapter] Database not found: {data.SourceName}");
                return false;
            }

            // Start dialogue from saved position
            _dialogueManager.StartDialogue(database, data.CurrentPosition);
            return true;
        }

        /// <summary>
        /// Gets a preview text snippet from dialogue text.
        /// </summary>
        private string GetPreviewText(string dialogueText)
        {
            if (string.IsNullOrEmpty(dialogueText)) return "";
            return dialogueText.Substring(0, Mathf.Min(50, dialogueText.Length));
        }

        /// <summary>
        /// Loads a DialogueDatabase by name.
        /// </summary>
        private DialogueDatabase LoadDatabase(string databaseName)
        {
            // Try loading from Resources
            var database = Resources.Load<DialogueDatabase>($"Dialogues/{databaseName}");
            if (database != null) return database;

            // Try finding in loaded assets
            var databases = Resources.FindObjectsOfTypeAll<DialogueDatabase>();
            foreach (var db in databases)
            {
                if (db.name == databaseName)
                {
                    return db;
                }
            }

            return null;
        }
    }
}
