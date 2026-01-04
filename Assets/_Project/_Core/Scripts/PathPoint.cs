using UnityEngine;

namespace MyGame.Core
{
    /// <summary>
    /// Represents a point along a path with all necessary data for positioning and orientation.
    /// Used by IPathProvider implementations to describe path geometry.
    /// </summary>
    public struct PathPoint
    {
        /// <summary>
        /// The world position of this point on the path.
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// The forward direction (tangent) at this point.
        /// </summary>
        public Vector3 Forward;
        
        /// <summary>
        /// The right direction (perpendicular to forward and up).
        /// </summary>
        public Vector3 Right;
        
        /// <summary>
        /// The up direction (normal) at this point.
        /// </summary>
        public Vector3 Up;
        
        /// <summary>
        /// The width of the path at this point (optional, used for roads/tracks).
        /// </summary>
        public float Width;
        
        /// <summary>
        /// The accumulated distance along the path from the start to this point.
        /// </summary>
        public float DistanceAlongPath;
        
        /// <summary>
        /// Creates a new PathPoint with the specified values.
        /// </summary>
        public PathPoint(Vector3 position, Vector3 forward, Vector3 right, Vector3 up, float width = 1f, float distanceAlongPath = 0f)
        {
            Position = position;
            Forward = forward;
            Right = right;
            Up = up;
            Width = width;
            DistanceAlongPath = distanceAlongPath;
        }
    }
}
