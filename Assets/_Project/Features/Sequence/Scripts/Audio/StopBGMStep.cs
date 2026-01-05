using System.Collections;
using UnityEngine;
using MyGame.Core.Audio;

namespace MyGame.Features.Sequence
{
    [CreateAssetMenu(fileName = "StopBGMStep", menuName = "Sequence/Audio/Stop BGM", order = 101)]
    public class StopBGMStep : SequenceStep
    {
        [Header("BGM Settings")]
        [Tooltip("Fade duration in seconds")]
        public float fadeTime = 1f;

        [Tooltip("Wait for fade to complete before continuing")]
        public bool waitForFade = true;

        public override IEnumerator Execute(SequenceManager manager)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("[StopBGMStep] AudioManager not found");
                yield break;
            }

            AudioManager.Instance.StopBGM(fadeTime);

            if (waitForFade)
            {
                yield return new WaitForSeconds(fadeTime);
            }
        }
    }
}
