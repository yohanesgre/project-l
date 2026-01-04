namespace Runtime
{
    /// <summary>
    /// Represents the current state of the visual novel game loop.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Main menu / title screen. Waiting for player input to start or continue.
        /// </summary>
        Title,

        /// <summary>
        /// Loading transition state. Used during save/load or scene transitions.
        /// </summary>
        Loading,

        /// <summary>
        /// Active gameplay. Dialogue is running and player can interact.
        /// </summary>
        Playing,

        /// <summary>
        /// Game is paused. In-game menu is active (save/load/settings).
        /// </summary>
        Paused
    }
}
