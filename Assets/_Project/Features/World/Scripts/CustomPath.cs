using System.Collections.Generic;
using UnityEngine;
using MyGame.Core;

namespace MyGame.Features.World
{
    /// <summary>
    /// A standalone path for character movement, independent of road generation.
    /// Uses Catmull-Rom splines for smooth interpolation between control points.
    /// 
    /// Usage:
    /// 1. Add this component to a GameObject
    /// 2. Add control points via the inspector or by creating child GameObjects
    /// 3. Assign this path to a CharacterPathFollower
    /// </summary>
    [ExecuteInEditMode]
    public class CustomPath : MonoBehaviour, IPathProvider
    {
        #region Serialized Fields
        
        [Header("Path Points")]
        [Tooltip("Control points defining the path. If empty, child transforms will be used.")]
        [SerializeField] private List<Transform> _controlPoints = new List<Transform>();
        
        [Tooltip("If true, the path forms a closed loop.")]
        [SerializeField] private bool _isLoop = false;
        
        [Header("Path Settings")]
        [Tooltip("Number of interpolated segments between each control point pair. Higher = smoother path.")]
        [Range(1, 50)]
        [SerializeField] private int _segmentsPerPoint = 10;
        
        [Header("Generation")]
        [Tooltip("Automatically recalculate the path when control points change.")]
        [SerializeField] private bool _autoRecalculate = true;
        
        #endregion
        
        #region Private Fields
        
        private List<RoadMeshBuilder.RoadPoint> _interpolatedPoints;
        private float _totalLength;
        private bool _isDirty = true;
        
        #endregion
        
        #region IPathProvider Implementation
        
        /// <summary>
        /// Total length of the path in world units.
        /// </summary>
        public float TotalPathLength
        {
            get
            {
                EnsurePathCalculated();
                return _totalLength;
            }
        }
        
        /// <summary>
        /// Whether this path forms a closed loop.
        /// </summary>
        public bool IsLoop
        {
            get => _isLoop;
            set
            {
                if (_isLoop != value)
                {
                    _isLoop = value;
                    MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// Whether the path has valid data.
        /// </summary>
        public bool HasValidPath
        {
            get
            {
                EnsurePathCalculated();
                return _interpolatedPoints != null && _interpolatedPoints.Count >= 2;
            }
        }
        
        /// <summary>
        /// Gets an interpolated point along the path.
        /// </summary>
        public PathPoint? GetPointAlongPath(float t)
        {
            EnsurePathCalculated();
            
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
                scaledT = t * pointCount;
                indexA = Mathf.FloorToInt(scaledT) % pointCount;
                indexB = (indexA + 1) % pointCount;
                lerpT = scaledT - Mathf.FloorToInt(scaledT);
                
                if (t >= 1f)
                {
                    indexA = 0;
                    indexB = 1;
                    lerpT = 0f;
                }
            }
            else
            {
                scaledT = t * (pointCount - 1);
                indexA = Mathf.FloorToInt(scaledT);
                indexB = Mathf.CeilToInt(scaledT);
                
                indexA = Mathf.Clamp(indexA, 0, pointCount - 1);
                indexB = Mathf.Clamp(indexB, 0, pointCount - 1);
                
                lerpT = scaledT - indexA;
            }
            
            var pointA = _interpolatedPoints[indexA];
            var pointB = _interpolatedPoints[indexB];
            
            // Interpolate between the two points (already in world space)
            return new PathPoint(
                Vector3.Lerp(pointA.Position, pointB.Position, lerpT),
                Vector3.Slerp(pointA.Forward, pointB.Forward, lerpT).normalized,
                Vector3.Slerp(pointA.Right, pointB.Right, lerpT).normalized,
                Vector3.Slerp(pointA.Up, pointB.Up, lerpT).normalized,
                Mathf.Lerp(pointA.Width, pointB.Width, lerpT),
                Mathf.Lerp(pointA.DistanceAlongPath, pointB.DistanceAlongPath, lerpT)
            );
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Read-only access to the control points.
        /// </summary>
        public IReadOnlyList<Transform> ControlPoints => _controlPoints;
        
        /// <summary>
        /// Number of interpolation segments per control point pair.
        /// </summary>
        public int SegmentsPerPoint
        {
            get => _segmentsPerPoint;
            set
            {
                _segmentsPerPoint = Mathf.Max(1, value);
                MarkDirty();
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            MarkDirty();
        }
        
        private void Start()
        {
            RecalculatePath();
        }
        
        private void OnValidate()
        {
            _segmentsPerPoint = Mathf.Max(1, _segmentsPerPoint);
            
            if (_autoRecalculate)
            {
                MarkDirty();
            }
        }
        
        private void Update()
        {
            // In editor, check if control points have moved
            if (!Application.isPlaying && _autoRecalculate)
            {
                if (HaveControlPointsMoved())
                {
                    MarkDirty();
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Marks the path as needing recalculation.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            
            if (_autoRecalculate && Application.isEditor && !Application.isPlaying)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        RecalculatePath();
                    }
                };
                #endif
            }
        }
        
        /// <summary>
        /// Forces immediate recalculation of the path.
        /// </summary>
        [ContextMenu("Recalculate Path")]
        public void RecalculatePath()
        {
            _isDirty = false;
            _interpolatedPoints = null;
            _totalLength = 0f;
            
            // Collect control points
            List<Transform> activePoints = new List<Transform>(_controlPoints);
            
            // If no explicit points, use children
            if (activePoints.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    activePoints.Add(child);
                }
            }
            
            // Remove null entries
            activePoints.RemoveAll(t => t == null);
            
            if (activePoints.Count < 2)
            {
                return;
            }
            
            // Extract world positions
            List<Vector3> positions = new List<Vector3>();
            List<float> widths = new List<float>();
            
            foreach (var point in activePoints)
            {
                positions.Add(point.position);
                widths.Add(1f); // Constant width (not used by followers, required by RoadMeshBuilder)
            }
            
            // Use RoadMeshBuilder for interpolation (reuse existing spline logic)
            var localPoints = RoadMeshBuilder.InterpolateWaypoints(
                positions,
                widths,
                _segmentsPerPoint,
                _isLoop
            );
            
            // The interpolated points are already in the same space as input (world space)
            _interpolatedPoints = localPoints;
            
            // Calculate total length
            if (_interpolatedPoints.Count > 0)
            {
                _totalLength = _interpolatedPoints[_interpolatedPoints.Count - 1].DistanceAlongPath;
                
                if (_isLoop && _interpolatedPoints.Count > 1)
                {
                    _totalLength += Vector3.Distance(
                        _interpolatedPoints[_interpolatedPoints.Count - 1].Position,
                        _interpolatedPoints[0].Position
                    );
                }
            }
        }
        
        /// <summary>
        /// Adds a new control point at the specified world position.
        /// </summary>
        public Transform AddControlPoint(Vector3 worldPosition)
        {
            GameObject pointGO = new GameObject($"Point_{_controlPoints.Count}");
            pointGO.transform.SetParent(transform);
            pointGO.transform.position = worldPosition;
            
            _controlPoints.Add(pointGO.transform);
            MarkDirty();
            
            return pointGO.transform;
        }
        
        /// <summary>
        /// Collects child transforms as control points.
        /// </summary>
        [ContextMenu("Collect Child Points")]
        public void CollectChildPoints()
        {
            _controlPoints.Clear();
            foreach (Transform child in transform)
            {
                _controlPoints.Add(child);
            }
            MarkDirty();
        }
        
        /// <summary>
        /// Clears all control points.
        /// </summary>
        [ContextMenu("Clear Points")]
        public void ClearPoints()
        {
            _controlPoints.Clear();
            _interpolatedPoints = null;
            _totalLength = 0f;
        }
        
        #endregion
        
        #region Private Methods
        
        private void EnsurePathCalculated()
        {
            if (_isDirty || _interpolatedPoints == null)
            {
                RecalculatePath();
            }
        }
        
        private Vector3[] _lastPointPositions;
        
        private bool HaveControlPointsMoved()
        {
            List<Transform> activePoints = new List<Transform>(_controlPoints);
            if (activePoints.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    activePoints.Add(child);
                }
            }
            
            activePoints.RemoveAll(t => t == null);
            
            if (_lastPointPositions == null || _lastPointPositions.Length != activePoints.Count)
            {
                _lastPointPositions = new Vector3[activePoints.Count];
                for (int i = 0; i < activePoints.Count; i++)
                {
                    _lastPointPositions[i] = activePoints[i].position;
                }
                return true;
            }
            
            for (int i = 0; i < activePoints.Count; i++)
            {
                if (Vector3.Distance(_lastPointPositions[i], activePoints[i].position) > 0.001f)
                {
                    _lastPointPositions[i] = activePoints[i].position;
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Editor Gizmos
        
#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _pathColor = Color.cyan;
        [SerializeField] private Color _pointColor = Color.yellow;
        [SerializeField] private float _pointSize = 0.3f;
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            DrawControlPoints();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;
            
            DrawInterpolatedPath();
        }
        
        private void DrawControlPoints()
        {
            List<Transform> activePoints = new List<Transform>(_controlPoints);
            if (activePoints.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    activePoints.Add(child);
                }
            }
            
            activePoints.RemoveAll(t => t == null);
            
            if (activePoints.Count < 2) return;
            
            // Draw control point spheres and connections
            Gizmos.color = _pointColor;
            for (int i = 0; i < activePoints.Count; i++)
            {
                Gizmos.DrawWireSphere(activePoints[i].position, _pointSize);
                
                // Draw connection line
                if (i < activePoints.Count - 1)
                {
                    Gizmos.color = new Color(_pathColor.r, _pathColor.g, _pathColor.b, 0.3f);
                    Gizmos.DrawLine(activePoints[i].position, activePoints[i + 1].position);
                    Gizmos.color = _pointColor;
                }
            }
            
            // Draw loop closing line
            if (_isLoop && activePoints.Count > 1)
            {
                Gizmos.color = new Color(_pathColor.r, _pathColor.g, _pathColor.b, 0.3f);
                Gizmos.DrawLine(activePoints[activePoints.Count - 1].position, activePoints[0].position);
            }
            
            // Draw point numbers
            for (int i = 0; i < activePoints.Count; i++)
            {
                UnityEditor.Handles.Label(
                    activePoints[i].position + Vector3.up * (_pointSize + 0.3f),
                    i.ToString(),
                    new GUIStyle { normal = { textColor = Color.white }, fontSize = 12, fontStyle = FontStyle.Bold }
                );
            }
        }
        
        private void DrawInterpolatedPath()
        {
            EnsurePathCalculated();
            
            if (_interpolatedPoints == null || _interpolatedPoints.Count < 2) return;
            
            Gizmos.color = _pathColor;
            
            for (int i = 0; i < _interpolatedPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(_interpolatedPoints[i].Position, _interpolatedPoints[i + 1].Position);
            }
            
            if (_isLoop && _interpolatedPoints.Count > 1)
            {
                Gizmos.DrawLine(
                    _interpolatedPoints[_interpolatedPoints.Count - 1].Position,
                    _interpolatedPoints[0].Position
                );
            }
            
            // Draw direction indicators at intervals
            int stepSize = Mathf.Max(1, _interpolatedPoints.Count / 10);
            for (int i = 0; i < _interpolatedPoints.Count; i += stepSize)
            {
                var point = _interpolatedPoints[i];
                
                // Forward arrow
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(point.Position, point.Forward * 0.5f);
                
                // Right indicator
                Gizmos.color = Color.red;
                Gizmos.DrawRay(point.Position, point.Right * 0.25f);
            }
        }
#endif
        
        #endregion
    }
}
