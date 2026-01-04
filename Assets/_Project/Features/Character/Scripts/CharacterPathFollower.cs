using UnityEngine;
using MyGame.Core;

namespace MyGame.Features.Character
{
    /// <summary>
    /// A path follower that works with any IPathProvider (CustomPath, RoadGenerator, etc.).
    /// Designed for character movement with support for various movement modes and offsets.
    /// </summary>
    public class CharacterPathFollower : MonoBehaviour, IPathFollower
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
        [Tooltip("The path provider component. Can be a CustomPath, RoadGenerator, or any IPathProvider.")]
        [SerializeField] private Component _pathProviderComponent;
        
        [Header("Movement Settings")]
        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float _speed = 5f;
        
        [Tooltip("Starting progress along the path (0 = start, 1 = end).")]
        [Range(0f, 1f)]
        [SerializeField] private float _startingProgress = 0f;
        
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
        
        [Header("Smoothing Settings")]
        [Tooltip("Smooth time for position interpolation. Set to 0 for instant movement.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _positionSmoothTime = 0.05f;
        
        #endregion

        #region Private Fields
        
        private Transform _transform;
        private IPathProvider _pathProvider;
        private float _pathLength;
        private int _direction = 1;
        private bool _hasValidPath;
        private bool _pendingAutoStart;
        
        // Smoothing state
        private Vector3 _currentVelocity;
        private Vector3 _smoothedPosition;
        private bool _needsPositionSnap;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The path provider component.
        /// </summary>
        public IPathProvider PathProvider
        {
            get => _pathProvider;
            set
            {
                _pathProvider = value;
                _pathProviderComponent = value as Component;
                CachePathLength();
            }
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
        /// Starting progress along the path.
        /// </summary>
        public float StartingProgress
        {
            get => _startingProgress;
            set => _startingProgress = Mathf.Clamp01(value);
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
            ResolvePathProvider();
        }
        
        private void Start()
        {
            _progress = _startingProgress;
            
            CachePathLength();
            ApplyPositionImmediate();
            
            _smoothedPosition = _transform.position;
            _currentVelocity = Vector3.zero;
            
            if (_autoStartFollowing)
            {
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
            
            ResolvePathProvider();
            
            if (!Application.isPlaying && _pathProvider != null)
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
            ResolvePathProvider();
            
            if (_pathProvider == null)
            {
                Debug.LogWarning("CharacterPathFollower: Cannot start following - no path provider assigned.", this);
                return;
            }
            
            CachePathLength();
            
            if (!_hasValidPath)
            {
                Debug.LogWarning("CharacterPathFollower: Cannot start following - path has no valid data.", this);
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
        public void SetProgress(float t)
        {
            _progress = Mathf.Clamp01(t);
            ApplyPositionImmediate();
        }
        
        /// <summary>
        /// Resets progress to the starting progress value.
        /// </summary>
        public void ResetProgress()
        {
            _progress = _startingProgress;
            _direction = 1;
            ApplyPositionImmediate();
        }
        
        /// <summary>
        /// Reverses the current movement direction.
        /// </summary>
        public void ReverseDirection()
        {
            _direction *= -1;
        }
        
        /// <summary>
        /// Sets a new path provider at runtime.
        /// </summary>
        public void SetPathProvider(IPathProvider provider)
        {
            _pathProvider = provider;
            _pathProviderComponent = provider as Component;
            CachePathLength();
            ApplyPositionImmediate();
        }
        
        #endregion

        #region Private Methods
        
        private void ResolvePathProvider()
        {
            if (_pathProviderComponent != null)
            {
                _pathProvider = _pathProviderComponent as IPathProvider;
                
                if (_pathProvider == null)
                {
                    Debug.LogWarning($"CharacterPathFollower: Assigned component '{_pathProviderComponent.GetType().Name}' does not implement IPathProvider.", this);
                }
            }
            else
            {
                _pathProvider = null;
            }
        }
        
        private void CachePathLength()
        {
            _hasValidPath = false;
            _pathLength = 0f;
            
            if (_pathProvider == null) return;
            
            _pathLength = _pathProvider.TotalPathLength;
            _hasValidPath = _pathProvider.HasValidPath && _pathLength > 0f;
        }
        
        private void UpdateMovement()
        {
            if (!_hasValidPath || _pathLength <= 0f) return;
            
            float progressDelta = (_speed * Time.deltaTime) / _pathLength;
            _progress += progressDelta * _direction;
            
            HandleEndOfPath();
            ApplyPositionAndRotation();
        }
        
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
        
        private void ApplyPositionAndRotation()
        {
            if (_pathProvider == null) return;
            
            var point = _pathProvider.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var pathPoint = point.Value;
            
            Vector3 targetPosition = pathPoint.Position;
            targetPosition += pathPoint.Up * _heightOffset;
            targetPosition += pathPoint.Right * _lateralOffset;
            
            if (_needsPositionSnap || _positionSmoothTime <= 0f)
            {
                _transform.position = targetPosition;
                _smoothedPosition = targetPosition;
                _currentVelocity = Vector3.zero;
                _needsPositionSnap = false;
            }
            else
            {
                _smoothedPosition = Vector3.SmoothDamp(
                    _smoothedPosition,
                    targetPosition,
                    ref _currentVelocity,
                    _positionSmoothTime
                );
                _transform.position = _smoothedPosition;
            }
            
            ApplyRotation(pathPoint);
        }
        
        private void ApplyPositionImmediate()
        {
            if (_pathProvider == null) return;
            
            if (_transform == null)
            {
                _transform = transform;
            }
            
            var point = _pathProvider.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var pathPoint = point.Value;
            
            Vector3 targetPosition = pathPoint.Position;
            targetPosition += pathPoint.Up * _heightOffset;
            targetPosition += pathPoint.Right * _lateralOffset;
            
            _transform.position = targetPosition;
            _smoothedPosition = targetPosition;
            _currentVelocity = Vector3.zero;
            
            ApplyRotationImmediate(pathPoint);
        }
        
        private void ApplyRotation(PathPoint pathPoint)
        {
            if (_rotationMode == RotationMode.None) return;
            
            Quaternion targetRotation = CalculateTargetRotation(pathPoint);
            
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
        
        private void ApplyRotationImmediate(PathPoint pathPoint)
        {
            if (_rotationMode == RotationMode.None) return;
            
            _transform.rotation = CalculateTargetRotation(pathPoint);
        }
        
        private Quaternion CalculateTargetRotation(PathPoint pathPoint)
        {
            Vector3 forward = pathPoint.Forward;
            
            if (_direction < 0)
            {
                forward = -forward;
            }
            
            switch (_rotationMode)
            {
                case RotationMode.Full3D:
                    return Quaternion.LookRotation(forward, pathPoint.Up);
                    
                case RotationMode.YAxisOnly:
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
        [SerializeField] private Color _gizmoColor = Color.magenta;
        [SerializeField] private float _gizmoSize = 0.5f;
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            ResolvePathProvider();
            if (_pathProvider == null) return;
            
            var point = _pathProvider.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var pathPoint = point.Value;
            
            Vector3 position = pathPoint.Position;
            position += pathPoint.Up * _heightOffset;
            position += pathPoint.Right * _lateralOffset;
            
            // Draw direction arrow
            Vector3 forward = _direction > 0 ? pathPoint.Forward : -pathPoint.Forward;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, forward * _gizmoSize * 2f);
            
            // Draw right vector
            Gizmos.color = Color.red;
            Gizmos.DrawRay(position, pathPoint.Right * _gizmoSize);
            
            // Draw up vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, pathPoint.Up * _gizmoSize);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;
            
            ResolvePathProvider();
            if (_pathProvider == null) return;
            
            var point = _pathProvider.GetPointAlongPath(_progress);
            if (!point.HasValue) return;
            
            var pathPoint = point.Value;
            
            Vector3 position = pathPoint.Position;
            position += pathPoint.Up * _heightOffset;
            position += pathPoint.Right * _lateralOffset;
            
            // Draw connection line to path surface
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.5f);
            Gizmos.DrawLine(position, pathPoint.Position);
            
            // Draw progress label
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
