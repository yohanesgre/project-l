using UnityEngine;

namespace Runtime.WorldBuilding
{
    /// <summary>
    /// A character controller that follows a road path defined by a RoadGenerator.
    /// Supports loop and ping-pong movement modes with configurable speed and rotation.
    /// </summary>
    public class RoadPathFollower : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// Defines the behavior when the follower reaches the end of the path.
        /// </summary>
        public enum EndBehavior
        {
            /// <summary>Stop at the end of the path.</summary>
            Stop,
            /// <summary>Loop back to the start when reaching the end.</summary>
            Loop,
            /// <summary>Reverse direction at each end of the path.</summary>
            PingPong
        }

        /// <summary>
        /// Defines how the follower rotates to match the path direction.
        /// </summary>
        public enum RotationMode
        {
            /// <summary>Full 3D rotation matching path forward, right, and up vectors.</summary>
            Full3D,
            /// <summary>Only rotate around Y axis (keeps character upright).</summary>
            YAxisOnly,
            /// <summary>No rotation applied.</summary>
            None
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Path Reference")]
        [Tooltip("The RoadGenerator that defines the path to follow.")]
        [SerializeField] private RoadGenerator _roadGenerator;
        
        [Header("Movement Settings")]
        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float _speed = 5f;
        
        [Tooltip("Current progress along the path (0 = start, 1 = end).")]
        [Range(0f, 1f)]
        [SerializeField] private float _progress = 0f;
        
        [Tooltip("Behavior when reaching the end of the path.")]
        [SerializeField] private EndBehavior _endBehavior = EndBehavior.Loop;
        
        [Tooltip("Whether the follower is currently moving.")]
        [SerializeField] private bool _isFollowing = false;
        
        [Tooltip("Automatically start following when the game starts.")]
        [SerializeField] private bool _autoStartFollowing = false;
        
        [Header("Rotation Settings")]
        [Tooltip("How the follower rotates to face the path direction.")]
        [SerializeField] private RotationMode _rotationMode = RotationMode.YAxisOnly;
        
        [Tooltip("How quickly the follower rotates to face the path direction.")]
        [SerializeField] private float _rotationSpeed = 10f;
        
        [Header("Offset Settings")]
        [Tooltip("Vertical offset from the path surface.")]
        [SerializeField] private float _heightOffset = 0f;
        
        [Tooltip("Lateral offset from the path center (positive = right, negative = left).")]
        [SerializeField] private float _lateralOffset = 0f;
        
        #endregion

        #region Private Fields
        
        private Transform _transform;
        private float _pathLength;
        private int _direction = 1; // 1 = forward, -1 = backward (for ping-pong)
        private bool _hasValidPath;
        private bool _pendingAutoStart;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The RoadGenerator that defines the path to follow.
        /// </summary>
        public RoadGenerator RoadGenerator
        {
            get => _roadGenerator;
            set => _roadGenerator = value;
        }
        
        /// <summary>
        /// Movement speed in units per second.
        /// </summary>
        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// Current progress along the path (0 = start, 1 = end).
        /// </summary>
        public float Progress
        {
            get => _progress;
            set => _progress = Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Whether the follower is currently moving.
        /// </summary>
        public bool IsFollowing => _isFollowing;
        
        /// <summary>
        /// Whether to automatically start following when the game starts.
        /// </summary>
        public bool AutoStartFollowing
        {
            get => _autoStartFollowing;
            set => _autoStartFollowing = value;
        }
        
        /// <summary>
        /// Current movement direction (1 = forward, -1 = backward).
        /// Only relevant for PingPong mode.
        /// </summary>
        public int Direction => _direction;
        
        /// <summary>
        /// Behavior when reaching the end of the path.
        /// </summary>
        public EndBehavior EndMode
        {
            get => _endBehavior;
            set => _endBehavior = value;
        }
        
        /// <summary>
        /// How the follower rotates to face the path direction.
        /// </summary>
        public RotationMode Rotation
        {
            get => _rotationMode;
            set => _rotationMode = value;
        }
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _transform = transform;
        }
        
        private void Start()
        {
            CachePathLength();
            ApplyPositionImmediate();
            
            if (_autoStartFollowing)
            {
                // Try to start immediately, if path not ready yet, mark as pending
                if (_hasValidPath)
                {
                    StartFollowing();
                }
                else
                {
                    _pendingAutoStart = true;
                }
            }
        }
        
        private void Update()
        {
            // Handle pending auto-start (wait for RoadGenerator to initialize)
            if (_pendingAutoStart && !_isFollowing)
            {
                CachePathLength();
                if (_hasValidPath)
                {
                    _pendingAutoStart = false;
                    StartFollowing();
                }
            }
            
            if (_isFollowing)
            {
                UpdateMovement();
            }
        }
        
        private void OnValidate()
        {
            _speed = Mathf.Max(0f, _speed);
            _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
            
            // In editor, update position when progress slider changes
            if (!Application.isPlaying && _roadGenerator != null)
            {
                CachePathLength();
                ApplyPositionImmediate();
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Starts following the path from the current progress position.
        /// </summary>
        public void StartFollowing()
        {
            if (_roadGenerator == null)
            {
                Debug.LogWarning("RoadPathFollower: Cannot start following - no RoadGenerator assigned.", this);
                return;
            }
            
            CachePathLength();
            
            if (!_hasValidPath)
            {
                Debug.LogWarning("RoadPathFollower: Cannot start following - road has no valid path.", this);
                return;
            }
            
            _isFollowing = true;
        }
        
        /// <summary>
        /// Stops following the path.
        /// </summary>
        public void StopFollowing()
        {
            _isFollowing = false;
        }
        
        /// <summary>
        /// Toggles the following state.
        /// </summary>
        public void ToggleFollowing()
        {
            if (_isFollowing)
            {
                StopFollowing();
            }
            else
            {
                StartFollowing();
            }
        }
        
        /// <summary>
        /// Sets the progress along the path and immediately updates the position.
        /// </summary>
        /// <param name="t">Normalized progress value (0-1).</param>
        public void SetProgress(float t)
        {
            _progress = Mathf.Clamp01(t);
            ApplyPositionImmediate();
        }
        
        /// <summary>
        /// Resets progress to the start of the path.
        /// </summary>
        public void ResetProgress()
        {
            _progress = 0f;
            _direction = 1;
            ApplyPositionImmediate();
        }
        
        /// <summary>
        /// Reverses the current movement direction (for ping-pong mode).
        /// </summary>
        public void ReverseDirection()
        {
            _direction *= -1;
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Caches the total path length for speed calculations.
        /// </summary>
        private void CachePathLength()
        {
            _hasValidPath = false;
            _pathLength = 0f;
            
            if (_roadGenerator == null) return;
            
            // Get the end point to determine total path length
            var endPoint = _roadGenerator.GetPointAlongPath(1f);
            if (endPoint.HasValue)
            {
                _pathLength = endPoint.Value.DistanceAlongPath;
                _hasValidPath = _pathLength > 0f;
            }
        }
        
        /// <summary>
        /// Updates movement along the path based on speed and delta time.
        /// </summary>
        private void UpdateMovement()
        {
            if (!_hasValidPath || _pathLength <= 0f) return;
            
            // Calculate progress delta based on speed
            float progressDelta = (_speed * Time.deltaTime) / _pathLength;
            _progress += progressDelta * _direction;
            
            // Handle end of path behavior
            HandleEndOfPath();
            
            // Apply position and rotation
            ApplyPositionAndRotation();
        }
        
        /// <summary>
        /// Handles behavior when reaching the end or start of the path.
        /// </summary>
        private void HandleEndOfPath()
        {
            switch (_endBehavior)
            {
                case EndBehavior.Stop:
                    if (_progress >= 1f)
                    {
                        _progress = 1f;
                        _isFollowing = false;
                    }
                    else if (_progress <= 0f)
                    {
                        _progress = 0f;
                        _isFollowing = false;
                    }
                    break;
                    
                case EndBehavior.Loop:
                    if (_progress >= 1f)
                    {
                        _progress -= 1f;
                    }
                    else if (_progress <= 0f)
                    {
                        _progress += 1f;
                    }
                    break;
                    
                case EndBehavior.PingPong:
                    if (_progress >= 1f)
                    {
                        _progress = 1f - (_progress - 1f);
                        _direction = -1;
                    }
                    else if (_progress <= 0f)
                    {
                        _progress = -_progress;
                        _direction = 1;
                    }
                    break;
            }
            
            _progress = Mathf.Clamp01(_progress);
        }
        
        /// <summary>
        /// Applies position and smooth rotation based on current progress.
        /// </summary>
        private void ApplyPositionAndRotation()
        {
            if (_roadGenerator == null) return;
            
            var point = _roadGenerator.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var roadPoint = point.Value;
            
            // Calculate target position with offsets
            Vector3 targetPosition = roadPoint.Position;
            targetPosition += roadPoint.Up * _heightOffset;
            targetPosition += roadPoint.Right * _lateralOffset;
            
            _transform.position = targetPosition;
            
            // Apply rotation based on mode
            ApplyRotation(roadPoint);
        }
        
        /// <summary>
        /// Applies position immediately without smoothing (for editor or SetProgress calls).
        /// </summary>
        private void ApplyPositionImmediate()
        {
            if (_roadGenerator == null) return;
            
            // Ensure we have a cached transform
            if (_transform == null)
            {
                _transform = transform;
            }
            
            var point = _roadGenerator.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var roadPoint = point.Value;
            
            // Calculate target position with offsets
            Vector3 targetPosition = roadPoint.Position;
            targetPosition += roadPoint.Up * _heightOffset;
            targetPosition += roadPoint.Right * _lateralOffset;
            
            _transform.position = targetPosition;
            
            // Apply rotation immediately
            ApplyRotationImmediate(roadPoint);
        }
        
        /// <summary>
        /// Applies smooth rotation towards the target direction.
        /// </summary>
        private void ApplyRotation(RoadMeshBuilder.RoadPoint roadPoint)
        {
            if (_rotationMode == RotationMode.None) return;
            
            Quaternion targetRotation = CalculateTargetRotation(roadPoint);
            
            if (_rotationSpeed > 0f)
            {
                _transform.rotation = Quaternion.Slerp(
                    _transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
            else
            {
                _transform.rotation = targetRotation;
            }
        }
        
        /// <summary>
        /// Applies rotation immediately without smoothing.
        /// </summary>
        private void ApplyRotationImmediate(RoadMeshBuilder.RoadPoint roadPoint)
        {
            if (_rotationMode == RotationMode.None) return;
            
            _transform.rotation = CalculateTargetRotation(roadPoint);
        }
        
        /// <summary>
        /// Calculates the target rotation based on the current rotation mode.
        /// </summary>
        private Quaternion CalculateTargetRotation(RoadMeshBuilder.RoadPoint roadPoint)
        {
            Vector3 forward = roadPoint.Forward;
            
            // Flip direction if moving backwards
            if (_direction < 0)
            {
                forward = -forward;
            }
            
            switch (_rotationMode)
            {
                case RotationMode.Full3D:
                    return Quaternion.LookRotation(forward, roadPoint.Up);
                    
                case RotationMode.YAxisOnly:
                    // Project forward onto horizontal plane
                    Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
                    if (flatForward.sqrMagnitude > 0.001f)
                    {
                        return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
                    }
                    return _transform.rotation;
                    
                default:
                    return _transform.rotation;
            }
        }
        
        #endregion

        #region Editor Gizmos
        
#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _gizmoColor = Color.yellow;
        [SerializeField] private float _gizmoSize = 0.5f;
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            if (_roadGenerator == null) return;
            
            var point = _roadGenerator.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var roadPoint = point.Value;
            
            // Calculate position with offsets
            Vector3 position = roadPoint.Position;
            position += roadPoint.Up * _heightOffset;
            position += roadPoint.Right * _lateralOffset;
            
            // Draw sphere at current position
            // Gizmos.color = _gizmoColor;
            // Gizmos.DrawWireSphere(position, _gizmoSize);
            
            // Draw direction arrow
            Vector3 forward = _direction > 0 ? roadPoint.Forward : -roadPoint.Forward;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, forward * _gizmoSize * 2f);
            
            // Draw right vector
            Gizmos.color = Color.red;
            Gizmos.DrawRay(position, roadPoint.Right * _gizmoSize);
            
            // Draw up vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, roadPoint.Up * _gizmoSize);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;
            if (_roadGenerator == null) return;
            
            // Draw a larger visualization when selected
            var point = _roadGenerator.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var roadPoint = point.Value;
            
            Vector3 position = roadPoint.Position;
            position += roadPoint.Up * _heightOffset;
            position += roadPoint.Right * _lateralOffset;
            
            // Draw connection line to road surface
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.5f);
            Gizmos.DrawLine(position, roadPoint.Position);
            
            // Draw progress text using Handles
            UnityEditor.Handles.Label(
                position + Vector3.up * (_gizmoSize + 0.5f),
                $"Progress: {_progress:P0}",
                new GUIStyle { normal = { textColor = Color.white }, fontSize = 12 }
            );
        }
#endif
        
        #endregion
    }
}
