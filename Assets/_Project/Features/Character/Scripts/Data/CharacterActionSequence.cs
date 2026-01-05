using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Features.Character.Data
{
    [CreateAssetMenu(fileName = "NewCharacterActionSequence", menuName = "MyGame/Character/Action Sequence")]
    public class CharacterActionSequence : ScriptableObject
    {
        [Tooltip("Unique identifier for this sequence.")]
        public string sequenceId;

        [Tooltip("The ordered list of actions to perform.")]
        public List<CharacterActionDefinition> actions = new List<CharacterActionDefinition>();

        [Tooltip("If true, the system waits for a manual trigger before advancing to the next action.")]
        public bool requireManualTrigger = false;
        
        [Tooltip("Should the sequence loop automatically?")]
        public bool loop = false;
    }
}
