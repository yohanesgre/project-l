using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using MyGame.Core.Audio;

namespace MyGame.Features.Dialogue.UI
{
    /// <summary>
    /// Typewriter effect for UI Toolkit Labels.
    /// Reveals text character by character with configurable speed.
    /// </summary>
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Time between each character reveal")]
        [SerializeField] private float defaultCharacterDelay = 0.04f;

        [Tooltip("Pause duration for punctuation marks")]
        [SerializeField] private float punctuationDelay = 0.15f;

        [Tooltip("Characters that trigger punctuation delay")]
        [SerializeField] private string punctuationChars = ".,!?;:";

        [Header("Sound")]
        [Tooltip("SFX key for typewriter sound")]
        [SerializeField] private string typewriterSFXKey = "typewriter";

        [Tooltip("Volume scale for typewriter sound (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float typewriterVolume = 0.5f;

        public event Action OnTypewriterStarted;
        public event Action OnTypewriterCompleted;
        public event Action<char> OnCharacterRevealed;

        private Label _targetLabel;
        private string _fullText;
        private int _currentIndex;
        private bool _isTyping;
        private bool _skipRequested;
        private Coroutine _typingCoroutine;
        private float _currentCharacterDelay;
        private AudioSource _typewriterSFXSource;

        public bool IsTyping => _isTyping;
        public float CharacterDelay
        {
            get => _currentCharacterDelay;
            set => _currentCharacterDelay = Mathf.Clamp(value, 0.001f, 0.5f);
        }

        private void Awake()
        {
            _currentCharacterDelay = defaultCharacterDelay;
        }

        public void StartTyping(Label label, string text)
        {
            if (label == null || string.IsNullOrEmpty(text)) return;

            StopTyping();
            _targetLabel = label;
            _fullText = text;
            _currentIndex = 0;
            _skipRequested = false;
            _isTyping = true;
            _targetLabel.text = "";

            OnTypewriterStarted?.Invoke();
            _typingCoroutine = StartCoroutine(TypeTextCoroutine());
        }

        public void Skip()
        {
            if (_isTyping) _skipRequested = true;
        }

        public void StopTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
            _isTyping = false;
            StopTypewriterSFX();
        }

        public void ShowAllText()
        {
            StopTyping();
            if (_targetLabel != null && !string.IsNullOrEmpty(_fullText))
                _targetLabel.text = _fullText;
            StopTypewriterSFX();
            OnTypewriterCompleted?.Invoke();
        }

        private IEnumerator TypeTextCoroutine()
        {
            // Start typewriter SFX (looping) when typing begins
            PlayTypewriterSFX();

            while (_currentIndex < _fullText.Length)
            {
                if (_skipRequested)
                {
                    _targetLabel.text = _fullText;
                    break;
                }

                char currentChar = _fullText[_currentIndex];
                _currentIndex++;
                _targetLabel.text = _fullText.Substring(0, _currentIndex);
                OnCharacterRevealed?.Invoke(currentChar);

                float delay = _currentCharacterDelay;
                if (punctuationChars.Contains(currentChar.ToString()))
                    delay = punctuationDelay;
                else if (currentChar == ' ')
                    delay = _currentCharacterDelay * 0.5f;

                yield return new WaitForSeconds(delay);
            }

            _isTyping = false;
            _typingCoroutine = null;
            StopTypewriterSFX();
            OnTypewriterCompleted?.Invoke();
        }

        private void PlayTypewriterSFX()
        {
            if (string.IsNullOrEmpty(typewriterSFXKey)) return;
            if (AudioManager.Instance == null) return;

            // Only start if not already playing
            if (_typewriterSFXSource == null || !_typewriterSFXSource.isPlaying)
            {
                _typewriterSFXSource = AudioManager.Instance.PlayLoopingSFX(typewriterSFXKey, 0f, typewriterVolume);
            }
        }

        private void StopTypewriterSFX()
        {
            if (_typewriterSFXSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoopingSFX(_typewriterSFXSource, 0.1f);
                _typewriterSFXSource = null;
            }
        }

        public void SetSpeedNormalized(float normalizedSpeed)
        {
            _currentCharacterDelay = Mathf.Lerp(0.1f, 0.01f, normalizedSpeed);
        }
    }
}
