using UnityEngine;
using MyGame.Core;

namespace MyGame.Examples
{
    /// <summary>
    /// Example script demonstrating how to use AnimatedSceneController.
    /// Attach this to a GameObject with AnimatedSceneController to see it in action.
    /// </summary>
    public class SceneControllerExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AnimatedSceneController sceneController;
        
        [Header("Example Settings")]
        [SerializeField] private KeyCode nextSceneKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode previousSceneKey = KeyCode.LeftArrow;
        [SerializeField] private bool enableKeyboardControls = true;
        
        private void Start()
        {
            // Auto-find controller if not assigned
            if (sceneController == null)
            {
                sceneController = GetComponent<AnimatedSceneController>();
            }
            
            if (sceneController == null)
            {
                Debug.LogWarning("[SceneControllerExample] No AnimatedSceneController found!");
                return;
            }
            
            // Subscribe to events
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            if (!enableKeyboardControls || sceneController == null) return;
            
            // Keyboard controls for testing
            if (Input.GetKeyDown(nextSceneKey))
            {
                sceneController.TransitionToNextScene();
            }
            
            if (Input.GetKeyDown(previousSceneKey))
            {
                sceneController.TransitionToPreviousScene();
            }
        }
        
        private void SubscribeToEvents()
        {
            if (sceneController == null) return;
            
            sceneController.OnSceneTransitionStarted += HandleTransitionStarted;
            sceneController.OnSceneLoaded += HandleSceneLoaded;
            sceneController.OnSceneTransitionCompleted += HandleTransitionCompleted;
            sceneController.OnTransitionProgress += HandleTransitionProgress;
        }
        
        private void UnsubscribeFromEvents()
        {
            if (sceneController == null) return;
            
            sceneController.OnSceneTransitionStarted -= HandleTransitionStarted;
            sceneController.OnSceneLoaded -= HandleSceneLoaded;
            sceneController.OnSceneTransitionCompleted -= HandleTransitionCompleted;
            sceneController.OnTransitionProgress -= HandleTransitionProgress;
        }
        
        private void HandleTransitionStarted(string sceneName)
        {
            Debug.Log($"[Example] Transition started to: {sceneName}");
            // Here you could:
            // - Show loading UI
            // - Pause gameplay
            // - Play transition sound
        }
        
        private void HandleSceneLoaded(string sceneName)
        {
            Debug.Log($"[Example] Scene loaded: {sceneName}");
            // Here you could:
            // - Initialize scene-specific systems
            // - Update UI labels
            // - Prepare gameplay elements
        }
        
        private void HandleTransitionCompleted(string sceneName)
        {
            Debug.Log($"[Example] Transition completed: {sceneName}");
            // Here you could:
            // - Resume gameplay
            // - Hide loading UI
            // - Start cutscene/dialogue
        }
        
        private void HandleTransitionProgress(float progress)
        {
            // Update loading bar if you have one
            // Debug.Log($"[Example] Transition progress: {progress:P0}");
        }
        
        #region Public API for UI Buttons
        
        /// <summary>
        /// Call this from UI Button OnClick events
        /// </summary>
        public void OnNextButtonClicked()
        {
            if (sceneController != null)
            {
                sceneController.TransitionToNextScene();
            }
        }
        
        /// <summary>
        /// Call this from UI Button OnClick events
        /// </summary>
        public void OnPreviousButtonClicked()
        {
            if (sceneController != null)
            {
                sceneController.TransitionToPreviousScene();
            }
        }
        
        /// <summary>
        /// Jump to a specific scene by index
        /// </summary>
        public void JumpToScene(int sceneIndex)
        {
            if (sceneController != null)
            {
                sceneController.TransitionToSceneByIndex(sceneIndex);
            }
        }
        
        #endregion
    }
}
