using System.Collections;
using UnityEngine;
using MyGame.Features.Character;
using MyGame.Features.Character.Data;

namespace MyGame.Features.Sequence
{
    [CreateAssetMenu(fileName = "PlayCharacterSequenceStep", menuName = "MyGame/Sequence/Steps/Play Character Sequence")]
    public class PlayCharacterSequenceStep : SequenceStep
    {
        [Tooltip("The Character Sequence (SO) to play.")]
        public CharacterActionSequence characterSequence;

        public override IEnumerator Execute(SequenceManager manager)
        {
            var actionManager = Object.FindObjectOfType<CharacterActionManager>();
            if (actionManager == null)
            {
                Debug.LogError("[PlayCharacterSequenceStep] CharacterActionManager not found in scene!");
                yield break;
            }

            if (characterSequence == null)
            {
                Debug.LogError("[PlayCharacterSequenceStep] No Character Sequence assigned!");
                yield break;
            }

            // Manually inject the sequence since the property is private/internal logic
            // We need to use reflection or modify CharacterActionManager to accept an SO at runtime nicely.
            // Looking at CharacterActionManager, it has `_sequenceAsset` serialized field.
            // But `LoadSequenceFromAsset()` is private.
            // Wait, I should make `LoadSequenceFromAsset` public or expose a method `SetSequence(CharacterActionSequence seq)`.
            
            // For now, let's assume I'll add a public method to CharacterActionManager.
            
            bool sequenceFinished = false;
            void OnFinished()
            {
                sequenceFinished = true;
            }

            actionManager.OnSequenceFinished += OnFinished;

            // We need a way to set the sequence.
            Debug.Log($"[PlayCharacterSequenceStep] Playing character sequence: {characterSequence.name}");
            actionManager.PlaySequence(characterSequence);

            while (!sequenceFinished)
            {
                yield return null;
            }

            actionManager.OnSequenceFinished -= OnFinished;
        }
    }
}
