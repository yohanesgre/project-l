using System.Collections.Generic;
using UnityEngine;

namespace Runtime.WorldBuilding
{
    /// <summary>
    /// Generates a road mesh that follows a path defined by waypoints.
    /// 
    /// Usage:
    /// 1. Add this component to a GameObject
    /// 2. Create child GameObjects with RoadWaypoint components to define the path
    /// 3. Alternatively, assign waypoints manually via the inspector
    /// 4. Configure road settings (width, segments, loop)
    /// 5. Click "Generate Road" in the inspector or call GenerateRoad() at runtime
    /// </summary>
    [ExecuteInEditMode]
    public class RoadGenerator : MonoBehaviour
    {
        [Header("Road Path")]
        [Tooltip("List of waypoints defining the road path. If empty, child RoadWaypoint components will be used.")]
        [SerializeField] private List<RoadWaypoint> _waypoints = new List<RoadWaypoint>();
        
        [Tooltip("If true, the road forms a closed loop connecting the last waypoint back to the first.")]
        [SerializeField] private bool _isLoop = false;
        
        [Header("Road Dimensions")]
        [Tooltip("Default width of the road in units.")]
        [SerializeField] private float _roadWidth = 4f;
        
        [Tooltip("Number of mesh segments generated between each pair of waypoints. Higher = smoother curves.")]
        [Range(1, 50)]
        [SerializeField] private int _segmentsPerWaypoint = 10;
        
        [Header("Material")]
        [Tooltip("Material applied to the generated road mesh.")]
        [SerializeField] private Material _roadMaterial;
        
        [Tooltip("How many times the texture tiles along the road length.")]
        [SerializeField] private float _uvTiling = 1f;
        
        [Header("Generation Settings")]
        [Tooltip("Automatically regenerate the road when waypoints change in the editor.")]
        [SerializeField] private bool _autoRegenerate = true;
        
        [Tooltip("Add a MeshCollider to the generated road for physics interactions.")]
        [SerializeField] private bool _generateCollider = true;
        
        // Generated components
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _generatedMesh;
        
        // Cached interpolated points for gizmo drawing
        private List<RoadMeshBuilder.RoadPoint> _interpolatedPoints;

        /// <summary>
        /// Whether the road forms a closed loop.
        /// </summary>
        public bool IsLoop
        {
            get => _isLoop;
            set
            {
                _isLoop = value;
                if (_autoRegenerate) GenerateRoad();
            }
        }

        /// <summary>
        /// The default road width.
        /// </summary>
        public float RoadWidth
        {
            get => _roadWidth;
            set
            {
                _roadWidth = Mathf.Max(0.1f, value);
                if (_autoRegenerate) GenerateRoad();
            }
        }

        /// <summary>
        /// Read-only access to the current waypoints.
        /// </summary>
        public IReadOnlyList<RoadWaypoint> Waypoints => _waypoints;
        
        /// <summary>
        /// The generated mesh (null if not yet generated).
        /// </summary>
        public Mesh GeneratedMesh => _generatedMesh;

        private void Awake()
        {
            CacheOrCreateComponents();
        }
        
        private void Start()
        {
            // Regenerate road at runtime to ensure interpolated points are available
            if (Application.isPlaying)
            {
                GenerateRoad();
            }
        }

        private void OnValidate()
        {
            // Ensure minimum values
            _roadWidth = Mathf.Max(0.1f, _roadWidth);
            _segmentsPerWaypoint = Mathf.Max(1, _segmentsPerWaypoint);
            _uvTiling = Mathf.Max(0.01f, _uvTiling);
            
            if (_autoRegenerate && Application.isEditor)
            {
                // Delay to avoid issues during serialization
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        GenerateRoad();
                    }
                };
            }
        }

        /// <summary>
        /// Collects waypoints from children if the waypoints list is empty.
        /// </summary>
        public void CollectChildWaypoints()
        {
            _waypoints.Clear();
            _waypoints.AddRange(GetComponentsInChildren<RoadWaypoint>());
        }

        /// <summary>
        /// Generates the road mesh based on current waypoint configuration.
        /// </summary>
        [ContextMenu("Generate Road")]
        public void GenerateRoad()
        {
            CacheOrCreateComponents();
            
            // If no explicit waypoints, try to find child waypoints
            List<RoadWaypoint> activeWaypoints = new List<RoadWaypoint>(_waypoints);
            if (activeWaypoints.Count == 0)
            {
                activeWaypoints.AddRange(GetComponentsInChildren<RoadWaypoint>());
            }
            
            // Need at least 2 waypoints
            if (activeWaypoints.Count < 2)
            {
                Debug.LogWarning($"RoadGenerator: Need at least 2 waypoints to generate a road. Current count: {activeWaypoints.Count}");
                ClearMesh();
                return;
            }
            
            // Extract positions and widths from waypoints
            // Convert world positions to local space so the mesh aligns correctly
            List<Vector3> positions = new List<Vector3>();
            List<float> widths = new List<float>();
            
            foreach (var waypoint in activeWaypoints)
            {
                if (waypoint == null) continue;
                
                // Convert waypoint world position to local space relative to this transform
                Vector3 localPosition = transform.InverseTransformPoint(waypoint.Position);
                positions.Add(localPosition);
                
                // Use waypoint's width override if set, otherwise use default
                float width = waypoint.WidthOverride > 0 ? waypoint.WidthOverride : _roadWidth;
                widths.Add(width);
            }
            
            if (positions.Count < 2)
            {
                Debug.LogWarning("RoadGenerator: Not enough valid waypoints.");
                ClearMesh();
                return;
            }
            
            // Interpolate path and generate mesh
            _interpolatedPoints = RoadMeshBuilder.InterpolateWaypoints(
                positions, 
                widths, 
                _segmentsPerWaypoint, 
                _isLoop
            );
            
            // Clean up old mesh
            if (_generatedMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_generatedMesh);
                }
                else
                {
                    DestroyImmediate(_generatedMesh);
                }
            }
            
            // Generate new mesh
            _generatedMesh = RoadMeshBuilder.GenerateRoadMesh(_interpolatedPoints, _uvTiling, _isLoop);
            
            if (_generatedMesh != null)
            {
                _meshFilter.sharedMesh = _generatedMesh;
                _meshRenderer.sharedMaterial = _roadMaterial;
                
                if (_generateCollider && _meshCollider != null)
                {
                    _meshCollider.sharedMesh = _generatedMesh;
                }
            }
        }

        /// <summary>
        /// Clears the generated mesh.
        /// </summary>
        [ContextMenu("Clear Road")]
        public void ClearMesh()
        {
            if (_meshFilter != null)
            {
                _meshFilter.sharedMesh = null;
            }
            
            if (_meshCollider != null)
            {
                _meshCollider.sharedMesh = null;
            }
            
            if (_generatedMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_generatedMesh);
                }
                else
                {
                    DestroyImmediate(_generatedMesh);
                }
                _generatedMesh = null;
            }
            
            _interpolatedPoints = null;
        }

        /// <summary>
        /// Adds a new waypoint at the specified position.
        /// </summary>
        public RoadWaypoint AddWaypoint(Vector3 worldPosition)
        {
            GameObject waypointGO = new GameObject($"Waypoint_{_waypoints.Count}");
            waypointGO.transform.SetParent(transform);
            waypointGO.transform.position = worldPosition;
            
            RoadWaypoint waypoint = waypointGO.AddComponent<RoadWaypoint>();
            _waypoints.Add(waypoint);
            
            if (_autoRegenerate)
            {
                GenerateRoad();
            }
            
            return waypoint;
        }

        /// <summary>
        /// Gets a point along the road path at the specified normalized position (0-1).
        /// The returned point's Position, Forward, Right, and Up are in world space.
        /// Smoothly interpolates between path points for continuous movement.
        /// </summary>
        /// <param name="t">Normalized position along the path (0 = start, 1 = end).</param>
        /// <returns>The interpolated road point in world space, or null if no path exists.</returns>
        public RoadMeshBuilder.RoadPoint? GetPointAlongPath(float t)
        {
            if (_interpolatedPoints == null || _interpolatedPoints.Count == 0)
            {
                return null;
            }
            
            t = Mathf.Clamp01(t);
            
            // Calculate the exact position between points
            float scaledT = t * (_interpolatedPoints.Count - 1);
            int indexA = Mathf.FloorToInt(scaledT);
            int indexB = Mathf.CeilToInt(scaledT);
            
            // Clamp indices to valid range
            indexA = Mathf.Clamp(indexA, 0, _interpolatedPoints.Count - 1);
            indexB = Mathf.Clamp(indexB, 0, _interpolatedPoints.Count - 1);
            
            // Get the two points to interpolate between
            var pointA = _interpolatedPoints[indexA];
            var pointB = _interpolatedPoints[indexB];
            
            // Calculate interpolation factor between the two points
            float lerpT = scaledT - indexA;
            
            // Interpolate all properties between the two points (in local space)
            Vector3 localPosition = Vector3.Lerp(pointA.Position, pointB.Position, lerpT);
            Vector3 localForward = Vector3.Slerp(pointA.Forward, pointB.Forward, lerpT).normalized;
            Vector3 localRight = Vector3.Slerp(pointA.Right, pointB.Right, lerpT).normalized;
            Vector3 localUp = Vector3.Slerp(pointA.Up, pointB.Up, lerpT).normalized;
            float width = Mathf.Lerp(pointA.Width, pointB.Width, lerpT);
            float distanceAlongPath = Mathf.Lerp(pointA.DistanceAlongPath, pointB.DistanceAlongPath, lerpT);
            
            // Convert local space to world space for the caller
            return new RoadMeshBuilder.RoadPoint
            {
                Position = transform.TransformPoint(localPosition),
                Forward = transform.TransformDirection(localForward),
                Right = transform.TransformDirection(localRight),
                Up = transform.TransformDirection(localUp),
                Width = width,
                DistanceAlongPath = distanceAlongPath
            };
        }

        private void CacheOrCreateComponents()
        {
            // Get or create MeshFilter
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            // Get or create MeshRenderer
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Get or create MeshCollider if needed
            if (_generateCollider)
            {
                _meshCollider = GetComponent<MeshCollider>();
                if (_meshCollider == null)
                {
                    _meshCollider = gameObject.AddComponent<MeshCollider>();
                }
            }
        }

#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private bool _showPathGizmos = true;
        [SerializeField] private bool _showRoadEdges = true;
        [SerializeField] private Color _pathColor = Color.green;
        [SerializeField] private Color _edgeColor = new Color(0.5f, 0.5f, 1f, 0.5f);

        private void OnDrawGizmos()
        {
            if (!_showPathGizmos) return;
            
            DrawWaypointConnections();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showPathGizmos) return;
            
            DrawInterpolatedPath();
            
            if (_showRoadEdges)
            {
                DrawRoadEdges();
            }
        }

        private void DrawWaypointConnections()
        {
            // Get waypoints
            List<RoadWaypoint> activeWaypoints = new List<RoadWaypoint>(_waypoints);
            if (activeWaypoints.Count == 0)
            {
                activeWaypoints.AddRange(GetComponentsInChildren<RoadWaypoint>());
            }
            
            if (activeWaypoints.Count < 2) return;
            
            Gizmos.color = _pathColor;
            
            // Draw connections between waypoints
            for (int i = 0; i < activeWaypoints.Count - 1; i++)
            {
                if (activeWaypoints[i] != null && activeWaypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(activeWaypoints[i].Position, activeWaypoints[i + 1].Position);
                }
            }
            
            // Draw closing line if loop
            if (_isLoop && activeWaypoints.Count > 1)
            {
                var first = activeWaypoints[0];
                var last = activeWaypoints[activeWaypoints.Count - 1];
                if (first != null && last != null)
                {
                    Gizmos.DrawLine(last.Position, first.Position);
                }
            }
            
            // Draw waypoint numbers
            for (int i = 0; i < activeWaypoints.Count; i++)
            {
                if (activeWaypoints[i] != null)
                {
                    UnityEditor.Handles.Label(
                        activeWaypoints[i].Position + Vector3.up * 1f, 
                        i.ToString(),
                        new GUIStyle { normal = { textColor = Color.white }, fontSize = 14, fontStyle = FontStyle.Bold }
                    );
                }
            }
        }

        private void DrawInterpolatedPath()
        {
            if (_interpolatedPoints == null || _interpolatedPoints.Count < 2) return;
            
            Gizmos.color = Color.cyan;
            
            // Interpolated points are in local space, convert to world space for drawing
            for (int i = 0; i < _interpolatedPoints.Count - 1; i++)
            {
                Vector3 worldPos = transform.TransformPoint(_interpolatedPoints[i].Position);
                Vector3 worldPosNext = transform.TransformPoint(_interpolatedPoints[i + 1].Position);
                Gizmos.DrawLine(worldPos, worldPosNext);
            }
            
            if (_isLoop && _interpolatedPoints.Count > 1)
            {
                Vector3 worldPosLast = transform.TransformPoint(_interpolatedPoints[_interpolatedPoints.Count - 1].Position);
                Vector3 worldPosFirst = transform.TransformPoint(_interpolatedPoints[0].Position);
                Gizmos.DrawLine(worldPosLast, worldPosFirst);
            }
        }

        private void DrawRoadEdges()
        {
            if (_interpolatedPoints == null || _interpolatedPoints.Count < 2) return;
            
            Gizmos.color = _edgeColor;
            
            // Interpolated points are in local space, convert to world space for drawing
            for (int i = 0; i < _interpolatedPoints.Count - 1; i++)
            {
                var current = _interpolatedPoints[i];
                var next = _interpolatedPoints[i + 1];
                
                float halfWidthCurrent = current.Width / 2f;
                float halfWidthNext = next.Width / 2f;
                
                // Convert local positions to world, then offset by local-space right vector transformed to world
                Vector3 worldPosCurrent = transform.TransformPoint(current.Position);
                Vector3 worldPosNext = transform.TransformPoint(next.Position);
                Vector3 worldRightCurrent = transform.TransformDirection(current.Right);
                Vector3 worldRightNext = transform.TransformDirection(next.Right);
                
                // Left edge
                Vector3 leftCurrent = worldPosCurrent - worldRightCurrent * halfWidthCurrent;
                Vector3 leftNext = worldPosNext - worldRightNext * halfWidthNext;
                Gizmos.DrawLine(leftCurrent, leftNext);
                
                // Right edge
                Vector3 rightCurrent = worldPosCurrent + worldRightCurrent * halfWidthCurrent;
                Vector3 rightNext = worldPosNext + worldRightNext * halfWidthNext;
                Gizmos.DrawLine(rightCurrent, rightNext);
            }
        }
#endif
    }
}
