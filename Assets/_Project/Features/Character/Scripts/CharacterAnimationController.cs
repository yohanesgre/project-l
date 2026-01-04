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
        
        #endregion

        #region Private Fields
        
        private CharacterPathFollower _pathFollower;
        private int _isDrivingHash;
        private int _speedHash;
        private float _currentSpeed;
        private float _speedVelocity;
        private bool _hasAnimator;
        private bool _wasDriving;
        
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
            }
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
                UpdateAnimationState(true);
            }
        }
        
        #endregion

        #region Private Methods
        
        private void UpdateAnimationState(bool immediate)
        {
            if (_pathFollower == null) return;
            
            bool isDriving = _pathFollower.IsFollowing;
            
            // Update driving state
            if (isDriving != _wasDriving || immediate)
            {
                _animator.SetBool(_isDrivingHash, isDriving);
                _wasDriving = isDriving;
            }
            
            // Update speed parameter if enabled
            if (_useSpeedParameter)
            {
                float targetSpeed = isDriving ? _pathFollower.Speed : 0f;
                
                if (immediate)
                {
                    _currentSpeed = targetSpeed;
                    _speedVelocity = 0f;
                }
                else
                {
                    _currentSpeed = Mathf.SmoothDamp(
                        _currentSpeed,
                        targetSpeed,
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
