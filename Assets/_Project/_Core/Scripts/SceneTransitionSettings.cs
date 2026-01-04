using UnityEngine;

namespace MyGame.Core
{
    /// <summary>
    /// Configuration settings for scene transitions in AnimatedSceneController.
    /// Create instances via: Assets > Create > MyGame > Scene Transition Settings
    /// </summary>
    [CreateAssetMenu(fileName = "SceneTransitionSettings", menuName = "MyGame/Scene Transition Settings", order = 100)]
    public class SceneTransitionSettings : ScriptableObject
    {
        [Header("Scene List")]
        [Tooltip("Ordered list of scene names to transition between")]
        public string[] sceneNames = new string[0];
        
        [Header("Transition Behavior")]
        [Tooltip("Loop back to first scene after last scene")]
        public bool loopScenes = true;
        
        [Tooltip("Auto-transition delay in seconds (0 = disabled)")]
        public float autoTransitionDelay = 0f;
        
        [Header("Fade Settings")]
        [Tooltip("Duration of fade out transition")]
        [Range(0f, 5f)]
        public float fadeOutDuration = 0.5f;
        
        [Tooltip("Duration of fade in transition")]
        [Range(0f, 5f)]
        public float fadeInDuration = 0.5f;
        
        [Tooltip("Color to fade to/from")]
        public Color fadeColor = Color.black;
        
        [Header("Loading Settings")]
        [Tooltip("Load scenes additively (keeps previous scene loaded during transition)")]
        public bool loadAdditive = false;
        
        [Tooltip("Unload previous scene when loading additively")]
        public bool unloadPreviousScene = true;
        
        /// <summary>
        /// Applies these settings to an AnimatedSceneController instance.
        /// </summary>
        public void ApplyToController(AnimatedSceneController controller)
        {
            if (controller == null)
            {
                Debug.LogWarning("[SceneTransitionSettings] Cannot apply to null controller.");
                return;
            }
            
            // Note: This would require exposing setters on AnimatedSceneController
            // For now, settings are applied in the inspector
            Debug.Log($"[SceneTransitionSettings] Settings '{name}' ready to apply to controller.");
        }
        
#if UNITY_EDITOR
        [ContextMenu("Detect Scenes in Animated_Scenes Folder")]
        private void DetectAnimatedScenes()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes/Animated_Scenes" });
            sceneNames = new string[guids.Length];
            
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(path);
            }
            
            System.Array.Sort(sceneNames);
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SceneTransitionSettings] Detected {sceneNames.Length} scenes: {string.Join(", ", sceneNames)}");
        }
#endif
    }
}
