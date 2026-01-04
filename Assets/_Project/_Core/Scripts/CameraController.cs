using UnityEngine;

namespace MyGame.Core
{
    /// <summary>
    /// A camera controller that follows any IPathProvider path.
    /// Designed for looping road animations with parallax backgrounds.
    /// Follows target in both edit mode and play mode.
    /// </summary>
    [ExecuteAlways]
    public class CameraController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("References")]
        [Tooltip("The path provider that defines the camera's path (CustomPath, RoadGenerator, etc.).")]
        [SerializeField] private Component _pathProviderComponent;
        
        [Tooltip("The target Transform to look at (e.g., the cyclist/character).")]
        [SerializeField] private Transform _target;
        
        [Tooltip("Optional: Reference to a path follower component on the target for synced movement.")]
        [SerializeField] private Component _targetPathFollowerComponent;
        
        [Header("Path Position Settings")]
        [Tooltip("How far ahead of the target the camera should be (in path progress, 0-1 range). E.g., 0.05 = 5% ahead on the path.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _progressOffset = 0.01f;
        
        [Tooltip("Height offset from the path surface.")]
        [SerializeField] private float _heightOffset = 2.5f;
        
        [Tooltip("Lateral offset from path center (positive = right, negative = left).")]
        [SerializeField] private float _lateralOffset = -7.5f;
        
        [Header("Look At Settings")]
        [Tooltip("Height offset for the look-at point on the target.")]
        [SerializeField] private float _lookAtHeightOffset = 1.5f;
        
        [Tooltip("How quickly the camera rotates to face the target.")]
        [SerializeField] private float _rotationSmoothSpeed = 25f;
        
        [Tooltip("Horizontal screen offset for framing. Positive = target appears on left, Negative = target on right.")]
        [Range(-0.5f, 0.5f)]
        [SerializeField] private float _framingOffsetX = 0f;
        
        [Tooltip("Vertical screen offset for framing. Positive = target appears lower, Negative = target higher.")]
        [Range(-0.5f, 0.5f)]
        [SerializeField] private float _framingOffsetY = 0f;
        
        [Header("Downhill Illusion (Camera Tilt)")]
        [Tooltip("Roll angle to tilt the camera, creating a downhill illusion. Positive = tilt right, Negative = tilt left.")]
        [Range(-45f, 45f)]
        [SerializeField] private float _rollAngle = -2.5f;
        
        [Tooltip("Additional pitch adjustment. Positive = look more down, Negative = look more up.")]
        [Range(-30f, 30f)]
        [SerializeField] private float _pitchOffset = 0f;
        
        [Header("Smoothing")]
        [Tooltip("Smooth time for position following. Lower = snappier.")]
        [Range(0f, 1f)]
        [SerializeField] private float _positionSmoothTime = 0.25f;
        
        [Tooltip("Snap camera to position immediately on start.")]
        [SerializeField] private bool _snapOnStart = true;
        
        [Header("Manual Control (if no target follower)")]
        [Tooltip("Current progress along the path (0 = start, 1 = end). Used when no target follower is assigned.")]
        [Range(0f, 1f)]
        [SerializeField] private float _manualProgress = 0f;
        
        #endregion

        #region Private Fields
        
        private Transform _transform;
        private IPathProvider _pathProvider;
        private IPathFollower _targetPathFollower;
        private Vector3 _currentVelocity;
        private bool _isInitialized;
        private bool _hasWarnedAboutInvalidPathFollower;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The path provider for the camera.
        /// </summary>
        public IPathProvider PathProvider
        {
            get => _pathProvider;
            set
            {
                _pathProvider = value;
                _pathProviderComponent = value as Component;
            }
        }
        
        /// <summary>
        /// The target Transform to look at.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }
        
        /// <summary>
        /// Reference to the target's path follower for synced movement.
        /// </summary>
        public IPathFollower TargetPathFollower
        {
            get => _targetPathFollower;
            set
            {
                _targetPathFollower = value;
                _targetPathFollowerComponent = value as Component;
            }
        }
        
        /// <summary>
        /// How far ahead of the target the camera is positioned (in path progress).
        /// </summary>
        public float ProgressOffset
        {
            get => _progressOffset;
            set => _progressOffset = Mathf.Clamp(value, 0f, 0.5f);
        }
        
        /// <summary>
        /// Height offset from the path surface.
        /// </summary>
        public float HeightOffset
        {
            get => _heightOffset;
            set => _heightOffset = value;
        }
        
        /// <summary>
        /// Current camera progress along the path.
        /// </summary>
        public float CurrentProgress { get; private set; }
        
        /// <summary>
        /// Roll angle for downhill illusion effect.
        /// </summary>
        public float RollAngle
        {
            get => _rollAngle;
            set => _rollAngle = Mathf.Clamp(value, -45f, 45f);
        }
        
        /// <summary>
        /// Pitch offset for looking more up or down.
        /// </summary>
        public float PitchOffset
        {
            get => _pitchOffset;
            set => _pitchOffset = Mathf.Clamp(value, -30f, 30f);
        }
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _transform = transform;
            ResolvePathProvider();
            ResolveTargetPathFollower();
        }
        
        private void OnEnable()
        {
            // Ensure transform is cached in edit mode
            if (_transform == null)
            {
                _transform = transform;
            }
            ResolvePathProvider();
            ResolveTargetPathFollower();
        }
        
        private void Start()
        {
            // Try to auto-find path follower on target if not assigned
            if (_targetPathFollower == null && _target != null)
            {
                _targetPathFollower = _target.GetComponent<IPathFollower>();
                _targetPathFollowerComponent = _targetPathFollower as Component;
            }
            
            // Try to get path provider from target's path follower if not assigned
            if (_pathProvider == null && _targetPathFollower != null)
            {
                _pathProvider = _targetPathFollower.PathProvider;
                _pathProviderComponent = _pathProvider as Component;
            }
            
            if (_snapOnStart)
            {
                SnapToPosition();
            }
            
            _isInitialized = true;
        }
        
        private void LateUpdate()
        {
            // Ensure path provider is resolved in edit mode
            if (!Application.isPlaying)
            {
                ResolvePathProvider();
                ResolveTargetPathFollower();
            }
            
            if (_pathProvider == null || !_pathProvider.HasValidPath) return;
            
            // In edit mode, always snap (no smoothing)
            if (!Application.isPlaying)
            {
                SnapToPosition();
            }
            else
            {
                UpdateCameraPosition();
                UpdateCameraRotation();
            }
        }
        
        private void OnValidate()
        {
            _progressOffset = Mathf.Clamp(_progressOffset, 0f, 0.5f);
            _positionSmoothTime = Mathf.Max(0f, _positionSmoothTime);
            _rotationSmoothSpeed = Mathf.Max(0f, _rotationSmoothSpeed);
            _rollAngle = Mathf.Clamp(_rollAngle, -45f, 45f);
            _pitchOffset = Mathf.Clamp(_pitchOffset, -30f, 30f);
            
            // Ensure transform is cached
            if (_transform == null)
            {
                _transform = transform;
            }
            
            ResolvePathProvider();
            ResolveTargetPathFollower();
            // LateUpdate will handle snapping in edit mode
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Immediately snaps the camera to its calculated position.
        /// </summary>
        public void SnapToPosition()
        {
            if (_pathProvider == null || !_pathProvider.HasValidPath) return;
            
            if (_transform == null)
            {
                _transform = transform;
            }
            
            Vector3 targetPosition = CalculateTargetPosition();
            _transform.position = targetPosition;
            _currentVelocity = Vector3.zero;
            
            // Snap rotation too
            if (_target != null)
            {
                Vector3 lookAtPos = _target.position + Vector3.up * _lookAtHeightOffset;
                Vector3 lookDirection = lookAtPos - _transform.position;
                
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                    _transform.rotation = ApplyTiltToRotation(lookRotation);
                }
            }
        }
        
        /// <summary>
        /// Sets the manual progress value (used when no target path follower is assigned).
        /// </summary>
        /// <param name="progress">Progress value (0-1).</param>
        public void SetManualProgress(float progress)
        {
            _manualProgress = Mathf.Clamp01(progress);
        }
        
        /// <summary>
        /// Called when the target loops/teleports. Camera snaps to correct position and rotation.
        /// </summary>
        /// <param name="teleportDelta">The position delta of the teleport (unused, kept for event signature).</param>
        public void OnTargetLoopTeleport(Vector3 teleportDelta)
        {
            // Snap camera position to the correct location based on new target progress.
            Vector3 targetPosition = CalculateTargetPosition();
            _transform.position = targetPosition;
            
            // Also snap rotation to prevent jarring angle changes
            Quaternion targetRotation = CalculateTargetRotation();
            _transform.rotation = targetRotation;
            
            // Reset velocity to prevent smoothing artifacts after teleport
            _currentVelocity = Vector3.zero;
        }
        
        /// <summary>
        /// Sets a new path provider at runtime.
        /// </summary>
        public void SetPathProvider(IPathProvider provider)
        {
            _pathProvider = provider;
            _pathProviderComponent = provider as Component;
        }
        
        /// <summary>
        /// Sets the target path follower at runtime.
        /// </summary>
        public void SetTargetPathFollower(IPathFollower follower)
        {
            _targetPathFollower = follower;
            _targetPathFollowerComponent = follower as Component;
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
                    Debug.LogWarning($"CameraController: Assigned component '{_pathProviderComponent.GetType().Name}' does not implement IPathProvider.", this);
                }
            }
            else
            {
                _pathProvider = null;
            }
        }
        
        private void ResolveTargetPathFollower()
        {
            if (_targetPathFollowerComponent != null)
            {
                _targetPathFollower = _targetPathFollowerComponent as IPathFollower;
                
                if (_targetPathFollower == null)
                {
                    if (!_hasWarnedAboutInvalidPathFollower)
                    {
                        Debug.LogWarning($"CameraController: Assigned component '{_targetPathFollowerComponent.GetType().Name}' does not implement IPathFollower. Attempting to find valid component on target...", this);
                        _hasWarnedAboutInvalidPathFollower = true;
                    }
                    
                    // Try to find a valid IPathFollower on the target GameObject
                    if (_target != null)
                    {
                        var follower = _target.GetComponent<IPathFollower>();
                        if (follower != null)
                        {
                            _targetPathFollowerComponent = follower as Component;
                            _targetPathFollower = follower;
                            Debug.Log($"CameraController: Found and assigned {_targetPathFollowerComponent.GetType().Name} as target path follower.", this);
                        }
                        else
                        {
                            _targetPathFollowerComponent = null;
                        }
                    }
                    else
                    {
                        _targetPathFollowerComponent = null;
                    }
                }
            }
            else
            {
                _targetPathFollower = null;
            }
        }
        
        /// <summary>
        /// Gets the current target progress, either from the path follower or manual value.
        /// </summary>
        private float GetTargetProgress()
        {
            if (_targetPathFollower != null)
            {
                return _targetPathFollower.Progress;
            }
            return _manualProgress;
        }
        
        /// <summary>
        /// Calculates the camera's target position along the path.
        /// </summary>
        private Vector3 CalculateTargetPosition()
        {
            if (_pathProvider == null) return _transform.position;
            
            float targetProgress = GetTargetProgress();
            
            // Camera is ahead of the target (in front, looking back)
            float cameraProgress = targetProgress + _progressOffset;
            
            // Handle looping - wrap around if we go past 1.0
            if (cameraProgress > 1f)
            {
                cameraProgress -= 1f;
            }
            
            CurrentProgress = cameraProgress;
            
            // Get point on path
            var point = _pathProvider.GetPointAlongPath(cameraProgress);
            if (!point.HasValue)
            {
                return _transform.position;
            }
            
            var pathPoint = point.Value;
            
            // Calculate position with offsets
            Vector3 position = pathPoint.Position;
            position += pathPoint.Up * _heightOffset;
            position += pathPoint.Right * _lateralOffset;
            
            return position;
        }
        
        /// <summary>
        /// Updates the camera position with smooth damping.
        /// </summary>
        private void UpdateCameraPosition()
        {
            Vector3 targetPosition = CalculateTargetPosition();
            
            if (_positionSmoothTime > 0f && _isInitialized)
            {
                _transform.position = Vector3.SmoothDamp(
                    _transform.position,
                    targetPosition,
                    ref _currentVelocity,
                    _positionSmoothTime,
                    Mathf.Infinity,
                    Time.deltaTime
                );
            }
            else
            {
                _transform.position = targetPosition;
                _currentVelocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Calculates the target rotation for the camera.
        /// </summary>
        private Quaternion CalculateTargetRotation()
        {
            if (_target == null) return _transform.rotation;
            
            // Look at the target with height offset
            Vector3 lookAtPos = _target.position + Vector3.up * _lookAtHeightOffset;
            
            // Apply framing offset
            if (Mathf.Abs(_framingOffsetX) > 0.001f || Mathf.Abs(_framingOffsetY) > 0.001f)
            {
                Vector3 toTarget = (lookAtPos - _transform.position).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, toTarget).normalized;
                Vector3 up = Vector3.Cross(toTarget, right).normalized;
                
                float distance = Vector3.Distance(_transform.position, lookAtPos);
                lookAtPos -= right * (_framingOffsetX * distance);
                lookAtPos -= up * (_framingOffsetY * distance);
            }
            
            Vector3 lookDirection = lookAtPos - _transform.position;
            
            if (lookDirection.sqrMagnitude < 0.001f) return _transform.rotation;
            
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            return ApplyTiltToRotation(lookRotation);
        }
        
        /// <summary>
        /// Updates the camera rotation to look at the target.
        /// </summary>
        private void UpdateCameraRotation()
        {
            if (_target == null) return;
            
            Quaternion targetRotation = CalculateTargetRotation();
            
            if (_rotationSmoothSpeed > 0f)
            {
                _transform.rotation = Quaternion.Slerp(
                    _transform.rotation,
                    targetRotation,
                    _rotationSmoothSpeed * Time.deltaTime
                );
            }
            else
            {
                _transform.rotation = targetRotation;
            }
        }
        
        /// <summary>
        /// Applies roll (tilt) and pitch offset to create downhill illusion.
        /// </summary>
        private Quaternion ApplyTiltToRotation(Quaternion baseRotation)
        {
            if (Mathf.Approximately(_rollAngle, 0f) && Mathf.Approximately(_pitchOffset, 0f))
            {
                return baseRotation;
            }
            
            Quaternion pitchRotation = Quaternion.AngleAxis(_pitchOffset, Vector3.right);
            Quaternion rollRotation = Quaternion.AngleAxis(_rollAngle, Vector3.forward);
            
            return baseRotation * pitchRotation * rollRotation;
        }
        
        #endregion

        #region Editor Gizmos
        
#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private float _gizmoSize = 0.5f;
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            Transform t = _transform != null ? _transform : transform;
            
            // Draw camera indicator
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(t.position, _gizmoSize * 0.5f);
            
            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(t.position, t.forward * _gizmoSize * 2f);
            
            // Draw line to target
            if (_target != null)
            {
                Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.5f);
                Vector3 lookAtPos = _target.position + Vector3.up * _lookAtHeightOffset;
                Gizmos.DrawLine(t.position, lookAtPos);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lookAtPos, _gizmoSize * 0.3f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;
            
            ResolvePathProvider();
            if (_pathProvider == null || !_pathProvider.HasValidPath) return;
            
            Transform t = _transform != null ? _transform : transform;
            
            // Show the camera's position on the path
            float targetProgress = GetTargetProgress();
            float cameraProgress = targetProgress + _progressOffset;
            if (cameraProgress > 1f) cameraProgress -= 1f;
            
            var point = _pathProvider.GetPointAlongPath(cameraProgress);
            if (point.HasValue)
            {
                var pathPoint = point.Value;
                Vector3 pathPosition = pathPoint.Position;
                
                // Draw connection to path surface
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(t.position, pathPosition);
                
                // Draw path point
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(pathPosition, _gizmoSize * 0.3f);
                
                // Draw up/right vectors at path point
                Gizmos.color = Color.green;
                Gizmos.DrawRay(pathPosition, pathPoint.Up * _gizmoSize);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(pathPosition, pathPoint.Right * _gizmoSize);
            }
            
            // Draw target's position on path if available
            if (_targetPathFollower != null)
            {
                var targetPoint = _pathProvider.GetPointAlongPath(targetProgress);
                if (targetPoint.HasValue)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(targetPoint.Value.Position, _gizmoSize * 0.4f);
                }
            }
            
            // Labels
            UnityEditor.Handles.Label(
                t.position + Vector3.up * (_gizmoSize + 0.5f),
                $"Camera Progress: {cameraProgress:P1}\nTarget Progress: {targetProgress:P1}\nOffset: {_progressOffset:P1}",
                new GUIStyle { normal = { textColor = Color.white }, fontSize = 10 }
            );
        }
#endif
        
        #endregion
    }
}
