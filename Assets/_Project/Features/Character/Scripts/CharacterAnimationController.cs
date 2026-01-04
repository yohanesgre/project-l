using UnityEngine;

namespace MyGame.Features.Character
{
    /// <summary>
    /// Controls character animations based on the CharacterPathFollower state.
    /// Switches between idle and driving animations depending on whether the character is moving.
    /// </summary>
    [RequireComponent(typeof(CharacterPathFollower))]
    public class CharacterAnimationController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("References")]
        [Tooltip("The Animator component to control. If not assigned, will search on this GameObject and children.")]
        [SerializeField] private Animator _animator;
        
        [Header("Animation Parameters")]
        [Tooltip("Name of the bool parameter for driving state.")]
        [SerializeField] private string _isDrivingParameter = "IsDriving";
        
        [Tooltip("Name of the float parameter for speed (optional, for blend trees).")]
        [SerializeField] private string _speedParameter = "Speed";
        
        [Tooltip("Use speed parameter for animation blending.")]
        [SerializeField] private bool _useSpeedParameter = false;
        
        [Header("Transition Settings")]
        [Tooltip("How quickly the speed parameter interpolates to the target value.")]
        [SerializeField] private float _speedSmoothTime = 0.1f;
        
        [Tooltip("Maximum speed value for animation normalization. Speed will be clamped to 0-1 range based on this value.")]
        [SerializeField] private float _maxSpeed = 10f;
        
        #endregion

        #region Private Fields
        
        private CharacterPathFollower _pathFollower;
        private int _isDrivingHash;
        private int _speedHash;
        private float _currentSpeed;
        private float _speedVelocity;
        private bool _hasAnimator;
        private bool _wasDriving;
        
        // Parameter validation
        private bool _hasDrivingParameter;
        private bool _hasSpeedParameter;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The Animator component being controlled.
        /// </summary>
        public Animator Animator => _animator;
        
        /// <summary>
        /// Whether the character is currently in the driving animation state.
        /// </summary>
        public bool IsDriving => _pathFollower != null && _pathFollower.IsFollowing;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _pathFollower = GetComponent<CharacterPathFollower>();
            
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            
            _hasAnimator = _animator != null;
            
            if (!_hasAnimator)
            {
                Debug.LogWarning("CharacterAnimationController: No Animator found. Animation control will be disabled.", this);
                return;
            }
            
            // Cache parameter hashes for performance
            _isDrivingHash = Animator.StringToHash(_isDrivingParameter);
            _speedHash = Animator.StringToHash(_speedParameter);
            
            // Validate animator parameters exist
            ValidateAnimatorParameters();
        }
        
        private void Start()
        {
            if (!_hasAnimator) return;
            
            // Initialize animation state
            UpdateAnimationState(true);
        }
        
        private void Update()
        {
            if (!_hasAnimator) return;
            
            UpdateAnimationState(false);
        }
        
        private void OnValidate()
        {
            // Re-cache hashes if parameters change in editor
            if (_animator != null)
            {
                _isDrivingHash = Animator.StringToHash(_isDrivingParameter);
                _speedHash = Animator.StringToHash(_speedParameter);
                
                if (Application.isPlaying)
                {
                    ValidateAnimatorParameters();
                }
            }
            
            // Ensure max speed is positive
            _maxSpeed = Mathf.Max(0.1f, _maxSpeed);
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Forces an immediate update of the animation state.
        /// </summary>
        public void ForceUpdateAnimation()
        {
            if (!_hasAnimator) return;
            UpdateAnimationState(true);
        }
        
        /// <summary>
        /// Sets the animator reference at runtime.
        /// </summary>
        public void SetAnimator(Animator animator)
        {
            _animator = animator;
            _hasAnimator = _animator != null;
            
            if (_hasAnimator)
            {
                _isDrivingHash = Animator.StringToHash(_isDrivingParameter);
                _speedHash = Animator.StringToHash(_speedParameter);
                ValidateAnimatorParameters();
                UpdateAnimationState(true);
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Validates that the required animator parameters exist in the animator controller.
        /// </summary>
        private void ValidateAnimatorParameters()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                _hasDrivingParameter = false;
                _hasSpeedParameter = false;
                return;
            }
            
            // Check for driving parameter
            _hasDrivingParameter = HasAnimatorParameter(_isDrivingParameter, AnimatorControllerParameterType.Bool);
            if (!_hasDrivingParameter)
            {
                Debug.LogWarning($"CharacterAnimationController: Animator parameter '{_isDrivingParameter}' (Bool) not found in controller '{_animator.runtimeAnimatorController.name}'.", this);
            }
            
            // Check for speed parameter (only if enabled)
            if (_useSpeedParameter)
            {
                _hasSpeedParameter = HasAnimatorParameter(_speedParameter, AnimatorControllerParameterType.Float);
                if (!_hasSpeedParameter)
                {
                    Debug.LogWarning($"CharacterAnimationController: Animator parameter '{_speedParameter}' (Float) not found in controller '{_animator.runtimeAnimatorController.name}'.", this);
                }
            }
            else
            {
                _hasSpeedParameter = false;
            }
        }
        
        /// <summary>
        /// Checks if an animator parameter exists with the specified name and type.
        /// </summary>
        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
        {
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.name == parameterName && parameter.type == parameterType)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void UpdateAnimationState(bool immediate)
        {
            if (_pathFollower == null) return;
            
            bool isDriving = _pathFollower.IsFollowing;
            
            // Update driving state (only if parameter exists)
            if (isDriving != _wasDriving || immediate)
            {
                if (_hasDrivingParameter)
                {
                    _animator.SetBool(_isDrivingHash, isDriving);
                }
                _wasDriving = isDriving;
            }
            
            // Update speed parameter if enabled and exists
            if (_useSpeedParameter && _hasSpeedParameter)
            {
                float targetSpeed = isDriving ? _pathFollower.Speed : 0f;
                
                // Normalize speed to 0-1 range based on max speed to prevent animation popping
                float normalizedSpeed = Mathf.Clamp01(targetSpeed / _maxSpeed);
                
                if (immediate)
                {
                    _currentSpeed = normalizedSpeed;
                    _speedVelocity = 0f;
                }
                else
                {
                    _currentSpeed = Mathf.SmoothDamp(
                        _currentSpeed,
                        normalizedSpeed,
                        ref _speedVelocity,
                        _speedSmoothTime
                    );
                }
                
                _animator.SetFloat(_speedHash, _currentSpeed);
            }
        }
        
        #endregion
    }
}
