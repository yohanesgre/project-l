using UnityEngine;
using MyGame.Core;

namespace MyGame.Features.Character
{
    /// <summary>
    /// Simple trigger to bridge CharacterActionManager events to AnimatedSceneController.
    /// </summary>
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [SerializeField] private string _targetSceneName = "Scene_Riding_Looping";
        
        private AnimatedSceneController _sceneController;

        private void Awake()
        {
            _sceneController = FindObjectOfType<AnimatedSceneController>();
            if (_sceneController == null)
            {
                Debug.LogWarning("[SceneTransitionTrigger] AnimatedSceneController not found in scene.");
            }
        }

        /// <summary>
        /// Triggers the scene transition defined in targetSceneName.
        /// Call this from UnityEvents (e.g. CharacterAction.onComplete).
        /// </summary>
        public void TriggerTransition()
        {
            if (_sceneController != null)
            {
                Debug.Log($"[SceneTransitionTrigger] Triggering transition to '{_targetSceneName}'");
                _sceneController.TransitionToSceneByName(_targetSceneName);
            }
            else
            {
                Debug.LogError("[SceneTransitionTrigger] Cannot transition: AnimatedSceneController is missing!");
            }
        }
        
        /// <summary>
        /// Triggers a transition to a specific scene name passed as parameter.
        /// </summary>
        public void TriggerTransitionTo(string sceneName)
        {
            if (_sceneController != null)
            {
                _sceneController.TransitionToSceneByName(sceneName);
            }
        }
    }
}
