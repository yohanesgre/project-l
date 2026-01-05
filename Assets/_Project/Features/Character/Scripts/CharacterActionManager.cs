using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using MyGame.Core;
using MyGame.Features.Character.Data;

namespace MyGame.Features.Character
{
    [Serializable]
    public struct SceneBinding
    {
        public string key;
        public GameObject target;
    }

    public enum ActionType
    {
        Idle,
        Ride,
        Teleport
    }

    [Serializable]
    public class CharacterAction
    {
        public string name = "New Action";
        public ActionType actionType;
        public CameraController.CameraMode cameraMode = CameraController.CameraMode.PathFollow;
        
        [Header("Transition")]
        [Tooltip("If true, the screen will fade to black before this action starts.")]
        public bool fadeOut = false;
        [Tooltip("If true, the screen will fade in (reveal scene) after this action completes.")]
        public bool fadeIn = false;
        [Tooltip("Duration of the fade in/out effect.")]
        public float fadeDuration = 0.5f;
        
        [Header("Idle Settings")]
        [Tooltip("Duration for Idle action in seconds")]
        public float duration = 2f;
        
        [Header("Ride Settings")]
        [Tooltip("Target path object (must have IPathProvider component)")]
        public GameObject pathTarget;
        [Tooltip("Progress along the path to start at (0-1)")]
        [Range(0f, 1f)]
        public float startProgress = 0f;
        [Tooltip("Progress along the path to finish at (0-1)")]
        [Range(0f, 1f)]
        public float finishProgress = 1f;
        [Tooltip("If true, the character will loop on the path until manual intervention.")]
        public bool loop = false;
        
        [Header("Teleport Settings")]
        [Tooltip("Target transform to teleport to")]
        public Transform teleportTarget;
        
        [Header("Events")]
        [Tooltip("Events to fire when this action starts")]
        public UnityEvent onStart;
        
        [Tooltip("Events to fire when this action completes")]
        public UnityEvent onComplete;
        
        // Runtime extension for command support from SO
        [HideInInspector] public string onCompleteCommand; 
    }

    /// <summary>
    /// Scene-level manager that directs a character to perform a sequence of actions.
    /// Supports UI Toolkit based fade transitions to hide actions.
    /// </summary>
    public class CharacterActionManager : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("The character to control. Must have a CharacterPathFollower component.")]
        [SerializeField] private CharacterPathFollower _targetCharacter;

        [Tooltip("The CameraController to control. If empty, it will be auto-found.")]
        [SerializeField] private CameraController _cameraController;

        [Header("UI Toolkit References")]
        [Tooltip("UIDocument for fade overlay")]
        [SerializeField] private UIDocument fadeUIDocument;
        [Tooltip("Name of the fade overlay element in UXML (default: 'action-fade-overlay')")]
        [SerializeField] private string fadeOverlayName = "action-fade-overlay";

        [Header("Configuration")]
        [Tooltip("Optional: Load actions from a ScriptableObject sequence.")]
        [SerializeField] private CharacterActionSequence _sequenceAsset;
        
        [Tooltip("Map keys used in the Sequence Asset to actual Scene Objects.")]
        [SerializeField] private List<SceneBinding> _sceneBindings = new List<SceneBinding>();

        [Tooltip("List of actions (Runtime buffer). If Sequence Asset is assigned, this is overwritten at start.")]
        [SerializeField] private List<CharacterAction> _actions = new List<CharacterAction>();
        
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private bool _loopActions = false;
        [SerializeField] private bool _requireManualTrigger = false;

        [Header("Debug/State")]
        [SerializeField] private int _currentActionIndex = -1;
        [SerializeField] private bool _isPlaying = false;
        [SerializeField] private string _currentActionName = "None";

        /// <summary>
        /// Fired when the entire action sequence finishes (and loop is false).
        /// </summary>
        public event Action OnSequenceFinished;

        private Coroutine _currentActionCoroutine;
        private VisualElement _fadeOverlay;
        private Dictionary<string, GameObject> _bindingCache = new Dictionary<string, GameObject>();

        private void RebuildBindingCache()
        {
            _bindingCache.Clear();
            foreach (var binding in _sceneBindings)
            {
                if (!string.IsNullOrEmpty(binding.key) && !_bindingCache.ContainsKey(binding.key))
                {
                    _bindingCache.Add(binding.key, binding.target);
                }
            }
        }
        
        // When played via SequenceManager, we want to suppress internal scene triggers
        // to avoid conflicts with the Master Sequence flow.
        private bool _suppressSceneTransitions = false;
        
        // Safety timeout to prevent getting stuck on black screen
        // private float _rideSafetyTimeout = 30f; // Moved to CharacterAction

        public CharacterAction CurrentAction => (_currentActionIndex >= 0 && _currentActionIndex < _actions.Count) ? _actions[_currentActionIndex] : null;

        private bool _isInitialized = false;

        private void Start()
        {
            if (_isInitialized) return;
            Initialize();
        }
        
        // Global flag to prevent local auto-starts when a Master Sequence is controlling the game flow
        public static bool IsMasterSequenceActive = false;

        private void Initialize()
        {
            _isInitialized = true;
            _suppressSceneTransitions = false;
            InitializeFadeOverlay();
            RebuildBindingCache();

            // Cache CameraController
            if (_cameraController == null)
            {
                _cameraController = FindObjectOfType<CameraController>();
            }

            // Load sequence from SO if assigned
            if (_sequenceAsset != null)
            {
                LoadSequenceFromAsset();
            }

            // Auto-configuration for MVP
            if (_targetCharacter == null)
            {
                var follower = FindObjectOfType<CharacterPathFollower>();
                if (follower != null)
                {
                    Debug.Log($"[CharacterActionManager] Auto-assigned target character: {follower.name}");
                    _targetCharacter = follower;
                }
            }

            if (_targetCharacter == null)
            {
                Debug.LogError("[CharacterActionManager] No target character assigned or found! Disabling.");
                enabled = false;
                return;
            }

            // Take control of the character
            _targetCharacter.AutoStartFollowing = false;

            // Auto-generate default sequence if empty
            if (_actions.Count == 0)
            {
                GenerateDefaultSequence();
            }

            // ONLY auto-start if we are NOT in Master Sequence mode
            if (!IsMasterSequenceActive && _autoStart && _actions.Count > 0)
            {
                PlayAction(0);
            }
            else if (IsMasterSequenceActive)
            {
                Debug.Log("[CharacterActionManager] Master Sequence is active. Holding off auto-start.");
            }
        }


        private void InitializeFadeOverlay()
        {
            if (fadeUIDocument == null)
            {
                // Try to find one in the scene if not assigned
                fadeUIDocument = FindObjectOfType<UIDocument>();
            }

            if (fadeUIDocument != null)
            {
                VisualElement root = fadeUIDocument.rootVisualElement;
                _fadeOverlay = root.Q<VisualElement>(fadeOverlayName);
                
                // Fallback: try finding by class name if ID fails
                if (_fadeOverlay == null)
                {
                    _fadeOverlay = root.Q<VisualElement>(className: fadeOverlayName);
                }
                
                if (_fadeOverlay != null)
                {
                    // Ensure it starts invisible only if we are just starting up
                    if (Time.time < 1f)
                    {
                        _fadeOverlay.style.opacity = 0f;
                        _fadeOverlay.style.display = DisplayStyle.None;
                    }
                }
                else
                {
                    Debug.LogWarning($"[CharacterActionManager] Fade overlay '{fadeOverlayName}' not found in UIDocument.");
                }
            }
        }

        [ContextMenu("Reset Actions to Default")]
        public void ResetActionsToDefault()
        {
            _actions.Clear();
            _sequenceAsset = null; // Clear asset reference to allow local generation
            _suppressSceneTransitions = false;
            GenerateDefaultSequence();
            Debug.Log("[CharacterActionManager] Actions reset to default sequence.");
        }

        public void PlaySequence(CharacterActionSequence sequence)
        {
            if (sequence == null) 
            {
                Debug.LogError("[CharacterActionManager] Received null sequence!");
                return;
            }

            if (_sequenceAsset == sequence && _isPlaying)
            {
                Debug.Log($"[CharacterActionManager] Sequence '{sequence.name}' is already active. Ignoring replay request.");
                return;
            }
            
            // Ensure initialization happens first (dependencies like CharacterPathFollower)
            if (!_isInitialized)
            {
                // We call Initialize manually, but we disable autoStart to avoid conflict
                _autoStart = false; 
                Initialize();
            }
            
            Debug.Log($"[CharacterActionManager] PlaySequence called with: {sequence.name}");
            
            // When externally controlled, suppress internal transitions
            _suppressSceneTransitions = true;
            
            _sequenceAsset = sequence;

            // Force assign binding for "MainPath" if it exists in the scene but bindings are empty
            // This is a common MVP fix for when bindings aren't set up in the Inspector for every scene
            if (_sceneBindings.Count == 0)
            {
                 var path = FindObjectOfType<Features.World.CustomPath>();
                 if (path != null)
                 {
                     _sceneBindings.Add(new SceneBinding { key = "MainPath", target = path.gameObject });
                     Debug.Log("[CharacterActionManager] Auto-bound 'MainPath' to found CustomPath.");
                 }
            }
            
            RebuildBindingCache();
            LoadSequenceFromAsset();

            if (_actions.Count > 0)
            {
                PlayAction(0);
            }
            else
            {
                Debug.LogWarning("[CharacterActionManager] Sequence asset was empty.");
                OnSequenceFinished?.Invoke();
            }
        }

        private void LoadSequenceFromAsset()
        {
            if (_sequenceAsset == null) return;
            
            _actions.Clear();
            _loopActions = _sequenceAsset.loop;
            _requireManualTrigger = _sequenceAsset.requireManualTrigger;
            
            Debug.Log($"[CharacterActionManager] Loading sequence from asset: {_sequenceAsset.name}");
            
            foreach (var def in _sequenceAsset.actions)
            {
                var action = new CharacterAction();
                action.name = def.actionName;
                action.actionType = (ActionType)def.actionType; // Cast between same-named enums
                action.cameraMode = def.cameraMode;
                action.fadeOut = def.fadeOut;
                action.fadeIn = def.fadeIn;
                action.fadeDuration = def.fadeDuration;
                action.duration = def.duration;
                action.startProgress = def.startProgress;
                action.finishProgress = def.finishProgress;
                action.loop = def.loop;
                action.onCompleteCommand = def.onCompleteCommand;
                
                // Resolve bindings
                if (!string.IsNullOrEmpty(def.targetKey))
                {
                    GameObject target = FindBinding(def.targetKey);
                    if (target != null)
                    {
                        if (action.actionType == ActionType.Ride)
                        {
                            action.pathTarget = target;
                        }
                        else if (action.actionType == ActionType.Teleport)
                        {
                            action.teleportTarget = target.transform;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterActionManager] Could not find binding for key '{def.targetKey}' in action '{def.actionName}'");
                    }
                }
                
                _actions.Add(action);
            }
        }
        
        private GameObject FindBinding(string key)
        {
            if (_bindingCache == null) RebuildBindingCache();
            return _bindingCache.TryGetValue(key, out var target) ? target : null;
        }

        private void GenerateDefaultSequence()
        {
            var path = FindObjectOfType<Features.World.CustomPath>();
            if (path != null)
            {
                _actions.Add(new CharacterAction 
                { 
                    name = "Ride Sequence", 
                    actionType = ActionType.Ride, 
                    pathTarget = path.gameObject,
                    fadeOut = true,
                    fadeIn = true,
                    startProgress = 0f,
                    finishProgress = 1f
                });
            }
        }

        private void OnDisable()
        {
            if (_targetCharacter != null)
            {
                _targetCharacter.OnPathComplete.RemoveListener(OnRideComplete);
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (_targetCharacter != null)
            {
                _targetCharacter.OnPathComplete.RemoveListener(OnRideComplete);
            }
        }

        public void PlayAction(int index)
        {
            if (index < 0 || index >= _actions.Count)
            {
                if (_loopActions && _actions.Count > 0)
                {
                    PlayAction(0);
                }
                else
                {
                    StopSequence();
                    Debug.Log("[CharacterActionManager] Sequence finished.");
                    OnSequenceFinished?.Invoke();
                }
                return;
            }

            // Optimization: If requesting the same action index that is currently playing, treat it as a continuation
            if (_isPlaying && _currentActionIndex == index)
            {
                Debug.Log($"[CharacterActionManager] Already playing action {index} ({_actions[index].name}). Continuing...");
                return;
            }

            if (_currentActionCoroutine != null)
            {
                StopCoroutine(_currentActionCoroutine);
            }
            
            if (_targetCharacter != null)
            {
                _targetCharacter.OnPathComplete.RemoveListener(OnRideComplete);
            }

            _currentActionIndex = index;
            _isPlaying = true;
            CharacterAction action = _actions[index];
            _currentActionName = action.name;
            
            // Start the action sequence (possibly with fade)
            _currentActionCoroutine = StartCoroutine(ExecuteActionSequence(action));
        }

        private IEnumerator ExecuteActionSequence(CharacterAction action)
        {
            // 1. Fade Out (Transition Start)
            // If enabled, we go to black BEFORE the action state changes.
            if (action.fadeOut)
            {
                Debug.Log($"[CharacterActionManager] Action '{action.name}' Transition: Fading Out...");
                yield return FadeOut(action.fadeDuration);
            }

            // 2. Start Action Logic
            // This sets up the character (teleport, start animation, start path).
            // The screen might be black here if fadeOut was true.
            Debug.Log($"[CharacterActionManager] Starting action {_currentActionIndex}: {action.name} ({action.actionType})");
            
            // Apply camera mode
            // Force apply camera mode from action definition as per request.
            if (_cameraController == null) _cameraController = FindObjectOfType<CameraController>(); // Late binding check

            if (_cameraController != null)
            {
                Debug.Log($"[CharacterActionManager] Setting Camera Mode to: {action.cameraMode}");
                _cameraController.Mode = action.cameraMode;
            }
            else
            {
                Debug.LogWarning("[CharacterActionManager] CameraController not found!");
            }
            
            action.onStart?.Invoke();

            switch (action.actionType)
            {
                case ActionType.Idle:
                    StartCoroutine(DoIdle(action));
                    break;
                case ActionType.Ride:
                    DoRide(action);
                    break;
                case ActionType.Teleport:
                    DoTeleport(action);
                    break;
            }

            // 3. Fade In (Transition End)
            // If enabled, we reveal the scene NOW, while the action is running.
            // This allows the action (like Riding) to be visible.
            if (action.fadeIn)
            {
                Debug.Log($"[CharacterActionManager] Action '{action.name}' Transition: Fading In...");
                yield return FadeIn(action.fadeDuration);
            }

            // For synchronous actions (like Teleport) or skipped actions, ensure completion is triggered
            if (action.actionType == ActionType.Teleport)
            {
                CompleteAction(action);
            }
        }

        private IEnumerator FadeOut(float duration)
        {
            if (_fadeOverlay == null) InitializeFadeOverlay();
            if (_fadeOverlay == null) 
            {
                Debug.LogError("[CharacterActionManager] Cannot Fade Out: Overlay not found!");
                yield break;
            }
            
            _fadeOverlay.style.display = DisplayStyle.Flex;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.style.opacity = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            _fadeOverlay.style.opacity = 1f;
        }

        private IEnumerator FadeIn(float duration)
        {
            if (_fadeOverlay == null) InitializeFadeOverlay();
            if (_fadeOverlay == null) 
            {
                Debug.LogError($"[CharacterActionManager] Cannot Fade In: Overlay '{fadeOverlayName}' not found in UIDocument '{fadeUIDocument?.name}'!");
                yield break;
            }
            
            Debug.Log($"[CharacterActionManager] Fading In ({duration}s)...");
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.style.opacity = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            _fadeOverlay.style.opacity = 0f;
            _fadeOverlay.style.display = DisplayStyle.None;
            Debug.Log($"[CharacterActionManager] Fade In Complete.");
        }

        private IEnumerator DoIdle(CharacterAction action)
        {
            if (_targetCharacter.IsFollowing)
            {
                _targetCharacter.StopFollowing();
            }

            yield return new WaitForSeconds(action.duration);
            
            CompleteAction(action);
        }

        private void DoRide(CharacterAction action)
        {
            if (action.pathTarget == null)
            {
                Debug.LogWarning($"[CharacterActionManager] No path target for Ride action '{action.name}'");
                CompleteAction(action);
                return;
            }

            IPathProvider provider = action.pathTarget.GetComponent<IPathProvider>();
            if (provider == null)
            {
                Debug.LogError($"[CharacterActionManager] Target object '{action.pathTarget.name}' does not implement IPathProvider!");
                CompleteAction(action);
                return;
            }

            Debug.Log($"[CharacterActionManager] Starting Ride on '{action.pathTarget.name}' from {action.startProgress} to {action.finishProgress}");

            _targetCharacter.OnPathComplete.AddListener(OnRideComplete);

            _targetCharacter.SetPathProvider(provider);
            _targetCharacter.StartingProgress = action.startProgress;
            _targetCharacter.FinishProgress = action.finishProgress;
            
            // Validate speed and length
            if (_targetCharacter.Speed <= 0.001f)
            {
                Debug.LogWarning("[CharacterActionManager] Character Speed is 0! It will never finish. Setting to 5 temporarily.");
                _targetCharacter.Speed = 5f;
            }
            
            float pathLength = provider.TotalPathLength;
            float dist = Mathf.Abs(action.finishProgress - action.startProgress) * pathLength;
            float estimatedTime = dist / _targetCharacter.Speed;
            Debug.Log($"[CharacterActionManager] Ride Distance: {dist:F1} units. Speed: {_targetCharacter.Speed}. Estimated Time: {estimatedTime:F1}s.");

            // Validate progress
            if (Mathf.Abs(action.startProgress - action.finishProgress) < 0.001f)
            {
                 Debug.LogWarning($"[CharacterActionManager] Action '{action.name}' has ~0 distance. Completing immediately.");
                 CompleteAction(action);
                 return;
            }

            if (action.startProgress > action.finishProgress)
            {
                 Debug.LogWarning($"[CharacterActionManager] StartProgress ({action.startProgress}) > FinishProgress ({action.finishProgress}).");
            }

            _targetCharacter.ResetProgress();
            
            if (action.loop)
            {
                _targetCharacter.EndMode = CharacterPathFollower.EndBehavior.Loop;
            }
            else
            {
                _targetCharacter.EndMode = CharacterPathFollower.EndBehavior.Stop;
            }
            
            _targetCharacter.StartFollowing();

            // Safety check: if the follower didn't start (e.g. invalid path), complete immediately
            if (!_targetCharacter.IsFollowing)
            {
                Debug.LogWarning($"[CharacterActionManager] Ride action '{action.name}' failed to start (likely invalid path or start >= finish). Skipping.");
                _targetCharacter.OnPathComplete.RemoveListener(OnRideComplete);
                CompleteAction(action);
            }
        }

        private void OnRideComplete()
        {
            Debug.Log("[CharacterActionManager] Ride Complete Event Received.");
            
            _targetCharacter.OnPathComplete.RemoveListener(OnRideComplete);
            
            CharacterAction action = CurrentAction;
            if (action != null && action.actionType == ActionType.Ride)
            {
                CompleteAction(action);
            }
        }

        private void DoTeleport(CharacterAction action)
        {
            if (action.teleportTarget == null)
            {
                Debug.LogWarning($"[CharacterActionManager] No teleport target for Teleport action '{action.name}'");
                CompleteAction(action);
                return;
            }

            if (_targetCharacter.IsFollowing)
            {
                _targetCharacter.StopFollowing();
            }

            // Check if already at position (avoid redundant snaps)
            float dist = Vector3.Distance(_targetCharacter.transform.position, action.teleportTarget.position);
            float angle = Quaternion.Angle(_targetCharacter.transform.rotation, action.teleportTarget.rotation);

            if (dist < 0.1f && angle < 5f)
            {
                Debug.Log($"[CharacterActionManager] Character already at '{action.teleportTarget.name}'. Skipping teleport.");
                return;
            }

            Debug.Log($"[CharacterActionManager] Teleporting to '{action.teleportTarget.name}'");
            _targetCharacter.transform.position = action.teleportTarget.position;
            _targetCharacter.transform.rotation = action.teleportTarget.rotation;

            // Snap camera if available
            if (_cameraController != null)
            {
                _cameraController.SnapToPosition();
            }
        }

        private void CompleteAction(CharacterAction action)
        {
            // Action is done. Trigger next.
            // Note: We don't do fade logic here anymore, as fades are now "Transition Into Action".
            // The Fade Out for the *next* action will handle the exit transition.
            Debug.Log($"[CharacterActionManager] Action '{action.name}' Completed.");
            
            // Invoke Unity Events
            action.onComplete?.Invoke();
            
            // Process command string (from SO)
            if (!string.IsNullOrEmpty(action.onCompleteCommand))
            {
                if (_suppressSceneTransitions)
                {
                    Debug.Log($"[CharacterActionManager] Suppressing scene transition '{action.onCompleteCommand}' because sequence is externally driven.");
                }
                else
                {
                    TriggerSceneTransition(action.onCompleteCommand);
                }
            }
            
            if (_requireManualTrigger)
            {
                Debug.Log("[CharacterActionManager] Waiting for manual trigger to proceed...");
                return;
            }
            
            PlayAction(_currentActionIndex + 1);
        }
        
        [ContextMenu("Trigger Next Action")]
        public void TriggerNextAction()
        {
            if (!_isPlaying) return;
            Debug.Log("[CharacterActionManager] Manual trigger received.");
            PlayAction(_currentActionIndex + 1);
        }

        public void StopSequence()
        {
            if (_currentActionCoroutine != null) 
            {
                StopCoroutine(_currentActionCoroutine);
                _currentActionCoroutine = null;
            }
            
            if (_targetCharacter != null)
            {
                _targetCharacter.StopFollowing();
                _targetCharacter.OnPathComplete.RemoveListener(OnRideComplete);
            }
            
            _isPlaying = false;
            _currentActionName = "Stopped";
        }

        /// <summary>
        /// Helper to trigger a scene transition command via AnimatedSceneController.
        /// Can be called from CharacterAction.onComplete events in the Inspector.
        /// </summary>
        /// <param name="command">Command string (e.g. "SceneA_to_SceneB" or "SceneName")</param>
        public void TriggerSceneTransition(string command)
        {
            var controller = FindObjectOfType<AnimatedSceneController>();
            if (controller != null)
            {
                Debug.Log($"[CharacterActionManager] Triggering transition command: {command}");
                controller.ProcessTransitionCommand(command);
            }
            else
            {
                Debug.LogError("[CharacterActionManager] Cannot trigger transition: AnimatedSceneController not found!");
            }
        }
    }
}
