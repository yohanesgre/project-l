using System.Collections;
using UnityEngine;

namespace MyGame.Features.Sequence
{
    [CreateAssetMenu(fileName = "WaitStep", menuName = "MyGame/Sequence/Steps/Wait")]
    public class WaitStep : SequenceStep
    {
        public float duration = 1f;

        public override IEnumerator Execute(SequenceManager manager)
        {
            yield return new WaitForSeconds(duration);
        }
    }
}
