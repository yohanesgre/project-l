using System.Collections;
using UnityEngine;
using MyGame.Core;

namespace MyGame.Features.Sequence
{
    [CreateAssetMenu(fileName = "LoadSceneStep", menuName = "MyGame/Sequence/Steps/Load Scene")]
    public class LoadSceneStep : SequenceStep
    {
        [Tooltip("Name of the scene to load (must be in AnimatedSceneController list)")]
        public string sceneName;
        
        [Tooltip("Optional text to show during transition")]
        public string transitionText;

        public override IEnumerator Execute(SequenceManager manager)
        {
            var controller = Object.FindObjectOfType<AnimatedSceneController>();
            if (controller == null)
            {
                Debug.LogError("[LoadSceneStep] AnimatedSceneController not found!");
                yield break;
            }

            if (controller.CurrentSceneName == sceneName)
            {
                Debug.Log($"[LoadSceneStep] Already in scene '{sceneName}'. Skipping transition.");
                yield break;
            }

            bool transitionComplete = false;
            
            void OnComplete(string loadedScene)
            {
                if (loadedScene == sceneName)
                {
                    transitionComplete = true;
                }
            }

            controller.OnSceneTransitionCompleted += OnComplete;
            
            controller.TransitionToSceneByName(sceneName, transitionText);

            // Wait for completion
            while (!transitionComplete)
            {
                yield return null;
            }
            
            controller.OnSceneTransitionCompleted -= OnComplete;
        }
    }
}
