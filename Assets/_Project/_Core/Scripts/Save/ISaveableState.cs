namespace MyGame.Core.Save
{
    /// <summary>
    /// Interface for any system that can save and restore its state.
    /// Implement this interface to make your feature saveable via SaveController.
    /// </summary>
    public interface ISaveableState
    {
        /// <summary>
        /// Whether the current state can be saved (e.g., is active and valid).
        /// </summary>
        bool CanSave { get; }

        /// <summary>
        /// Captures the current state into a SaveStateData object.
        /// </summary>
        /// <returns>The current state data, or null if unable to capture.</returns>
        SaveStateData GetCurrentState();

        /// <summary>
        /// Restores state from the provided save data.
        /// </summary>
        /// <param name="data">The save data to restore from.</param>
        /// <returns>True if restore was successful.</returns>
        bool RestoreState(SaveStateData data);
    }
}
