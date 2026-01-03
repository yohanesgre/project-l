using System;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime
{
    /// <summary>
    /// Handles background change events (bg:scene_name).
    /// Loads backgrounds from Resources/Backgrounds/ folder.
    /// </summary>
    public class BackgroundHandler : MonoBehaviour, IEventHandler
    {
        public string EventType => "bg";

        [Header("References")]
        [Tooltip("The Image component to display backgrounds")]
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [Tooltip("Path in Resources folder where backgrounds are stored")]
        [SerializeField] private string resourcePath = "Backgrounds";

        [Tooltip("Fade duration when changing backgrounds (0 = instant)")]
        [SerializeField] private float fadeDuration = 0.5f;

        public void Execute(string value, Action onComplete)
        {
            if (backgroundImage == null)
            {
                Debug.LogWarning("[BackgroundHandler] No background Image assigned");
                onComplete?.Invoke();
                return;
            }

            var path = string.IsNullOrEmpty(resourcePath) 
                ? value 
                : $"{resourcePath}/{value}";

            var sprite = Resources.Load<Sprite>(path);

            if (sprite == null)
            {
                Debug.LogWarning($"[BackgroundHandler] Background not found: {path}");
                onComplete?.Invoke();
                return;
            }

            if (fadeDuration > 0)
            {
                StartCoroutine(FadeToBackground(sprite, onComplete));
            }
            else
            {
                backgroundImage.sprite = sprite;
                onComplete?.Invoke();
            }
        }

        private System.Collections.IEnumerator FadeToBackground(Sprite newSprite, Action onComplete)
        {
            float elapsed = 0;
            Color color = backgroundImage.color;
            float halfDuration = fadeDuration / 2;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
                backgroundImage.color = color;
                yield return null;
            }

            backgroundImage.sprite = newSprite;

            elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
                backgroundImage.color = color;
                yield return null;
            }

            color.a = 1f;
            backgroundImage.color = color;
            onComplete?.Invoke();
        }
    }
}
