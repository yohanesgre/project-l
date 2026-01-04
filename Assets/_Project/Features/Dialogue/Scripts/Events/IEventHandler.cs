using System;

namespace MyGame.Features.Dialogue.Events
{
    /// <summary>
    /// Interface for dialogue event handlers.
    /// Each handler processes a specific event type (e.g., bg, sfx, bgm).
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// The event type this handler processes (e.g., "bg", "sfx", "bgm").
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Executes the event with the given value.
        /// </summary>
        /// <param name="value">The event value (e.g., "cafe" for "bg:cafe").</param>
        /// <param name="onComplete">Callback to invoke when the event completes. Must be called!</param>
        void Execute(string value, Action onComplete);
    }
}
