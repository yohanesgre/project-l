using UnityEngine;

namespace Runtime.WorldBuilding
{
    /// <summary>
    /// A camera controller designed for looping road animations with parallax backgrounds.
    /// Supports multiple modes: path-following and orbit (inside circle looking out).
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// Defines how the camera follows the target.
        /// </summary>
        public enum CameraMode
        {
            /// <summary>Camera follows the road path ahead of the target, looking back.</summary>
            FollowPath,
            /// <summary>Camera orbits around a center point, always looking at target. Great for circular roads.</summary>
            OrbitCenter,
            /// <summary>Camera stays at a fixed position, always looking at target.</summary>
            FixedPosition
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Camera Mode")]
        [Tooltip("How the camera follows the target.")]
        [SerializeField] private CameraMode _cameraMode = CameraMode.FollowPath;
        
        [Header("References")]
        [Tooltip("The RoadGenerator that defines the path for the camera to follow (used in FollowPath mode).")]
        [SerializeField] private RoadGenerator _roadGenerator;
        
        [Tooltip("The target Transform to look at (e.g., the cyclist/character).")]
        [SerializeField] private Transform _target;
        
        [Tooltip("Optional: Reference to the RoadPathFollower on the target for synced movement.")]
        [SerializeField] private RoadPathFollower _targetPathFollower;
        
        [Header("Path Mode Settings")]
        [Tooltip("How far ahead of the target the camera should be (in path progress, 0-1 range). E.g., 0.05 = 5% ahead on the path.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _progressOffset = 0.02f;
        
        [Tooltip("Height offset from the road surface.")]
        [SerializeField] private float _heightOffset = 1.5f;
        
        [Tooltip("Lateral offset from road center (positive = right, negative = left).")]
        [SerializeField] private float _lateralOffset = 0f;
        
        [Header("Orbit Mode Settings")]
        [Tooltip("Center point of the orbit (e.g., center of circular road). If null, uses this transform's initial position.")]
        [SerializeField] private Transform _orbitCenter;
        
        [Tooltip("Distance from orbit center to camera.")]
        [SerializeField] private float _orbitRadius = 5f;
        
        [Tooltip("Height offset from orbit center.")]
        [SerializeField] private float _orbitHeightOffset = 0f;
        
        [Tooltip("If true, camera stays at same height as target. If false, uses orbit height offset.")]
        [SerializeField] private bool _matchTargetHeight = false;
        
        [Header("Fixed Position Settings")]
        [Tooltip("Fixed world position for the camera (used in FixedPosition mode).")]
        [SerializeField] private Vector3 _fixedPosition = Vector3.zero;
        
        [Header("Look At Settings")]
        [Tooltip("Height offset for the look-at point on the target.")]
        [SerializeField] private float _lookAtHeightOffset = 1.2f;
        
        [Tooltip("How quickly the camera rotates to face the target.")]
        [SerializeField] private float _rotationSmoothSpeed = 10f;
        
        [Tooltip("Horizontal screen offset for framing. Positive = target appears on left, Negative = target on right.")]
        [Range(-0.5f, 0.5f)]
        [SerializeField] private float _framingOffsetX = 0f;
        
        [Tooltip("Vertical screen offset for framing. Positive = target appears lower, Negative = target higher.")]
        [Range(-0.5f, 0.5f)]
        [SerializeField] private float _framingOffsetY = 0f;
        
        [Header("Downhill Illusion (Camera Tilt)")]
        [Tooltip("Roll angle to tilt the camera, creating a downhill illusion. Positive = tilt right, Negative = tilt left.")]
        [Range(-45f, 45f)]
        [SerializeField] private float _rollAngle = 0f;
        
        [Tooltip("Additional pitch adjustment. Positive = look more down, Negative = look more up.")]
        [Range(-30f, 30f)]
        [SerializeField] private float _pitchOffset = 0f;
        
        [Header("Smoothing")]
        [Tooltip("Smooth time for position following. Lower = snappier.")]
        [Range(0f, 1f)]
        [SerializeField] private float _positionSmoothTime = 0.1f;
        
        [Tooltip("Snap camera to position immediately on start.")]
        [SerializeField] private bool _snapOnStart = true;
        
        [Header("Manual Control (if no target follower)")]
        [Tooltip("Current progress along the path (0 = start, 1 = end). Used when no target follower is assigned.")]
        [Range(0f, 1f)]
        [SerializeField] private float _manualProgress = 0f;
        
        #endregion

        #region Private Fields
        
        private Transform _transform;
        private Vector3 _currentVelocity;
        private bool _isInitialized;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The RoadGenerator that defines the camera's path.
        /// </summary>
        public RoadGenerator RoadGenerator
        {
            get => _roadGenerator;
            set => _roadGenerator = value;
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
        /// Reference to the target's RoadPathFollower for synced movement.
        /// </summary>
        public RoadPathFollower TargetPathFollower
        {
            get => _targetPathFollower;
            set => _targetPathFollower = value;
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
        /// Height offset from the road surface.
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
        /// The current camera mode.
        /// </summary>
        public CameraMode Mode
        {
            get => _cameraMode;
            set => _cameraMode = value;
        }
        
        /// <summary>
        /// Orbit radius for OrbitCenter mode.
        /// </summary>
        public float OrbitRadius
        {
            get => _orbitRadius;
            set => _orbitRadius = Mathf.Max(0.1f, value);
        }
        
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
        }
        
        private void Start()
        {
            // Try to auto-find RoadPathFollower on target if not assigned
            if (_targetPathFollower == null && _target != null)
            {
                _targetPathFollower = _target.GetComponent<RoadPathFollower>();
            }
            
            // Try to get RoadGenerator from target's path follower if not assigned
            if (_roadGenerator == null && _targetPathFollower != null)
            {
                _roadGenerator = _targetPathFollower.RoadGenerator;
            }
            
            if (_snapOnStart)
            {
                SnapToPosition();
            }
            
            _isInitialized = true;
        }
        
        private void OnDestroy()
        {
            // Nothing to clean up now
        }
        
        private void LateUpdate()
        {
            // Check required references based on mode
            if (_cameraMode == CameraMode.FollowPath && _roadGenerator == null) return;
            if (_cameraMode == CameraMode.OrbitCenter && _target == null) return;
            if (_cameraMode == CameraMode.FixedPosition && _target == null) return;
            
            UpdateCameraPosition();
            UpdateCameraRotation();
        }
        
        private void OnValidate()
        {
            _progressOffset = Mathf.Clamp(_progressOffset, 0f, 0.5f);
            _positionSmoothTime = Mathf.Max(0f, _positionSmoothTime);
            _rotationSmoothSpeed = Mathf.Max(0f, _rotationSmoothSpeed);
            _rollAngle = Mathf.Clamp(_rollAngle, -45f, 45f);
            _pitchOffset = Mathf.Clamp(_pitchOffset, -30f, 30f);
            _orbitRadius = Mathf.Max(0.1f, _orbitRadius);
            
            // Update in editor
            if (!Application.isPlaying && (_roadGenerator != null || _target != null))
            {
                if (_transform == null)
                {
                    _transform = transform;
                }
                SnapToPosition();
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Immediately snaps the camera to its calculated position.
        /// </summary>
        public void SnapToPosition()
        {
            // Check required references based on mode
            if (_cameraMode == CameraMode.FollowPath && _roadGenerator == null) return;
            if (_cameraMode == CameraMode.OrbitCenter && _target == null) return;
            if (_cameraMode == CameraMode.FixedPosition && _target == null) return;
            
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
            // This handles curved/sloped roads correctly where a simple delta won't work.
            Vector3 targetPosition = CalculateTargetPosition();
            _transform.position = targetPosition;
            
            // Also snap rotation to prevent jarring angle changes
            Quaternion targetRotation = CalculateTargetRotation();
            _transform.rotation = targetRotation;
            
            // Reset velocity to prevent smoothing artifacts after teleport
            _currentVelocity = Vector3.zero;
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Gets the current target progress, either from the path follower or manual value.
        /// </summary>
        /// <returns>The current progress along the path (0-1).</returns>
        private float GetTargetProgress()
        {
            if (_targetPathFollower != null)
            {
                return _targetPathFollower.Progress;
            }
            return _manualProgress;
        }
        
        /// <summary>
        /// Calculates the camera's target position based on the current camera mode.
        /// </summary>
        /// <returns>The world position for the camera.</returns>
        private Vector3 CalculateTargetPosition()
        {
            switch (_cameraMode)
            {
                case CameraMode.FollowPath:
                    return CalculatePathPosition();
                    
                case CameraMode.OrbitCenter:
                    return CalculateOrbitPosition();
                    
                case CameraMode.FixedPosition:
                    return _fixedPosition;
                    
                default:
                    return _transform.position;
            }
        }
        
        /// <summary>
        /// Calculates camera position for FollowPath mode (ahead of target on the road).
        /// </summary>
        private Vector3 CalculatePathPosition()
        {
            if (_roadGenerator == null) return _transform.position;
            
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
            var point = _roadGenerator.GetPointAlongPath(cameraProgress);
            if (!point.HasValue)
            {
                return _transform.position;
            }
            
            var roadPoint = point.Value;
            
            // Calculate position with offsets
            Vector3 position = roadPoint.Position;
            position += roadPoint.Up * _heightOffset;
            position += roadPoint.Right * _lateralOffset;
            
            return position;
        }
        
        /// <summary>
        /// Calculates camera position for OrbitCenter mode (inside circle looking out).
        /// Camera orbits around a center point, always facing the target.
        /// </summary>
        private Vector3 CalculateOrbitPosition()
        {
            if (_target == null) return _transform.position;
            
            // Get the orbit center position
            Vector3 centerPos = _orbitCenter != null ? _orbitCenter.position : transform.position;
            
            // Calculate direction from center to target
            Vector3 targetPos = _target.position;
            Vector3 directionToTarget = (targetPos - centerPos).normalized;
            
            // Handle case where target is at center
            if (directionToTarget.sqrMagnitude < 0.001f)
            {
                directionToTarget = Vector3.forward;
            }
            
            // Flatten direction to horizontal plane for orbit calculation
            Vector3 flatDirection = new Vector3(directionToTarget.x, 0f, directionToTarget.z).normalized;
            if (flatDirection.sqrMagnitude < 0.001f)
            {
                flatDirection = Vector3.forward;
            }
            
            // Camera position is on the OPPOSITE side of center from target (looking through center at target)
            // This puts the camera INSIDE a circular road, looking OUT at the target
            Vector3 cameraPos = centerPos - flatDirection * _orbitRadius;
            
            // Apply height
            if (_matchTargetHeight)
            {
                cameraPos.y = targetPos.y + _orbitHeightOffset;
            }
            else
            {
                cameraPos.y = centerPos.y + _orbitHeightOffset;
            }
            
            return cameraPos;
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
        /// Calculates the target rotation for the camera (looking at target with tilt and framing applied).
        /// </summary>
        /// <returns>The target rotation.</returns>
        private Quaternion CalculateTargetRotation()
        {
            if (_target == null) return _transform.rotation;
            
            // Look at the target with height offset
            Vector3 lookAtPos = _target.position + Vector3.up * _lookAtHeightOffset;
            
            // Apply framing offset - shift the look-at point to frame the target off-center
            if (Mathf.Abs(_framingOffsetX) > 0.001f || Mathf.Abs(_framingOffsetY) > 0.001f)
            {
                // Calculate right and up vectors relative to camera-to-target direction
                Vector3 toTarget = (lookAtPos - _transform.position).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, toTarget).normalized;
                Vector3 up = Vector3.Cross(toTarget, right).normalized;
                
                // Offset the look-at point (negative because we want target to appear on that side)
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
        /// <param name="baseRotation">The base look-at rotation.</param>
        /// <returns>The rotation with tilt applied.</returns>
        private Quaternion ApplyTiltToRotation(Quaternion baseRotation)
        {
            if (Mathf.Approximately(_rollAngle, 0f) && Mathf.Approximately(_pitchOffset, 0f))
            {
                return baseRotation;
            }
            
            // Apply pitch offset (rotate around local X axis) and roll (rotate around local Z axis)
            // Order: base rotation -> pitch -> roll
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
            
            // Draw forward direction (where camera is looking)
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(t.position, t.forward * _gizmoSize * 2f);
            
            // Draw line to target
            if (_target != null)
            {
                Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.5f);
                Vector3 lookAtPos = _target.position + Vector3.up * _lookAtHeightOffset;
                Gizmos.DrawLine(t.position, lookAtPos);
                
                // Draw look-at point
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lookAtPos, _gizmoSize * 0.3f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;
            if (_roadGenerator == null) return;
            
            Transform t = _transform != null ? _transform : transform;
            
            // Show the camera's position on the path
            float targetProgress = GetTargetProgress();
            float cameraProgress = targetProgress + _progressOffset;
            if (cameraProgress > 1f) cameraProgress -= 1f;
            
            var point = _roadGenerator.GetPointAlongPath(cameraProgress);
            if (point.HasValue)
            {
                var roadPoint = point.Value;
                Vector3 pathPosition = roadPoint.Position;
                
                // Draw connection to road surface
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(t.position, pathPosition);
                
                // Draw road point
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(pathPosition, _gizmoSize * 0.3f);
                
                // Draw up/right vectors at road point
                Gizmos.color = Color.green;
                Gizmos.DrawRay(pathPosition, roadPoint.Up * _gizmoSize);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(pathPosition, roadPoint.Right * _gizmoSize);
            }
            
            // Draw target's position on path if available
            if (_targetPathFollower != null)
            {
                var targetPoint = _roadGenerator.GetPointAlongPath(targetProgress);
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
