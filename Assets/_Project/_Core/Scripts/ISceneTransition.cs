using System.Collections;
using UnityEngine;

namespace MyGame.Core
{
    /// <summary>
    /// Interface for custom scene transition effects.
    /// Implement this to create custom transition animations (wipes, dissolves, etc.)
    /// </summary>
    public interface ISceneTransition
    {
        /// <summary>
        /// Initialize the transition effect. Called once before first use.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Executes the "out" transition (hiding the current scene).
        /// </summary>
        /// <param name="duration">How long the transition should take.</param>
        /// <returns>Coroutine that completes when transition is done.</returns>
        IEnumerator TransitionOut(float duration);
        
        /// <summary>
        /// Executes the "in" transition (revealing the new scene).
        /// </summary>
        /// <param name="duration">How long the transition should take.</param>
        /// <returns>Coroutine that completes when transition is done.</returns>
        IEnumerator TransitionIn(float duration);
        
        /// <summary>
        /// Clean up resources used by the transition.
        /// </summary>
        void Cleanup();
    }
}
