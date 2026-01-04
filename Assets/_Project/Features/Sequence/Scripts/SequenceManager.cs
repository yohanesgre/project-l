using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Features.Sequence
{
    /// <summary>
    /// Executes a MasterSequence of steps (Scenes and Actions).
    /// </summary>
    public class SequenceManager : MonoBehaviour
    {
        [Tooltip("List of available sequences. The one at '_startIndex' will play on start if enabled.")]
        [SerializeField] private List<MasterSequence> _sequences = new List<MasterSequence>();
        
        [Tooltip("Index of the sequence to play on start.")]
        [SerializeField] private int _startIndex = 0;
        
        [SerializeField] private bool _playOnStart = true;

        private MasterSequence _currentSequence;
        private Coroutine _sequenceCoroutine;
        private bool _isPlaying;

        public List<MasterSequence> Sequences => _sequences;

        private void Start()
        {
            if (_playOnStart && _sequences.Count > 0)
            {
                if (_startIndex >= 0 && _startIndex < _sequences.Count)
                {
                    if (_sequences[_startIndex] != null)
                    {
                        // Signal that we are in control
                        MyGame.Features.Character.CharacterActionManager.IsMasterSequenceActive = true;
                        PlaySequence(_sequences[_startIndex]);
                    }
                }
                else
                {
                    Debug.LogWarning($"[SequenceManager] Start index {_startIndex} is out of range (0-{_sequences.Count-1})");
                }
            }
        }

        public void PlaySequenceAtIndex(int index)
        {
            if (index >= 0 && index < _sequences.Count)
            {
                if (_sequences[index] != null)
                {
                    PlaySequence(_sequences[index]);
                }
                else
                {
                    Debug.LogWarning($"[SequenceManager] Sequence at index {index} is null.");
                }
            }
            else
            {
                Debug.LogError($"[SequenceManager] Invalid sequence index: {index}");
            }
        }

        public void PlaySequence(MasterSequence sequence)
        {
            if (_isPlaying)
            {
                StopSequence();
            }

            MyGame.Features.Character.CharacterActionManager.IsMasterSequenceActive = true;
            _currentSequence = sequence;
            _sequenceCoroutine = StartCoroutine(RunSequenceRoutine());
        }

        public void StopSequence()
        {
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
                _sequenceCoroutine = null;
            }
            _isPlaying = false;
            MyGame.Features.Character.CharacterActionManager.IsMasterSequenceActive = false;
        }

        private IEnumerator RunSequenceRoutine()
        {
            if (_currentSequence == null) yield break;

            _isPlaying = true;
            Debug.Log($"[SequenceManager] Starting Master Sequence: {_currentSequence.name}");

            do
            {
                foreach (var step in _currentSequence.steps)
                {
                    if (step != null)
                    {
                        Debug.Log($"[SequenceManager] Executing Step: {step.stepName}");
                        yield return step.Execute(this);
                    }
                }
            } 
            while (_currentSequence.loopSequence && _isPlaying);

            Debug.Log($"[SequenceManager] Master Sequence Complete: {_currentSequence.name}");
            _isPlaying = false;
        }
    }
}
