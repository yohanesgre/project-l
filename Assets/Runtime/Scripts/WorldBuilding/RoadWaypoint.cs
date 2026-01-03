using UnityEngine;

namespace Runtime.WorldBuilding
{
    /// <summary>
    /// Represents a waypoint on the road path.
    /// Add this component to GameObjects that define the road's path.
    /// The road generator will connect these waypoints in order.
    /// </summary>
    public class RoadWaypoint : MonoBehaviour
    {
        [Header("Waypoint Settings")]
        [Tooltip("Optional width override for this specific waypoint. If 0, uses the generator's default width.")]
        [SerializeField] private float _widthOverride = 0f;
        
        [Tooltip("Optional custom tangent direction. If zero, tangent is calculated automatically.")]
        [SerializeField] private Vector3 _customTangent = Vector3.zero;
        
        /// <summary>
        /// The width override for this waypoint. Returns 0 if using default width.
        /// </summary>
        public float WidthOverride => _widthOverride;
        
        /// <summary>
        /// Custom tangent direction for more control over curves. Returns Vector3.zero if auto-calculated.
        /// </summary>
        public Vector3 CustomTangent => _customTangent;
        
        /// <summary>
        /// Whether this waypoint has a custom tangent defined.
        /// </summary>
        public bool HasCustomTangent => _customTangent.sqrMagnitude > 0.001f;
        
        /// <summary>
        /// World position of this waypoint.
        /// </summary>
        public Vector3 Position => transform.position;

#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private float _gizmoSize = 0.5f;
        [SerializeField] private Color _gizmoColor = Color.yellow;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawSphere(transform.position, _gizmoSize);
            
            // Draw custom tangent if defined
            if (HasCustomTangent)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, _customTangent.normalized * 2f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _gizmoSize * 1.2f);
            
            // Show width override
            if (_widthOverride > 0)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Vector3 right = transform.right * (_widthOverride / 2f);
                Gizmos.DrawLine(transform.position - right, transform.position + right);
            }
        }
#endif
    }
}
