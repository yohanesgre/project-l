using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Features.Sequence
{
    [CreateAssetMenu(fileName = "NewMasterSequence", menuName = "MyGame/Sequence/Master Sequence")]
    public class MasterSequence : ScriptableObject
    {
        [TextArea]
        public string description;
        
        public List<SequenceStep> steps = new List<SequenceStep>();
        
        public bool loopSequence = false;
    }
}
