namespace MyGame.Core
{
    /// <summary>
    /// Interface for components that follow a path.
    /// Allows the camera and other systems to track path followers without tight coupling.
    /// </summary>
    public interface IPathFollower
    {
        /// <summary>
        /// The path provider this follower is using.
        /// </summary>
        IPathProvider PathProvider { get; }
        
        /// <summary>
        /// Current normalized progress along the path (0 = start, 1 = end).
        /// </summary>
        float Progress { get; }
    }
}
