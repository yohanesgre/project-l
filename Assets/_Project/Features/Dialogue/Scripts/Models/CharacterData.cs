using System;
using UnityEngine;

namespace MyGame.Features.Dialogue.Models
{
    /// <summary>
    /// ScriptableObject for character metadata.
    /// Placeholder for future 3D character support.
    /// </summary>
    [CreateAssetMenu(fileName = "Character", menuName = "Data/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique identifier matching the Speaker field in dialogues")]
        public string CharacterID;

        [Tooltip("Display name shown in the dialogue UI")]
        public string DisplayName;

        [Tooltip("Color of the character's name in the dialogue box")]
        public Color NameColor = Color.white;

        [Header("Audio")]
        [Tooltip("Sound played during text typing (optional)")]
        public AudioClip TextBlipSound;

        [Tooltip("Volume for text blip sound")]
        [Range(0f, 1f)]
        public float TextBlipVolume = 0.5f;

        // =========================================
        // Future 3D Character Support (Placeholder)
        // =========================================
        
        // [Header("3D Character")]
        // [Tooltip("The 3D character prefab to instantiate")]
        // public GameObject CharacterPrefab;
        
        // [Tooltip("Animator controller for expressions and animations")]
        // public RuntimeAnimatorController AnimatorController;
        
        // [Tooltip("Available expressions mapped to animation triggers")]
        // public ExpressionMapping[] Expressions;

        /// <summary>
        /// Gets the display name, falling back to CharacterID if not set.
        /// </summary>
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(DisplayName) ? CharacterID : DisplayName;
        }
    }

    // =========================================
    // Future Expression Mapping (Placeholder)
    // =========================================
    
    // [Serializable]
    // public class ExpressionMapping
    // {
    //     public string ExpressionName;  // Matches Expression field in DialogueEntry
    //     public string AnimatorTrigger; // Animator trigger to play
    //     public AnimationClip FacialAnimation; // Optional direct animation
    // }
}
