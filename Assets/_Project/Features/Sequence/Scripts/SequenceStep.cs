using System.Collections;
using UnityEngine;

namespace MyGame.Features.Sequence
{
    /// <summary>
    /// Base class for a single step in a Master Sequence.
    /// </summary>
    public abstract class SequenceStep : ScriptableObject
    {
        /// <summary>
        /// Executes the step. Returns an IEnumerator for coroutine execution.
        /// </summary>
        /// <param name="manager">The SequenceManager context</param>
        public abstract IEnumerator Execute(SequenceManager manager);
    }
}
