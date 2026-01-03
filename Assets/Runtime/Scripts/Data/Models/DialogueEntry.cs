using System;
using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// Represents a single dialogue entry matching the Google Sheet schema.
    /// Each entry corresponds to one row in the spreadsheet.
    /// </summary>
    [Serializable]
    public class DialogueEntry
    {
        [Tooltip("Unique identifier for this dialogue entry (e.g., 'ch1_001')")]
        public string TextID;

        [Tooltip("Scene or chapter grouping (e.g., 'chapter_1')")]
        public string SceneID;

        [Tooltip("Display order within the scene")]
        public int Order;

        [Tooltip("Character ID speaking this line, or 'narrator' for narration")]
        public string Speaker;

        [Tooltip("Character expression/emotion (for future 3D character support)")]
        public string Expression;

        [Tooltip("The actual dialogue text to display")]
        [TextArea(2, 5)]
        public string DialogueText;

        [Tooltip("Pipe-separated event tags (e.g., 'bg:cafe|sfx:door|wait:2')")]
        public string EventTag;

        [Tooltip("Next dialogue ID, or 'choice:id1,id2,id3' for branching")]
        public string NextID;

        [Tooltip("Developer notes (ignored at runtime)")]
        public string Notes;

        /// <summary>
        /// Checks if this entry has any events to process.
        /// </summary>
        public bool HasEvents => !string.IsNullOrEmpty(EventTag);

        /// <summary>
        /// Checks if this entry presents choices to the player.
        /// </summary>
        public bool HasChoices => !string.IsNullOrEmpty(NextID) && NextID.StartsWith("choice:");

        /// <summary>
        /// Checks if this is the end of a dialogue branch.
        /// </summary>
        public bool IsEndOfDialogue => string.IsNullOrEmpty(NextID) || NextID.Equals("end", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if this entry is narration (no speaker or narrator).
        /// </summary>
        public bool IsNarration => string.IsNullOrEmpty(Speaker) || Speaker.Equals("narrator", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the choice IDs if this entry has choices.
        /// </summary>
        /// <returns>Array of choice TextIDs, or empty array if no choices.</returns>
        public string[] GetChoiceIDs()
        {
            if (!HasChoices) return Array.Empty<string>();
            
            // Format: "choice:id1,id2,id3"
            var choicesPart = NextID.Substring("choice:".Length);
            return choicesPart.Split(',');
        }

        public override string ToString()
        {
            return $"[{TextID}] {Speaker}: {DialogueText?.Substring(0, Math.Min(30, DialogueText?.Length ?? 0))}...";
        }
    }
}
