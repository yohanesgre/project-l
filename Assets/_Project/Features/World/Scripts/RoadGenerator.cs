using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Features.World
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
        
        [Header("Dirt Layer")]
        [Tooltip("Enable a dirt/ground layer beneath the road.")]
        [SerializeField] private bool _enableDirtLayer = false;
        
        [Tooltip("Material applied to the dirt layer.")]
        [SerializeField] private Material _dirtMaterial;
        
        [Tooltip("How much wider the dirt layer is than the road (added to each side).")]
        [SerializeField] private float _dirtLayerExtraWidth = 1f;
        
        [Tooltip("How far below the road surface the dirt layer sits.")]
        [SerializeField] private float _dirtLayerOffset = 0.05f;
        
        [Tooltip("UV tiling for the dirt layer.")]
        [SerializeField] private float _dirtUvTiling = 1f;
        
        [Header("Road Rails")]
        [Tooltip("Enable road rails/barriers along the edges.")]
        [SerializeField] private bool _enableRails = false;
        
        [Tooltip("Prefab to use for the rail (e.g., Railing.fbx).")]
        [SerializeField] private GameObject _railPrefab;
        
        [Tooltip("Spacing between rail instances along the path. Set to 0 to auto-calculate from prefab bounds.")]
        [SerializeField] private float _railSpacing = 0f;
        
        [Tooltip("Offset from road edge (positive = outward, negative = inward).")]
        [SerializeField] private float _railEdgeOffset = 0.1f;
        
        [Tooltip("Vertical offset for rail placement.")]
        [SerializeField] private float _railVerticalOffset = 0f;
        
        [Tooltip("Scale multiplier for rail prefabs.")]
        [SerializeField] private Vector3 _railScale = Vector3.one;
        
        [Tooltip("Rotation offset in degrees (applied after aligning to road direction).")]
        [SerializeField] private Vector3 _railRotationOffset = Vector3.zero;
        
        [Tooltip("Which axis of the prefab points along the road direction.")]
        [SerializeField] private RailForwardAxis _railForwardAxis = RailForwardAxis.Z;
        
        [Tooltip("Enable rails on the left side of the road.")]
        [SerializeField] private bool _leftRails = true;
        
        [Tooltip("Enable rails on the right side of the road.")]
        [SerializeField] private bool _rightRails = true;
        
        public enum RailForwardAxis { X, Y, Z, NegativeX, NegativeY, NegativeZ }
        
        [Header("Road Signs")]
        [Tooltip("Enable decorative signs along the road edges.")]
        [SerializeField] private bool _enableSigns = false;
        
        [Tooltip("Prefab to use for signs (e.g., Signage.fbx).")]
        [SerializeField] private GameObject _signPrefab;
        
        [Tooltip("Average spacing between signs. Actual placement will be randomized.")]
        [SerializeField] private float _signSpacing = 15f;
        
        [Tooltip("Random offset range for sign placement along the road (0 = evenly spaced).")]
        [SerializeField] private float _signSpacingVariation = 5f;
        
        [Tooltip("Minimum distance from road edge (positive = outward from road).")]
        [SerializeField] private float _signMinEdgeOffset = 0.5f;
        
        [Tooltip("Maximum distance from road edge (positive = outward from road).")]
        [SerializeField] private float _signMaxEdgeOffset = 2f;
        
        [Tooltip("Vertical offset for sign placement.")]
        [SerializeField] private float _signVerticalOffset = 0f;
        
        [Tooltip("Base scale for sign prefabs.")]
        [SerializeField] private Vector3 _signScale = Vector3.one;
        
        [Tooltip("Random scale variation (applied as multiplier: 1 +/- this value).")]
        [SerializeField] private float _signScaleVariation = 0.1f;
        
        [Tooltip("If true, signs will face toward the road. If false, signs face along road direction.")]
        [SerializeField] private bool _signsFaceRoad = true;
        
        [Tooltip("Random Y-axis rotation variation in degrees.")]
        [SerializeField] private float _signRotationVariation = 15f;
        
        [Tooltip("Enable signs on the left side of the road.")]
        [SerializeField] private bool _leftSigns = true;
        
        [Tooltip("Enable signs on the right side of the road.")]
        [SerializeField] private bool _rightSigns = true;
        
        [Tooltip("Random seed for sign placement (0 = use system time).")]
        [SerializeField] private int _signRandomSeed = 0;
        
        [Header("Road Grass")]
        [Tooltip("Enable decorative grass along the road edges.")]
        [SerializeField] private bool _enableGrass = false;
        
        [Tooltip("Prefab to use for grass (e.g., Grass_1.fbx).")]
        [SerializeField] private GameObject _grassPrefab;
        
        [Tooltip("Average spacing between grass instances.")]
        [SerializeField] private float _grassSpacing = 2f;
        
        [Tooltip("Random offset range for grass placement along the road.")]
        [SerializeField] private float _grassSpacingVariation = 1f;
        
        [Tooltip("Minimum distance from road edge (positive = outward from road).")]
        [SerializeField] private float _grassMinEdgeOffset = 0.2f;
        
        [Tooltip("Maximum distance from road edge (positive = outward from road).")]
        [SerializeField] private float _grassMaxEdgeOffset = 3f;
        
        [Tooltip("Vertical offset for grass placement.")]
        [SerializeField] private float _grassVerticalOffset = 0f;
        
        [Tooltip("Base scale for grass prefabs.")]
        [SerializeField] private Vector3 _grassScale = Vector3.one;
        
        [Tooltip("Random scale variation (applied as multiplier: 1 +/- this value).")]
        [SerializeField] private float _grassScaleVariation = 0.3f;
        
        [Tooltip("Random Y-axis rotation variation in degrees (360 = full random rotation).")]
        [SerializeField] private float _grassRotationVariation = 360f;
        
        [Tooltip("Enable grass on the left side of the road.")]
        [SerializeField] private bool _leftGrass = true;
        
        [Tooltip("Enable grass on the right side of the road.")]
        [SerializeField] private bool _rightGrass = true;
        
        [Tooltip("Random seed for grass placement (0 = use system time).")]
        [SerializeField] private int _grassRandomSeed = 0;
        
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
        
        // Dirt layer components
        private GameObject _dirtLayerObject;
        private MeshFilter _dirtMeshFilter;
        private MeshRenderer _dirtMeshRenderer;
        private Mesh _dirtMesh;
        
        // Rail container
        private GameObject _railsContainer;
        private List<GameObject> _spawnedRails = new List<GameObject>();
        
        // Sign container
        private GameObject _signsContainer;
        private List<GameObject> _spawnedSigns = new List<GameObject>();
        
        // Grass container
        private GameObject _grassContainer;
        private List<GameObject> _spawnedGrass = new List<GameObject>();
        
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
        
        /// <summary>
        /// Gets the total length of the path in world units.
        /// For loops, this includes the segment from the last point back to the first.
        /// </summary>
        public float TotalPathLength
        {
            get
            {
                if (_interpolatedPoints == null || _interpolatedPoints.Count < 2)
                    return 0f;
                
                // Get the distance of the last interpolated point
                float length = _interpolatedPoints[_interpolatedPoints.Count - 1].DistanceAlongPath;
                
                // For loops, add the distance from the last point back to the first
                if (_isLoop)
                {
                    Vector3 lastPos = transform.TransformPoint(_interpolatedPoints[_interpolatedPoints.Count - 1].Position);
                    Vector3 firstPos = transform.TransformPoint(_interpolatedPoints[0].Position);
                    length += Vector3.Distance(lastPos, firstPos);
                }
                
                return length;
            }
        }

        private void Awake()
        {
            CacheOrCreateComponents();
        }
        
        private void Start()
        {
            // Only recalculate interpolated points at runtime if needed for path following
            // Don't regenerate the entire road (mesh, rails, signs, grass) since those persist from editor
            if (Application.isPlaying && _interpolatedPoints == null)
            {
                RecalculateInterpolatedPoints();
            }
        }
        
        /// <summary>
        /// Recalculates interpolated points without regenerating mesh or spawned objects.
        /// Used at runtime to restore path data for RoadPathFollower.
        /// </summary>
        private void RecalculateInterpolatedPoints()
        {
            // If no explicit waypoints, try to find child waypoints
            List<RoadWaypoint> activeWaypoints = new List<RoadWaypoint>(_waypoints);
            if (activeWaypoints.Count == 0)
            {
                activeWaypoints.AddRange(GetComponentsInChildren<RoadWaypoint>());
            }
            
            if (activeWaypoints.Count < 2)
            {
                return;
            }
            
            // Extract positions and widths from waypoints
            List<Vector3> positions = new List<Vector3>();
            List<float> widths = new List<float>();
            
            foreach (var waypoint in activeWaypoints)
            {
                if (waypoint == null) continue;
                
                Vector3 localPosition = transform.InverseTransformPoint(waypoint.Position);
                positions.Add(localPosition);
                
                float width = waypoint.WidthOverride > 0 ? waypoint.WidthOverride : _roadWidth;
                widths.Add(width);
            }
            
            if (positions.Count < 2)
            {
                return;
            }
            
            // Only recalculate the interpolated points - don't touch mesh or spawned objects
            _interpolatedPoints = RoadMeshBuilder.InterpolateWaypoints(
                positions, 
                widths, 
                _segmentsPerWaypoint, 
                _isLoop
            );
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
                
                // Generate dirt layer if enabled
                if (_enableDirtLayer)
                {
                    GenerateDirtLayer();
                }
                else
                {
                    ClearDirtLayer();
                }
                
                // Generate rails if enabled
                if (_enableRails && _railPrefab != null)
                {
                    GenerateRails();
                }
                else
                {
                    ClearRails();
                }
                
                // Generate signs if enabled
                if (_enableSigns && _signPrefab != null)
                {
                    GenerateSigns();
                }
                else
                {
                    ClearSigns();
                }
                
                // Generate grass if enabled
                if (_enableGrass && _grassPrefab != null)
                {
                    GenerateGrass();
                }
                else
                {
                    ClearGrass();
                }
            }
        }

        /// <summary>
        /// Generates the dirt/ground layer beneath the road.
        /// </summary>
        private void GenerateDirtLayer()
        {
            // Create or get dirt layer object
            if (_dirtLayerObject == null)
            {
                _dirtLayerObject = transform.Find("DirtLayer")?.gameObject;
                if (_dirtLayerObject == null)
                {
                    _dirtLayerObject = new GameObject("DirtLayer");
                    _dirtLayerObject.transform.SetParent(transform);
                    _dirtLayerObject.transform.localPosition = Vector3.zero;
                    _dirtLayerObject.transform.localRotation = Quaternion.identity;
                    _dirtLayerObject.transform.localScale = Vector3.one;
                }
            }
            
            // Get or add components
            _dirtMeshFilter = _dirtLayerObject.GetComponent<MeshFilter>();
            if (_dirtMeshFilter == null)
            {
                _dirtMeshFilter = _dirtLayerObject.AddComponent<MeshFilter>();
            }
            
            _dirtMeshRenderer = _dirtLayerObject.GetComponent<MeshRenderer>();
            if (_dirtMeshRenderer == null)
            {
                _dirtMeshRenderer = _dirtLayerObject.AddComponent<MeshRenderer>();
            }
            
            // Create wider road points for dirt layer
            List<RoadMeshBuilder.RoadPoint> dirtPoints = new List<RoadMeshBuilder.RoadPoint>();
            foreach (var point in _interpolatedPoints)
            {
                var dirtPoint = new RoadMeshBuilder.RoadPoint
                {
                    Position = point.Position - Vector3.up * _dirtLayerOffset,
                    Forward = point.Forward,
                    Right = point.Right,
                    Up = point.Up,
                    Width = point.Width + (_dirtLayerExtraWidth * 2f),
                    DistanceAlongPath = point.DistanceAlongPath
                };
                dirtPoints.Add(dirtPoint);
            }
            
            // Clean up old mesh
            if (_dirtMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_dirtMesh);
                }
                else
                {
                    DestroyImmediate(_dirtMesh);
                }
            }
            
            // Generate dirt mesh
            _dirtMesh = RoadMeshBuilder.GenerateRoadMesh(dirtPoints, _dirtUvTiling, _isLoop);
            
            if (_dirtMesh != null)
            {
                _dirtMesh.name = "GeneratedDirtLayer";
                _dirtMeshFilter.sharedMesh = _dirtMesh;
                _dirtMeshRenderer.sharedMaterial = _dirtMaterial;
            }
        }
        
        /// <summary>
        /// Clears the dirt layer.
        /// </summary>
        private void ClearDirtLayer()
        {
            if (_dirtMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_dirtMesh);
                }
                else
                {
                    DestroyImmediate(_dirtMesh);
                }
                _dirtMesh = null;
            }
            
            if (_dirtLayerObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_dirtLayerObject);
                }
                else
                {
                    DestroyImmediate(_dirtLayerObject);
                }
                _dirtLayerObject = null;
            }
        }
        
        /// <summary>
        /// Generates rails along the road edges.
        /// </summary>
        private void GenerateRails()
        {
            ClearRails();
            
            if (_railPrefab == null || _interpolatedPoints == null || _interpolatedPoints.Count < 2)
            {
                return;
            }
            
            // Create rails container
            _railsContainer = new GameObject("Rails");
            _railsContainer.transform.SetParent(transform);
            _railsContainer.transform.localPosition = Vector3.zero;
            _railsContainer.transform.localRotation = Quaternion.identity;
            _railsContainer.transform.localScale = Vector3.one;
            
            // Calculate spacing - auto-detect from prefab bounds if spacing is 0
            float spacing = _railSpacing;
            if (spacing <= 0f)
            {
                spacing = GetRailPrefabLength();
                if (spacing <= 0f)
                {
                    spacing = 2f; // Fallback
                    Debug.LogWarning("RoadGenerator: Could not auto-detect rail prefab length, using default spacing of 2.");
                }
            }
            
            // Generate left and right rails separately to handle different arc lengths on curves
            if (_leftRails)
            {
                GenerateRailsOnSide(spacing, isLeftSide: true);
            }
            
            if (_rightRails)
            {
                GenerateRailsOnSide(spacing, isLeftSide: false);
            }
        }
        
        /// <summary>
        /// Generates rails on one side of the road, calculating proper arc length for curves.
        /// </summary>
        private void GenerateRailsOnSide(float baseSpacing, bool isLeftSide)
        {
            // Calculate actual edge path length for this side
            // On the inside of curves, the arc is shorter; on outside, it's longer
            float edgePathLength = 0f;
            Vector3 prevEdgePos = Vector3.zero;
            
            List<float> edgeDistances = new List<float>();
            List<RoadMeshBuilder.RoadPoint> edgePoints = new List<RoadMeshBuilder.RoadPoint>();
            
            int pointCount = _interpolatedPoints.Count;
            int totalPoints = _isLoop ? pointCount + 1 : pointCount; // +1 to close the loop
            
            for (int i = 0; i < totalPoints; i++)
            {
                int idx = i % pointCount;
                var point = _interpolatedPoints[idx];
                float halfWidth = point.Width / 2f;
                
                // Calculate edge position
                Vector3 edgePos;
                if (isLeftSide)
                {
                    edgePos = transform.TransformPoint(point.Position) 
                        - transform.TransformDirection(point.Right) * (halfWidth + _railEdgeOffset);
                }
                else
                {
                    edgePos = transform.TransformPoint(point.Position) 
                        + transform.TransformDirection(point.Right) * (halfWidth + _railEdgeOffset);
                }
                
                if (i > 0)
                {
                    edgePathLength += Vector3.Distance(prevEdgePos, edgePos);
                }
                
                edgeDistances.Add(edgePathLength);
                
                // Store world-space point data
                edgePoints.Add(new RoadMeshBuilder.RoadPoint
                {
                    Position = edgePos,
                    Forward = transform.TransformDirection(point.Forward),
                    Right = transform.TransformDirection(point.Right),
                    Up = transform.TransformDirection(point.Up),
                    Width = point.Width,
                    DistanceAlongPath = edgePathLength
                });
                
                prevEdgePos = edgePos;
            }
            
            if (edgePathLength <= 0) return;
            
            // Calculate number of rails based on edge path length
            int railCount = Mathf.FloorToInt(edgePathLength / baseSpacing);
            if (railCount < 1) railCount = 1;
            
            // For loops, distribute rails evenly around the entire loop
            // For non-loops, use distance-based placement
            Quaternion axisRotation = GetAxisAlignmentRotation();
            Quaternion userRotationOffset = Quaternion.Euler(_railRotationOffset);
            
            for (int i = 0; i < railCount; i++)
            {
                float targetDistance;
                
                if (_isLoop)
                {
                    // Evenly distribute around the loop
                    targetDistance = (float)i / railCount * edgePathLength;
                }
                else
                {
                    // Distance-based for non-loops
                    targetDistance = i * baseSpacing;
                    if (targetDistance > edgePathLength) continue;
                }
                
                // Find the interpolated position along the edge path
                var edgePoint = GetPointAtEdgeDistance(edgePoints, edgeDistances, targetDistance);
                if (!edgePoint.HasValue) continue;
                
                Vector3 position = edgePoint.Value.Position + edgePoint.Value.Up * _railVerticalOffset;
                
                // Calculate rotation
                Quaternion baseRot = Quaternion.LookRotation(edgePoint.Value.Forward, edgePoint.Value.Up);
                Quaternion finalRot;
                
                if (isLeftSide)
                {
                    finalRot = baseRot * axisRotation * userRotationOffset;
                }
                else
                {
                    // Flip 180 degrees for right side
                    finalRot = baseRot * Quaternion.Euler(0, 180, 0) * axisRotation * userRotationOffset;
                }
                
                string sideName = isLeftSide ? "Left" : "Right";
                SpawnRail(position, finalRot, $"{sideName}Rail_{i}");
            }
        }
        
        /// <summary>
        /// Gets an interpolated point at a specific distance along the edge path.
        /// </summary>
        private RoadMeshBuilder.RoadPoint? GetPointAtEdgeDistance(
            List<RoadMeshBuilder.RoadPoint> edgePoints, 
            List<float> edgeDistances, 
            float targetDistance)
        {
            if (edgePoints.Count < 2) return null;
            
            // Find the segment containing this distance
            int indexA = 0;
            for (int i = 0; i < edgeDistances.Count - 1; i++)
            {
                if (edgeDistances[i + 1] >= targetDistance)
                {
                    indexA = i;
                    break;
                }
                indexA = i;
            }
            
            int indexB = Mathf.Min(indexA + 1, edgePoints.Count - 1);
            
            // Calculate interpolation factor
            float segmentStart = edgeDistances[indexA];
            float segmentEnd = edgeDistances[indexB];
            float segmentLength = segmentEnd - segmentStart;
            
            float lerpT = 0f;
            if (segmentLength > 0.001f)
            {
                lerpT = (targetDistance - segmentStart) / segmentLength;
            }
            
            var pointA = edgePoints[indexA];
            var pointB = edgePoints[indexB];
            
            return new RoadMeshBuilder.RoadPoint
            {
                Position = Vector3.Lerp(pointA.Position, pointB.Position, lerpT),
                Forward = Vector3.Slerp(pointA.Forward, pointB.Forward, lerpT).normalized,
                Right = Vector3.Slerp(pointA.Right, pointB.Right, lerpT).normalized,
                Up = Vector3.Slerp(pointA.Up, pointB.Up, lerpT).normalized,
                Width = Mathf.Lerp(pointA.Width, pointB.Width, lerpT),
                DistanceAlongPath = targetDistance
            };
        }
        
        /// <summary>
        /// Gets the length of the rail prefab along its forward axis.
        /// </summary>
        private float GetRailPrefabLength()
        {
            if (_railPrefab == null) return 0f;
            
            // Try to get bounds from renderer
            var renderer = _railPrefab.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                
                // Get the size along the axis that will be aligned to the road
                switch (_railForwardAxis)
                {
                    case RailForwardAxis.X:
                    case RailForwardAxis.NegativeX:
                        return bounds.size.x * _railScale.x;
                    case RailForwardAxis.Y:
                    case RailForwardAxis.NegativeY:
                        return bounds.size.y * _railScale.y;
                    case RailForwardAxis.Z:
                    case RailForwardAxis.NegativeZ:
                    default:
                        return bounds.size.z * _railScale.z;
                }
            }
            
            // Try mesh filter
            var meshFilter = _railPrefab.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds bounds = meshFilter.sharedMesh.bounds;
                
                switch (_railForwardAxis)
                {
                    case RailForwardAxis.X:
                    case RailForwardAxis.NegativeX:
                        return bounds.size.x * _railScale.x;
                    case RailForwardAxis.Y:
                    case RailForwardAxis.NegativeY:
                        return bounds.size.y * _railScale.y;
                    case RailForwardAxis.Z:
                    case RailForwardAxis.NegativeZ:
                    default:
                        return bounds.size.z * _railScale.z;
                }
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Gets the rotation needed to align the prefab's forward axis to Unity's forward (Z+).
        /// </summary>
        private Quaternion GetAxisAlignmentRotation()
        {
            switch (_railForwardAxis)
            {
                case RailForwardAxis.X:
                    return Quaternion.Euler(0, -90, 0);
                case RailForwardAxis.NegativeX:
                    return Quaternion.Euler(0, 90, 0);
                case RailForwardAxis.Y:
                    return Quaternion.Euler(90, 0, 0);
                case RailForwardAxis.NegativeY:
                    return Quaternion.Euler(-90, 0, 0);
                case RailForwardAxis.NegativeZ:
                    return Quaternion.Euler(0, 180, 0);
                case RailForwardAxis.Z:
                default:
                    return Quaternion.identity;
            }
        }
        
        /// <summary>
        /// Spawns a single rail instance.
        /// </summary>
        private void SpawnRail(Vector3 position, Quaternion rotation, string name)
        {
            GameObject rail;
            
            if (Application.isPlaying)
            {
                rail = Instantiate(_railPrefab, position, rotation, _railsContainer.transform);
            }
            else
            {
                #if UNITY_EDITOR
                rail = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_railPrefab, _railsContainer.transform);
                rail.transform.position = position;
                rail.transform.rotation = rotation;
                #else
                rail = Instantiate(_railPrefab, position, rotation, _railsContainer.transform);
                #endif
            }
            
            rail.name = name;
            rail.transform.localScale = _railScale;
            _spawnedRails.Add(rail);
        }
        
        /// <summary>
        /// Clears all spawned rails.
        /// </summary>
        private void ClearRails()
        {
            foreach (var rail in _spawnedRails)
            {
                if (rail != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(rail);
                    }
                    else
                    {
                        DestroyImmediate(rail);
                    }
                }
            }
            _spawnedRails.Clear();
            
            if (_railsContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_railsContainer);
                }
                else
                {
                    DestroyImmediate(_railsContainer);
                }
                _railsContainer = null;
            }
        }
        
        /// <summary>
        /// Generates decorative signs scattered along the road edges.
        /// </summary>
        private void GenerateSigns()
        {
            ClearSigns();
            
            if (_signPrefab == null || _interpolatedPoints == null || _interpolatedPoints.Count < 2)
            {
                return;
            }
            
            // Create signs container
            _signsContainer = new GameObject("Signs");
            _signsContainer.transform.SetParent(transform);
            _signsContainer.transform.localPosition = Vector3.zero;
            _signsContainer.transform.localRotation = Quaternion.identity;
            _signsContainer.transform.localScale = Vector3.one;
            
            // Initialize random with seed
            System.Random random = _signRandomSeed != 0 
                ? new System.Random(_signRandomSeed) 
                : new System.Random();
            
            // Calculate total path length
            float totalLength = _interpolatedPoints[_interpolatedPoints.Count - 1].DistanceAlongPath;
            if (totalLength <= 0) return;
            
            // Generate on each enabled side
            if (_leftSigns)
            {
                GenerateScatteredDecorations(
                    _signPrefab, _signsContainer, _spawnedSigns, random,
                    _signSpacing, _signSpacingVariation,
                    _signMinEdgeOffset, _signMaxEdgeOffset,
                    _signVerticalOffset, _signScale, _signScaleVariation,
                    _signRotationVariation, _signsFaceRoad,
                    isLeftSide: true, namePrefix: "LeftSign"
                );
            }
            
            if (_rightSigns)
            {
                GenerateScatteredDecorations(
                    _signPrefab, _signsContainer, _spawnedSigns, random,
                    _signSpacing, _signSpacingVariation,
                    _signMinEdgeOffset, _signMaxEdgeOffset,
                    _signVerticalOffset, _signScale, _signScaleVariation,
                    _signRotationVariation, _signsFaceRoad,
                    isLeftSide: false, namePrefix: "RightSign"
                );
            }
        }
        
        /// <summary>
        /// Generates decorative grass scattered along the road edges.
        /// </summary>
        private void GenerateGrass()
        {
            ClearGrass();
            
            if (_grassPrefab == null || _interpolatedPoints == null || _interpolatedPoints.Count < 2)
            {
                return;
            }
            
            // Create grass container
            _grassContainer = new GameObject("Grass");
            _grassContainer.transform.SetParent(transform);
            _grassContainer.transform.localPosition = Vector3.zero;
            _grassContainer.transform.localRotation = Quaternion.identity;
            _grassContainer.transform.localScale = Vector3.one;
            
            // Initialize random with seed
            System.Random random = _grassRandomSeed != 0 
                ? new System.Random(_grassRandomSeed) 
                : new System.Random();
            
            // Calculate total path length
            float totalLength = _interpolatedPoints[_interpolatedPoints.Count - 1].DistanceAlongPath;
            if (totalLength <= 0) return;
            
            // Generate on each enabled side
            if (_leftGrass)
            {
                GenerateScatteredDecorations(
                    _grassPrefab, _grassContainer, _spawnedGrass, random,
                    _grassSpacing, _grassSpacingVariation,
                    _grassMinEdgeOffset, _grassMaxEdgeOffset,
                    _grassVerticalOffset, _grassScale, _grassScaleVariation,
                    _grassRotationVariation, faceRoad: false,
                    isLeftSide: true, namePrefix: "LeftGrass"
                );
            }
            
            if (_rightGrass)
            {
                GenerateScatteredDecorations(
                    _grassPrefab, _grassContainer, _spawnedGrass, random,
                    _grassSpacing, _grassSpacingVariation,
                    _grassMinEdgeOffset, _grassMaxEdgeOffset,
                    _grassVerticalOffset, _grassScale, _grassScaleVariation,
                    _grassRotationVariation, faceRoad: false,
                    isLeftSide: false, namePrefix: "RightGrass"
                );
            }
        }
        
        /// <summary>
        /// Generates scattered decorations along one side of the road.
        /// </summary>
        private void GenerateScatteredDecorations(
            GameObject prefab,
            GameObject container,
            List<GameObject> spawnedList,
            System.Random random,
            float baseSpacing,
            float spacingVariation,
            float minEdgeOffset,
            float maxEdgeOffset,
            float verticalOffset,
            Vector3 baseScale,
            float scaleVariation,
            float rotationVariation,
            bool faceRoad,
            bool isLeftSide,
            string namePrefix)
        {
            float totalLength = _interpolatedPoints[_interpolatedPoints.Count - 1].DistanceAlongPath;
            float currentDistance = 0f;
            int index = 0;
            
            while (currentDistance < totalLength)
            {
                // Add random variation to spacing
                float variation = (float)(random.NextDouble() * 2.0 - 1.0) * spacingVariation;
                float nextSpacing = Mathf.Max(0.5f, baseSpacing + variation);
                
                currentDistance += nextSpacing;
                if (currentDistance >= totalLength) break;
                
                // Get the road point at this distance
                float normalizedT = currentDistance / totalLength;
                var roadPoint = GetPointAlongPath(normalizedT);
                if (!roadPoint.HasValue) continue;
                
                // Calculate random edge offset
                float edgeOffset = minEdgeOffset + (float)random.NextDouble() * (maxEdgeOffset - minEdgeOffset);
                float halfWidth = roadPoint.Value.Width / 2f;
                
                // Calculate position offset from road center
                Vector3 lateralOffset;
                if (isLeftSide)
                {
                    lateralOffset = -roadPoint.Value.Right * (halfWidth + edgeOffset);
                }
                else
                {
                    lateralOffset = roadPoint.Value.Right * (halfWidth + edgeOffset);
                }
                
                Vector3 position = roadPoint.Value.Position + lateralOffset + roadPoint.Value.Up * verticalOffset;
                
                // Calculate rotation
                Quaternion rotation;
                if (faceRoad)
                {
                    // Face toward the road center
                    Vector3 toRoad = isLeftSide ? roadPoint.Value.Right : -roadPoint.Value.Right;
                    rotation = Quaternion.LookRotation(toRoad, roadPoint.Value.Up);
                }
                else
                {
                    // Random rotation around up axis
                    rotation = Quaternion.LookRotation(roadPoint.Value.Forward, roadPoint.Value.Up);
                }
                
                // Apply random rotation variation
                float randomYRotation = (float)(random.NextDouble() * 2.0 - 1.0) * rotationVariation;
                rotation *= Quaternion.Euler(0f, randomYRotation, 0f);
                
                // Calculate scale with variation
                float scaleMultiplier = 1f + (float)(random.NextDouble() * 2.0 - 1.0) * scaleVariation;
                Vector3 finalScale = baseScale * scaleMultiplier;
                
                // Spawn the decoration
                SpawnDecoration(prefab, container, spawnedList, position, rotation, finalScale, $"{namePrefix}_{index}");
                index++;
            }
        }
        
        /// <summary>
        /// Spawns a single decoration instance.
        /// </summary>
        private void SpawnDecoration(
            GameObject prefab,
            GameObject container,
            List<GameObject> spawnedList,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            string name)
        {
            GameObject instance;
            
            if (Application.isPlaying)
            {
                instance = Instantiate(prefab, position, rotation, container.transform);
            }
            else
            {
                #if UNITY_EDITOR
                instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, container.transform);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                #else
                instance = Instantiate(prefab, position, rotation, container.transform);
                #endif
            }
            
            instance.name = name;
            instance.transform.localScale = scale;
            spawnedList.Add(instance);
        }
        
        /// <summary>
        /// Clears all spawned signs.
        /// </summary>
        private void ClearSigns()
        {
            foreach (var sign in _spawnedSigns)
            {
                if (sign != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(sign);
                    }
                    else
                    {
                        DestroyImmediate(sign);
                    }
                }
            }
            _spawnedSigns.Clear();
            
            if (_signsContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_signsContainer);
                }
                else
                {
                    DestroyImmediate(_signsContainer);
                }
                _signsContainer = null;
            }
        }
        
        /// <summary>
        /// Clears all spawned grass.
        /// </summary>
        private void ClearGrass()
        {
            foreach (var grass in _spawnedGrass)
            {
                if (grass != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(grass);
                    }
                    else
                    {
                        DestroyImmediate(grass);
                    }
                }
            }
            _spawnedGrass.Clear();
            
            if (_grassContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_grassContainer);
                }
                else
                {
                    DestroyImmediate(_grassContainer);
                }
                _grassContainer = null;
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
            
            // Clear dirt layer, rails, signs, and grass
            ClearDirtLayer();
            ClearRails();
            ClearSigns();
            ClearGrass();
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
        /// For looping paths, the path is treated as circular (t=1.0 equals t=0.0).
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
            
            int pointCount = _interpolatedPoints.Count;
            float scaledT;
            int indexA, indexB;
            float lerpT;
            
            if (_isLoop)
            {
                // For loops, treat the path as circular
                // The path goes from point 0 to point N-1, then wraps back to point 0
                // So t=0 and t=1 should both return point 0
                scaledT = t * pointCount;
                indexA = Mathf.FloorToInt(scaledT) % pointCount;
                indexB = (indexA + 1) % pointCount;
                lerpT = scaledT - Mathf.FloorToInt(scaledT);
                
                // At exactly t=1.0, return the first point (same as t=0)
                if (t >= 1f)
                {
                    indexA = 0;
                    indexB = 1;
                    lerpT = 0f;
                }
            }
            else
            {
                // For non-loops, interpolate from first to last point
                scaledT = t * (pointCount - 1);
                indexA = Mathf.FloorToInt(scaledT);
                indexB = Mathf.CeilToInt(scaledT);
                
                // Clamp indices to valid range
                indexA = Mathf.Clamp(indexA, 0, pointCount - 1);
                indexB = Mathf.Clamp(indexB, 0, pointCount - 1);
                
                lerpT = scaledT - indexA;
            }
            
            // Get the two points to interpolate between
            var pointA = _interpolatedPoints[indexA];
            var pointB = _interpolatedPoints[indexB];
            
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
