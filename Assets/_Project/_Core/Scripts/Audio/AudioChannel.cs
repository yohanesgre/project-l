namespace MyGame.Core.Audio
{
    /// <summary>
    /// Audio volume channels for independent volume control.
    /// </summary>
    public enum AudioChannel
    {
        /// <summary>
        /// Master volume - affects all audio.
        /// </summary>
        Master,

        /// <summary>
        /// Background music volume.
        /// </summary>
        BGM,

        /// <summary>
        /// Sound effects volume.
        /// </summary>
        SFX
    }
}
