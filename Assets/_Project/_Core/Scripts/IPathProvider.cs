namespace MyGame.Core
{
    /// <summary>
    /// Interface for any component that can provide path data for followers.
    /// Allows decoupling of path sources (roads, custom paths, etc.) from followers.
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>
        /// Gets the total length of the path in world units.
        /// For looped paths, this includes the closing segment.
        /// </summary>
        float TotalPathLength { get; }
        
        /// <summary>
        /// Whether the path forms a closed loop.
        /// </summary>
        bool IsLoop { get; }
        
        /// <summary>
        /// Whether the path has valid data for following.
        /// </summary>
        bool HasValidPath { get; }
        
        /// <summary>
        /// Gets an interpolated point along the path at the specified normalized position.
        /// </summary>
        /// <param name="t">Normalized position along the path (0 = start, 1 = end).</param>
        /// <returns>The interpolated path point in world space, or null if no valid path.</returns>
        PathPoint? GetPointAlongPath(float t);
    }
}
