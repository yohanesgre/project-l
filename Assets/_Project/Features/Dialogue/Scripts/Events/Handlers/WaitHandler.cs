using System;
using System.Collections;
using UnityEngine;

namespace MyGame.Features.Dialogue.Events
{
    /// <summary>
    /// Handles wait/delay events (wait:seconds).
    /// Pauses dialogue progression for the specified duration.
    /// </summary>
    public class WaitHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "wait";

        public void Execute(string value, Action onComplete)
        {
            if (float.TryParse(value, out float duration) && duration > 0)
            {
                StartCoroutine(WaitCoroutine(duration, onComplete));
            }
            else
            {
                Debug.LogWarning($"[WaitHandler] Invalid wait duration: {value}");
                onComplete?.Invoke();
            }
        }

        private IEnumerator WaitCoroutine(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Handles screen shake events (shake:intensity or shake:intensity,duration).
    /// </summary>
    public class ShakeHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "shake";

        [Header("References")]
        [Tooltip("Transform to shake (usually the main camera or UI canvas)")]
        [SerializeField] private Transform targetTransform;

        [Header("Settings")]
        [Tooltip("Default shake duration if not specified")]
        [SerializeField] private float defaultDuration = 0.5f;

        [Tooltip("Default shake intensity")]
        [SerializeField] private float defaultIntensity = 0.3f;

        private Vector3 _originalPosition;

        private void Awake()
        {
            if (targetTransform == null)
            {
                targetTransform = Camera.main?.transform;
            }
        }

        public void Execute(string value, Action onComplete)
        {
            float intensity = defaultIntensity;
            float duration = defaultDuration;

            // Parse value: "intensity" or "intensity,duration"
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(',');
                if (parts.Length >= 1)
                {
                    float.TryParse(parts[0].Trim(), out intensity);
                }
                if (parts.Length >= 2)
                {
                    float.TryParse(parts[1].Trim(), out duration);
                }
            }

            if (targetTransform == null)
            {
                Debug.LogWarning("[ShakeHandler] No target transform assigned");
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(ShakeCoroutine(intensity, duration, onComplete));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration, Action onComplete)
        {
            _originalPosition = targetTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity;

                targetTransform.localPosition = _originalPosition + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            targetTransform.localPosition = _originalPosition;
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Handles fade events (fade:in,duration or fade:out,duration).
    /// </summary>
    public class FadeHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "fade";

        [Header("References")]
        [Tooltip("CanvasGroup or Image to use for fading")]
        [SerializeField] private CanvasGroup fadeOverlay;

        [Header("Settings")]
        [Tooltip("Default fade duration")]
        [SerializeField] private float defaultDuration = 1f;

        public void Execute(string value, Action onComplete)
        {
            if (fadeOverlay == null)
            {
                Debug.LogWarning("[FadeHandler] No fade overlay assigned");
                onComplete?.Invoke();
                return;
            }

            bool fadeIn = true;
            float duration = defaultDuration;

            // Parse value: "in,duration" or "out,duration" or just "in"/"out"
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(',');
                if (parts.Length >= 1)
                {
                    fadeIn = parts[0].Trim().Equals("in", StringComparison.OrdinalIgnoreCase);
                }
                if (parts.Length >= 2)
                {
                    float.TryParse(parts[1].Trim(), out duration);
                }
            }

            StartCoroutine(FadeCoroutine(fadeIn, duration, onComplete));
        }

        private IEnumerator FadeCoroutine(bool fadeIn, float duration, Action onComplete)
        {
            float startAlpha = fadeIn ? 1f : 0f;
            float endAlpha = fadeIn ? 0f : 1f;
            float elapsed = 0f;

            fadeOverlay.alpha = startAlpha;
            fadeOverlay.blocksRaycasts = true;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }

            fadeOverlay.alpha = endAlpha;
            fadeOverlay.blocksRaycasts = !fadeIn; // Block input during fade out

            onComplete?.Invoke();
        }
    }
}
