using System.Collections.Generic;
using UnityEngine;
using MyGame.Core;

namespace MyGame.Features.World
{
    /// <summary>
    /// Utility class for building road meshes from path data.
    /// Handles mesh generation, UV mapping, and normals.
    /// </summary>
    public static class RoadMeshBuilder
    {
        /// <summary>
        /// Represents a point along the road path with all necessary data for mesh generation.
        /// This is now an alias for PathPoint from Core for backward compatibility.
        /// </summary>
        public struct RoadPoint
        {
            public Vector3 Position;
            public Vector3 Forward;
            public Vector3 Right;
            public Vector3 Up;
            public float Width;
            public float DistanceAlongPath;
            
            /// <summary>
            /// Converts a PathPoint to a RoadPoint.
            /// </summary>
            public static implicit operator RoadPoint(PathPoint pathPoint)
            {
                return new RoadPoint
                {
                    Position = pathPoint.Position,
                    Forward = pathPoint.Forward,
                    Right = pathPoint.Right,
                    Up = pathPoint.Up,
                    Width = pathPoint.Width,
                    DistanceAlongPath = pathPoint.DistanceAlongPath
                };
            }
            
            /// <summary>
            /// Converts a RoadPoint to a PathPoint.
            /// </summary>
            public static implicit operator PathPoint(RoadPoint roadPoint)
            {
                return new PathPoint(
                    roadPoint.Position,
                    roadPoint.Forward,
                    roadPoint.Right,
                    roadPoint.Up,
                    roadPoint.Width,
                    roadPoint.DistanceAlongPath
                );
            }
        }
        
        /// <summary>
        /// Generates a road mesh from a list of road points.
        /// </summary>
        /// <param name="points">The interpolated points along the road path.</param>
        /// <param name="uvTiling">How many times the texture tiles along the road length.</param>
        /// <param name="isLoop">Whether the road forms a closed loop (connects last to first).</param>
        /// <returns>A new Mesh representing the road surface.</returns>
        public static Mesh GenerateRoadMesh(List<RoadPoint> points, float uvTiling = 1f, bool isLoop = false)
        {
            if (points == null || points.Count < 2)
            {
                Debug.LogWarning("RoadMeshBuilder: Need at least 2 points to generate a road mesh.");
                return null;
            }
            
            Mesh mesh = new Mesh();
            mesh.name = "GeneratedRoad";
            
            int pointCount = points.Count;
            
            // For loops, we add 2 extra vertices for the closing segment that duplicate 
            // the first point's position but with proper UVs for seamless tiling
            int vertexCount = isLoop ? (pointCount + 1) * 2 : pointCount * 2;
            int segmentCount = pointCount - 1 + (isLoop ? 1 : 0);
            int triangleCount = segmentCount * 6;
            
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[triangleCount];
            
            // Calculate total path length for UV mapping
            float totalLength = points[points.Count - 1].DistanceAlongPath;
            
            // For loops, add the distance of the closing segment
            if (isLoop)
            {
                totalLength += Vector3.Distance(points[points.Count - 1].Position, points[0].Position);
            }
            
            if (totalLength <= 0) totalLength = 1f;
            
            // Generate vertices (two per road point - left and right edges)
            for (int i = 0; i < pointCount; i++)
            {
                RoadPoint point = points[i];
                float halfWidth = point.Width / 2f;
                
                // Left vertex
                int leftIndex = i * 2;
                vertices[leftIndex] = point.Position - point.Right * halfWidth;
                normals[leftIndex] = point.Up;
                
                // Right vertex
                int rightIndex = i * 2 + 1;
                vertices[rightIndex] = point.Position + point.Right * halfWidth;
                normals[rightIndex] = point.Up;
                
                // UVs: U goes across width (0 = left, 1 = right), V goes along length
                float v = (point.DistanceAlongPath / totalLength) * uvTiling;
                uvs[leftIndex] = new Vector2(0f, v);
                uvs[rightIndex] = new Vector2(1f, v);
            }
            
            // For loops, add closing vertices at the EXACT same positions as the first point's vertices
            // This ensures the mesh closes seamlessly with no visible gap
            // The only difference is the UVs continue from the end of the path for proper texture tiling
            if (isLoop)
            {
                RoadPoint firstPoint = points[0];
                float halfWidth = firstPoint.Width / 2f;
                
                int closingLeftIndex = pointCount * 2;
                int closingRightIndex = pointCount * 2 + 1;
                
                // Use the FIRST point's Right vector so positions match exactly with vertices 0 and 1
                // This closes the gap - the closing vertices are at the same world position as the first vertices
                vertices[closingLeftIndex] = firstPoint.Position - firstPoint.Right * halfWidth;
                vertices[closingRightIndex] = firstPoint.Position + firstPoint.Right * halfWidth;
                normals[closingLeftIndex] = firstPoint.Up;
                normals[closingRightIndex] = firstPoint.Up;
                
                // UV continues from the last point for seamless texture tiling
                float closingV = uvTiling; // Full loop = full tiling
                uvs[closingLeftIndex] = new Vector2(0f, closingV);
                uvs[closingRightIndex] = new Vector2(1f, closingV);
            }
            
            // Generate triangles (two per segment forming a quad)
            int triIndex = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                int bl = i * 2;       // Bottom-left
                int br = i * 2 + 1;   // Bottom-right
                int tl = (i + 1) * 2;     // Top-left
                int tr = (i + 1) * 2 + 1; // Top-right
                
                // First triangle (bottom-left, top-left, top-right)
                triangles[triIndex++] = bl;
                triangles[triIndex++] = tl;
                triangles[triIndex++] = tr;
                
                // Second triangle (bottom-left, top-right, bottom-right)
                triangles[triIndex++] = bl;
                triangles[triIndex++] = tr;
                triangles[triIndex++] = br;
            }
            
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            
            return mesh;
        }
        
        /// <summary>
        /// Interpolates between waypoints using Catmull-Rom splines.
        /// </summary>
        /// <param name="waypoints">List of waypoint positions.</param>
        /// <param name="widths">List of widths at each waypoint.</param>
        /// <param name="segmentsPerWaypoint">Number of segments between each waypoint pair.</param>
        /// <param name="isLoop">Whether the path should form a closed loop.</param>
        /// <returns>List of interpolated road points.</returns>
        public static List<RoadPoint> InterpolateWaypoints(
            List<Vector3> waypoints, 
            List<float> widths, 
            int segmentsPerWaypoint, 
            bool isLoop)
        {
            if (waypoints == null || waypoints.Count < 2)
            {
                return new List<RoadPoint>();
            }
            
            List<RoadPoint> result = new List<RoadPoint>();
            float totalDistance = 0f;
            
            int waypointCount = waypoints.Count;
            int iterations = isLoop ? waypointCount : waypointCount - 1;
            
            for (int i = 0; i < iterations; i++)
            {
                // Get four points for Catmull-Rom interpolation
                Vector3 p0, p1, p2, p3;
                float w1, w2;
                
                if (isLoop)
                {
                    p0 = waypoints[(i - 1 + waypointCount) % waypointCount];
                    p1 = waypoints[i];
                    p2 = waypoints[(i + 1) % waypointCount];
                    p3 = waypoints[(i + 2) % waypointCount];
                    
                    w1 = widths[i];
                    w2 = widths[(i + 1) % waypointCount];
                }
                else
                {
                    // For open paths, clamp indices
                    p0 = waypoints[Mathf.Max(0, i - 1)];
                    p1 = waypoints[i];
                    p2 = waypoints[Mathf.Min(waypointCount - 1, i + 1)];
                    p3 = waypoints[Mathf.Min(waypointCount - 1, i + 2)];
                    
                    w1 = widths[i];
                    w2 = widths[Mathf.Min(widths.Count - 1, i + 1)];
                }
                
                // Generate points along this segment
                // For non-loop paths, the last segment needs to include t=1.0 (the final waypoint)
                // For loop paths, we never include t=1.0 here because t=1.0 of segment i
                // is the same position as t=0.0 of segment i+1
                int steps = (i == iterations - 1 && !isLoop) ? segmentsPerWaypoint + 1 : segmentsPerWaypoint;
                
                for (int s = 0; s < steps; s++)
                {
                    float t = (float)s / segmentsPerWaypoint;
                    
                    Vector3 position = CatmullRom(p0, p1, p2, p3, t);
                    Vector3 tangent = CatmullRomDerivative(p0, p1, p2, p3, t).normalized;
                    
                    // Handle edge cases where tangent might be zero
                    if (tangent.sqrMagnitude < 0.001f)
                    {
                        tangent = (p2 - p1).normalized;
                        if (tangent.sqrMagnitude < 0.001f)
                        {
                            tangent = Vector3.forward;
                        }
                    }
                    
                    Vector3 up = Vector3.up;
                    Vector3 right = Vector3.Cross(up, tangent).normalized;
                    
                    // Recalculate up to be perpendicular to both forward and right
                    up = Vector3.Cross(tangent, right).normalized;
                    
                    // Calculate distance from previous point
                    if (result.Count > 0)
                    {
                        totalDistance += Vector3.Distance(result[result.Count - 1].Position, position);
                    }
                    
                    result.Add(new RoadPoint
                    {
                        Position = position,
                        Forward = tangent,
                        Right = right,
                        Up = up,
                        Width = Mathf.Lerp(w1, w2, t),
                        DistanceAlongPath = totalDistance
                    });
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Catmull-Rom spline interpolation.
        /// </summary>
        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
        
        /// <summary>
        /// Catmull-Rom spline derivative (for tangent calculation).
        /// </summary>
        private static Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            
            return 0.5f * (
                (-p0 + p2) +
                2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
                3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2
            );
        }
    }
}
