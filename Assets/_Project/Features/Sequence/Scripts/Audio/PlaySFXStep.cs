using System.Collections;
using UnityEngine;
using MyGame.Core.Audio;

namespace MyGame.Features.Sequence
{
    [CreateAssetMenu(fileName = "PlaySFXStep", menuName = "Sequence/Audio/Play SFX", order = 102)]
    public class PlaySFXStep : SequenceStep
    {
        [Header("SFX Settings")]
        [Tooltip("Key of the SFX in the AudioDB")]
        public string sfxKey;

        [Tooltip("Volume scale multiplier (0-1)")]
        [Range(0f, 1f)]
        public float volumeScale = 1f;

        [Tooltip("Optional delay before playing")]
        public float delay = 0f;

        public override IEnumerator Execute(SequenceManager manager)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning($"[PlaySFXStep] AudioManager not found. Cannot play: {sfxKey}");
                yield break;
            }

            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            AudioManager.Instance.PlaySFX(sfxKey, volumeScale);
        }
    }
}
