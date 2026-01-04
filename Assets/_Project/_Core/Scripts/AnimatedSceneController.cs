using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace MyGame.Core
{
    /// <summary>
    /// Controls transitions between animated scenes in the game.
    /// Manages scene loading/unloading with smooth transitions and event callbacks.
    /// Uses UI Toolkit for fade transitions.
    /// </summary>
    public class AnimatedSceneController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Scene Configuration")]
        [Tooltip("List of animated scenes to manage (in order)")]
        [SerializeField] private List<string> animatedScenes = new List<string>();
        
        [Tooltip("Should scenes loop back to the first after the last one?")]
        [SerializeField] private bool loopScenes = true;
        
        [Tooltip("Auto-transition to next scene after duration (0 = manual only)")]
        [SerializeField] private float autoTransitionDelay = 0f;
        
        [Header("Transition Settings")]
        [Tooltip("Duration of fade out before scene change")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Tooltip("Duration of fade in after scene loads")]
        [SerializeField] private float fadeInDuration = 0.5f;
        
        [Tooltip("Color to fade to")]
        [SerializeField] private Color fadeColor = Color.black;
        
        [Tooltip("Should the new scene load additively?")]
        [SerializeField] private bool loadAdditive = false;
        
        [Tooltip("Unload previous scene when loading additively?")]
        [SerializeField] private bool unloadPreviousScene = true;
        
        [Tooltip("Automatically load the first scene on Start?")]
        [SerializeField] private bool autoLoadFirstScene = false;
        
        [Header("UI Toolkit References")]
        [Tooltip("UIDocument for fade overlay")]
        [SerializeField] private UIDocument fadeUIDocument;
        
        [Tooltip("Name of the fade overlay element in UXML (default: 'fade-overlay')")]
        [SerializeField] private string fadeOverlayName = "fade-overlay";
        
        [Tooltip("Name of the label element to show text on (default: 'loading-label')")]
        [SerializeField] private string loadingLabelName = "loading-label";
        
        [Header("Events")]
        [SerializeField] private UnityEvent<string> onSceneTransitionStarted;
        [SerializeField] private UnityEvent<string> onSceneLoaded;
        [SerializeField] private UnityEvent<string> onSceneTransitionCompleted;
        [SerializeField] private UnityEvent<float> onTransitionProgress;
        
        #endregion
        
        #region C# Events
        
        /// <summary>
        /// Fired when a scene transition begins.
        /// </summary>
        public event Action<string> OnSceneTransitionStarted;
        
        /// <summary>
        /// Fired when a new scene has finished loading (before fade in).
        /// </summary>
        public event Action<string> OnSceneLoaded;
        
        /// <summary>
        /// Fired when the entire transition is complete (after fade in).
        /// </summary>
        public event Action<string> OnSceneTransitionCompleted;
        
        /// <summary>
        /// Fired during transition with progress value (0-1).
        /// </summary>
        public event Action<float> OnTransitionProgress;
        
        #endregion
        
        #region Private Fields
        
        private int _currentSceneIndex = -1;
        private string _currentSceneName;
        private bool _isTransitioning = false;
        private Coroutine _autoTransitionCoroutine;
        private Scene _previousScene;
        
        // UI Toolkit references
        private VisualElement _fadeOverlay;
        private Label _loadingLabel;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// The currently active animated scene name.
        /// </summary>
        public string CurrentSceneName => _currentSceneName;
        
        /// <summary>
        /// The index of the current scene in the animatedScenes list.
        /// </summary>
        public int CurrentSceneIndex => _currentSceneIndex;
        
        /// <summary>
        /// Is a scene transition currently in progress?
        /// </summary>
        public bool IsTransitioning => _isTransitioning;
        
        /// <summary>
        /// Total number of animated scenes configured.
        /// </summary>
        public int SceneCount => animatedScenes.Count;
        
        /// <summary>
        /// Can we transition to the next scene?
        /// </summary>
        public bool CanTransitionNext => _currentSceneIndex < animatedScenes.Count - 1 || loopScenes;
        
        /// <summary>
        /// Can we transition to the previous scene?
        /// </summary>
        public bool CanTransitionPrevious => _currentSceneIndex > 0 || loopScenes;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateConfiguration();
            InitializeFadeOverlay();
        }
        
        private void Start()
        {
            // Find current scene if we're starting in an animated scene
            DetectCurrentScene();
            
            // Auto-load first scene if enabled and not already in an animated scene
            if (autoLoadFirstScene && _currentSceneIndex == -1 && animatedScenes.Count > 0)
            {
                Debug.Log($"<color=cyan>[AnimatedSceneController]</color> Auto-loading first scene: {animatedScenes[0]}");
                TransitionToSceneByIndex(0);
            }
        }
        
        private void OnDestroy()
        {
            if (_autoTransitionCoroutine != null)
            {
                StopCoroutine(_autoTransitionCoroutine);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Transitions to the next scene in the list.
        /// </summary>
        public void TransitionToNextScene()
        {
            if (!CanTransitionNext)
            {
                Debug.LogWarning("[AnimatedSceneController] Cannot transition to next scene: at end of list and looping is disabled.");
                return;
            }
            
            int nextIndex = (_currentSceneIndex + 1) % animatedScenes.Count;
            TransitionToSceneByIndex(nextIndex);
        }
        
        /// <summary>
        /// Transitions to the previous scene in the list.
        /// </summary>
        public void TransitionToPreviousScene()
        {
            if (!CanTransitionPrevious)
            {
                Debug.LogWarning("[AnimatedSceneController] Cannot transition to previous scene: at start of list and looping is disabled.");
                return;
            }
            
            int prevIndex = _currentSceneIndex - 1;
            if (prevIndex < 0 && loopScenes)
            {
                prevIndex = animatedScenes.Count - 1;
            }
            
            TransitionToSceneByIndex(prevIndex);
        }
        
        /// <summary>
        /// Transitions to a specific scene by its index in the animatedScenes list.
        /// </summary>
        /// <param name="index">The index of the scene to load.</param>
        public void TransitionToSceneByIndex(int index)
        {
            if (index < 0 || index >= animatedScenes.Count)
            {
                Debug.LogError($"[AnimatedSceneController] Invalid scene index: {index}. Valid range is 0-{animatedScenes.Count - 1}");
                return;
            }
            
            string sceneName = animatedScenes[index];
            TransitionToScene(sceneName, index);
        }
        
        /// <summary>
        /// Transitions to a specific scene by name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="transitionText">Optional text to show during transition.</param>
        public void TransitionToSceneByName(string sceneName, string transitionText = null)
        {
            int index = animatedScenes.IndexOf(sceneName);
            if (index == -1)
            {
                Debug.LogError($"[AnimatedSceneController] Scene '{sceneName}' not found in animatedScenes list.");
                return;
            }
            
            TransitionToScene(sceneName, index, transitionText);
        }
        
        /// <summary>
        /// Jumps to a scene immediately without transition effects.
        /// </summary>
        /// <param name="index">The index of the scene to load.</param>
        public void JumpToSceneByIndex(int index)
        {
            if (index < 0 || index >= animatedScenes.Count)
            {
                Debug.LogError($"[AnimatedSceneController] Invalid scene index: {index}");
                return;
            }
            
            _currentSceneIndex = index;
            _currentSceneName = animatedScenes[index];
            
            if (loadAdditive)
            {
                SceneManager.LoadScene(_currentSceneName, LoadSceneMode.Additive);
            }
            else
            {
                SceneManager.LoadScene(_currentSceneName, LoadSceneMode.Single);
            }
            
            Debug.Log($"[AnimatedSceneController] Jumped to scene: {_currentSceneName} (index {index})");
        }
        
        /// <summary>
        /// Sets the auto-transition delay. Set to 0 to disable auto-transitions.
        /// </summary>
        /// <param name="delay">Delay in seconds between scenes.</param>
        public void SetAutoTransitionDelay(float delay)
        {
            autoTransitionDelay = Mathf.Max(0f, delay);
            
            // Restart auto-transition if currently active
            if (_autoTransitionCoroutine != null)
            {
                StopCoroutine(_autoTransitionCoroutine);
                _autoTransitionCoroutine = null;
            }
            
            if (autoTransitionDelay > 0f && !_isTransitioning)
            {
                _autoTransitionCoroutine = StartCoroutine(AutoTransitionCoroutine());
            }
        }
        
        /// <summary>
        /// Stops any auto-transition that's in progress.
        /// </summary>
        public void StopAutoTransition()
        {
            if (_autoTransitionCoroutine != null)
            {
                StopCoroutine(_autoTransitionCoroutine);
                _autoTransitionCoroutine = null;
            }
        }
        
        /// <summary>
        /// Gets the name of a scene at a specific index.
        /// </summary>
        public string GetSceneNameAtIndex(int index)
        {
            if (index >= 0 && index < animatedScenes.Count)
            {
                return animatedScenes[index];
            }
            return null;
        }

        /// <summary>
        /// Processes a transition command string (e.g. "SceneA_to_SceneB" or just "SceneB").
        /// </summary>
        /// <param name="command">The command string to parse.</param>
        public void ProcessTransitionCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                Debug.LogWarning("[AnimatedSceneController] Received empty transition command.");
                return;
            }

            string targetScene = command;

            // Check for "source_to_target" format
            if (command.Contains("_to_"))
            {
                string[] parts = command.Split(new string[] { "_to_" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    // Optional: could verify we are currently in parts[0]
                    targetScene = parts[1];
                    Debug.Log($"[AnimatedSceneController] Parsed command '{command}' -> Target: '{targetScene}'");
                }
                else
                {
                    Debug.LogWarning($"[AnimatedSceneController] Invalid command format: '{command}'. Expected 'Source_to_Target'.");
                    return;
                }
            }

            TransitionToSceneByName(targetScene);
        }
        
        #endregion
        
        #region Private Methods
        
        private void ValidateConfiguration()
        {
            if (animatedScenes == null || animatedScenes.Count == 0)
            {
                Debug.LogWarning("[AnimatedSceneController] No animated scenes configured.");
            }
            
            // Remove null or empty scene names
            animatedScenes.RemoveAll(string.IsNullOrEmpty);
        }
        
        private void InitializeFadeOverlay()
        {
            if (fadeUIDocument == null)
            {
                Debug.LogWarning("[AnimatedSceneController] No UIDocument assigned. Transitions will be instant.");
                return;
            }
            
            // Get the root visual element
            VisualElement root = fadeUIDocument.rootVisualElement;
            
            // Find the fade overlay element
            _fadeOverlay = root.Q<VisualElement>(fadeOverlayName);
            _loadingLabel = root.Q<Label>(loadingLabelName);
            
            if (_fadeOverlay == null)
            {
                Debug.LogWarning($"[AnimatedSceneController] Fade overlay element '{fadeOverlayName}' not found in UIDocument. Transitions will be instant.");
                return;
            }
            
            // Set initial state
            _fadeOverlay.style.backgroundColor = fadeColor;
            _fadeOverlay.style.opacity = 0f;
            _fadeOverlay.style.display = DisplayStyle.None;
            _fadeOverlay.pickingMode = PickingMode.Ignore;
        }
        
        private void DetectCurrentScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            int index = animatedScenes.IndexOf(activeScene.name);
            
            if (index != -1)
            {
                _currentSceneIndex = index;
                _currentSceneName = activeScene.name;
                Debug.Log($"[AnimatedSceneController] Detected current scene: {_currentSceneName} (index {index})");
            }
        }
        
        private void TransitionToScene(string sceneName, int sceneIndex, string transitionText = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[AnimatedSceneController] Transition already in progress.");
                return;
            }
            
            StartCoroutine(TransitionCoroutine(sceneName, sceneIndex, transitionText));
        }
        
        private IEnumerator TransitionCoroutine(string targetSceneName, int targetSceneIndex, string transitionText)
        {
            _isTransitioning = true;
            
            // Stop auto-transition during manual transition
            StopAutoTransition();
            
            // Set transition text if provided
            if (_loadingLabel != null && !string.IsNullOrEmpty(transitionText))
            {
                _loadingLabel.text = transitionText;
            }
            
            // Fire started events
            OnSceneTransitionStarted?.Invoke(targetSceneName);
            onSceneTransitionStarted?.Invoke(targetSceneName);
            
            Debug.Log($"[AnimatedSceneController] Starting transition to: {targetSceneName}");
            
            // Phase 1: Fade Out
            if (_fadeOverlay != null)
            {
                yield return FadeOut();
            }
            
            // Phase 2: Load Scene
            ReportProgress(0.33f);
            
            if (loadAdditive)
            {
                // Store previous scene for unloading
                if (_currentSceneName != null && unloadPreviousScene)
                {
                    _previousScene = SceneManager.GetSceneByName(_currentSceneName);
                }
                
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
                
                while (!loadOperation.isDone)
                {
                    float progress = Mathf.Lerp(0.33f, 0.66f, loadOperation.progress);
                    ReportProgress(progress);
                    yield return null;
                }
                
                // Set as active scene
                Scene newScene = SceneManager.GetSceneByName(targetSceneName);
                if (newScene.IsValid())
                {
                    SceneManager.SetActiveScene(newScene);
                }
                
                // Unload previous scene
                if (_previousScene.IsValid() && unloadPreviousScene)
                {
                    yield return SceneManager.UnloadSceneAsync(_previousScene);
                }
            }
            else
            {
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
                
                while (!loadOperation.isDone)
                {
                    float progress = Mathf.Lerp(0.33f, 0.66f, loadOperation.progress);
                    ReportProgress(progress);
                    yield return null;
                }
            }
            
            // Update state
            _currentSceneIndex = targetSceneIndex;
            _currentSceneName = targetSceneName;
            
            // Fire loaded event
            OnSceneLoaded?.Invoke(targetSceneName);
            onSceneLoaded?.Invoke(targetSceneName);
            
            ReportProgress(0.66f);
            
            // Phase 3: Fade In
            if (_fadeOverlay != null)
            {
                yield return FadeIn();
            }
            
            ReportProgress(1f);
            
            // Fire completed events
            OnSceneTransitionCompleted?.Invoke(targetSceneName);
            onSceneTransitionCompleted?.Invoke(targetSceneName);
            
            Debug.Log($"[AnimatedSceneController] Transition complete: {targetSceneName}");
            
            _isTransitioning = false;
            
            // Restart auto-transition if enabled
            if (autoTransitionDelay > 0f)
            {
                _autoTransitionCoroutine = StartCoroutine(AutoTransitionCoroutine());
            }
        }
        
        private IEnumerator FadeOut()
        {
            if (_fadeOverlay == null) yield break;
            
            _fadeOverlay.style.display = DisplayStyle.Flex;
            _fadeOverlay.pickingMode = PickingMode.Position; // Block input during fade
            
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
                _fadeOverlay.style.opacity = alpha;
                yield return null;
            }
            
            _fadeOverlay.style.opacity = 1f;
        }
        
        private IEnumerator FadeIn()
        {
            if (_fadeOverlay == null) yield break;
            
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
                _fadeOverlay.style.opacity = alpha;
                yield return null;
            }
            
            _fadeOverlay.style.opacity = 0f;
            _fadeOverlay.style.display = DisplayStyle.None;
            _fadeOverlay.pickingMode = PickingMode.Ignore;
        }
        
        private IEnumerator AutoTransitionCoroutine()
        {
            yield return new WaitForSeconds(autoTransitionDelay);
            
            if (CanTransitionNext && !_isTransitioning)
            {
                TransitionToNextScene();
            }
        }
        
        private void ReportProgress(float progress)
        {
            OnTransitionProgress?.Invoke(progress);
            onTransitionProgress?.Invoke(progress);
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        [ContextMenu("Detect Animated Scenes")]
        private void DetectAnimatedScenesInProject()
        {
            animatedScenes.Clear();
            
            // Find all scenes in Animated_Scenes folder
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes/Animated_Scenes" });
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                animatedScenes.Add(sceneName);
            }
            
            animatedScenes.Sort();
            Debug.Log($"[AnimatedSceneController] Detected {animatedScenes.Count} animated scenes.");
        }
        
        [ContextMenu("Add Current Scene to Build Settings")]
        private void AddScenesToBuildSettings()
        {
            var scenes = new List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
            
            foreach (string sceneName in animatedScenes)
            {
                string[] foundScenes = UnityEditor.AssetDatabase.FindAssets($"{sceneName} t:Scene");
                if (foundScenes.Length > 0)
                {
                    string scenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(foundScenes[0]);
                    
                    // Check if already in build settings
                    bool alreadyAdded = scenes.Exists(s => s.path == scenePath);
                    
                    if (!alreadyAdded)
                    {
                        scenes.Add(new UnityEditor.EditorBuildSettingsScene(scenePath, true));
                        Debug.Log($"Added to build settings: {scenePath}");
                    }
                }
            }
            
            UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
        }
#endif
        
        #endregion
    }
}
