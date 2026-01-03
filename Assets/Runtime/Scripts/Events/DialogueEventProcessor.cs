using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    /// <summary>
    /// Processes dialogue event tags and dispatches to appropriate handlers.
    /// Event tags are pipe-separated: "bg:cafe|sfx:door|wait:2"
    /// </summary>
    public class DialogueEventProcessor : MonoBehaviour
    {
        [Header("Handlers")]
        [Tooltip("Event handlers to use. If empty, will auto-discover from children.")]
        [SerializeField] private List<MonoBehaviour> handlers = new List<MonoBehaviour>();

        private Dictionary<string, IEventHandler> _handlerMap;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the event processor and discovers handlers.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _handlerMap = new Dictionary<string, IEventHandler>(StringComparer.OrdinalIgnoreCase);

            // Register handlers from serialized list
            foreach (var handler in handlers)
            {
                if (handler is IEventHandler eventHandler)
                {
                    RegisterHandler(eventHandler);
                }
            }

            // Auto-discover handlers from children
            var childHandlers = GetComponentsInChildren<IEventHandler>();
            foreach (var handler in childHandlers)
            {
                if (!_handlerMap.ContainsKey(handler.EventType))
                {
                    RegisterHandler(handler);
                }
            }

            _isInitialized = true;
            Debug.Log($"[DialogueEventProcessor] Initialized with {_handlerMap.Count} handlers");
        }

        /// <summary>
        /// Registers an event handler.
        /// </summary>
        /// <param name="handler">The handler to register.</param>
        public void RegisterHandler(IEventHandler handler)
        {
            if (handler == null) return;

            var eventType = handler.EventType;
            if (string.IsNullOrEmpty(eventType))
            {
                Debug.LogWarning($"[DialogueEventProcessor] Handler has empty EventType: {handler}");
                return;
            }

            if (_handlerMap == null)
            {
                _handlerMap = new Dictionary<string, IEventHandler>(StringComparer.OrdinalIgnoreCase);
            }

            _handlerMap[eventType] = handler;
            Debug.Log($"[DialogueEventProcessor] Registered handler: {eventType}");
        }

        /// <summary>
        /// Unregisters an event handler.
        /// </summary>
        /// <param name="eventType">The event type to unregister.</param>
        public void UnregisterHandler(string eventType)
        {
            _handlerMap?.Remove(eventType);
        }

        /// <summary>
        /// Processes an event tag string and executes all events sequentially.
        /// </summary>
        /// <param name="eventTag">Pipe-separated event tags (e.g., "bg:cafe|sfx:door").</param>
        /// <param name="onAllComplete">Callback when all events have completed.</param>
        public void ProcessEventTag(string eventTag, Action onAllComplete = null)
        {
            if (string.IsNullOrEmpty(eventTag))
            {
                onAllComplete?.Invoke();
                return;
            }

            Initialize();

            var events = ParseEventTag(eventTag);
            if (events.Count == 0)
            {
                onAllComplete?.Invoke();
                return;
            }

            StartCoroutine(ProcessEventsSequentially(events, onAllComplete));
        }

        /// <summary>
        /// Parses an event tag string into individual events.
        /// </summary>
        private List<(string type, string value)> ParseEventTag(string eventTag)
        {
            var events = new List<(string type, string value)>();
            var parts = eventTag.Split('|');

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var type = trimmed.Substring(0, colonIndex).Trim();
                    var value = trimmed.Substring(colonIndex + 1).Trim();
                    events.Add((type, value));
                }
                else
                {
                    // Event with no value (e.g., "bgm_stop")
                    events.Add((trimmed, ""));
                }
            }

            return events;
        }

        /// <summary>
        /// Processes events one at a time, waiting for each to complete.
        /// </summary>
        private IEnumerator ProcessEventsSequentially(List<(string type, string value)> events, Action onAllComplete)
        {
            foreach (var (type, value) in events)
            {
                bool eventComplete = false;

                if (_handlerMap.TryGetValue(type, out var handler))
                {
                    Debug.Log($"[DialogueEventProcessor] Executing: {type}:{value}");
                    handler.Execute(value, () => eventComplete = true);

                    // Wait for event to complete
                    while (!eventComplete)
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"[DialogueEventProcessor] No handler for event type: {type}");
                }
            }

            onAllComplete?.Invoke();
        }

        /// <summary>
        /// Processes all events in parallel (use when order doesn't matter).
        /// </summary>
        public void ProcessEventTagParallel(string eventTag, Action onAllComplete = null)
        {
            if (string.IsNullOrEmpty(eventTag))
            {
                onAllComplete?.Invoke();
                return;
            }

            Initialize();

            var events = ParseEventTag(eventTag);
            if (events.Count == 0)
            {
                onAllComplete?.Invoke();
                return;
            }

            int remaining = events.Count;
            object lockObj = new object();

            foreach (var (type, value) in events)
            {
                if (_handlerMap.TryGetValue(type, out var handler))
                {
                    handler.Execute(value, () =>
                    {
                        lock (lockObj)
                        {
                            remaining--;
                            if (remaining <= 0)
                            {
                                onAllComplete?.Invoke();
                            }
                        }
                    });
                }
                else
                {
                    Debug.LogWarning($"[DialogueEventProcessor] No handler for event type: {type}");
                    remaining--;
                }
            }

            if (remaining <= 0)
            {
                onAllComplete?.Invoke();
            }
        }
    }
}
