using System.Collections;
using UnityEngine;
using MyGame.Core.Audio;

namespace MyGame.Features.Sequence
{
    [CreateAssetMenu(fileName = "PlayBGMStep", menuName = "Sequence/Audio/Play BGM", order = 100)]
    public class PlayBGMStep : SequenceStep
    {
        [Header("BGM Settings")]
        [Tooltip("Key of the BGM in the AudioDB")]
        public string bgmKey;

        [Tooltip("Fade duration in seconds")]
        public float fadeTime = 1f;

        [Tooltip("Wait for fade to complete before continuing")]
        public bool waitForFade = false;

        public override IEnumerator Execute(SequenceManager manager)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning($"[PlayBGMStep] AudioManager not found. Cannot play: {bgmKey}");
                yield break;
            }

            AudioManager.Instance.PlayBGM(bgmKey, fadeTime);

            if (waitForFade)
            {
                yield return new WaitForSeconds(fadeTime);
            }
        }
    }
}
