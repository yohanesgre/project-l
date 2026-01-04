using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MyGame.Core.Editor
{
    [CustomEditor(typeof(AnimatedSceneController))]
    public class AnimatedSceneControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _animatedScenes;
        private SerializedProperty _loopScenes;
        private SerializedProperty _autoTransitionDelay;
        private SerializedProperty _fadeOutDuration;
        private SerializedProperty _fadeInDuration;
        private SerializedProperty _loadAdditive;
        private SerializedProperty _unloadPreviousScene;
        private SerializedProperty _autoLoadFirstScene;
        private SerializedProperty _fadeUIDocument;
        private SerializedProperty _fadeOverlayName;
        private SerializedProperty _fadeColor;
        
        private void OnEnable()
        {
            _animatedScenes = serializedObject.FindProperty("animatedScenes");
            _loopScenes = serializedObject.FindProperty("loopScenes");
            _autoTransitionDelay = serializedObject.FindProperty("autoTransitionDelay");
            _fadeOutDuration = serializedObject.FindProperty("fadeOutDuration");
            _fadeInDuration = serializedObject.FindProperty("fadeInDuration");
            _loadAdditive = serializedObject.FindProperty("loadAdditive");
            _unloadPreviousScene = serializedObject.FindProperty("unloadPreviousScene");
            _autoLoadFirstScene = serializedObject.FindProperty("autoLoadFirstScene");
            _fadeUIDocument = serializedObject.FindProperty("fadeUIDocument");
            _fadeOverlayName = serializedObject.FindProperty("fadeOverlayName");
            _fadeColor = serializedObject.FindProperty("fadeColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            AnimatedSceneController controller = (AnimatedSceneController)target;
            
            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animated Scene Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Manages transitions between animated scenes. Configure scene list, transitions, and loading behavior.",
                MessageType.Info
            );
            EditorGUILayout.Space();
            
            // Status Display (only in play mode)
            if (Application.isPlaying)
            {
                DrawPlayModeStatus(controller);
                EditorGUILayout.Space();
            }
            
            // Scene Configuration
            EditorGUILayout.LabelField("Scene Configuration", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(_animatedScenes, new GUIContent("Animated Scenes"));
            
            if (_animatedScenes.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No scenes configured. Click 'Detect Animated Scenes' below.", MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(_loopScenes, new GUIContent("Loop Scenes", "Return to first scene after last scene"));
            EditorGUILayout.PropertyField(_autoTransitionDelay, new GUIContent("Auto Transition Delay", "Auto-transition after delay (0 = disabled)"));
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            
            // Transition Settings
            EditorGUILayout.LabelField("Transition Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(_fadeOutDuration, new GUIContent("Fade Out Duration"));
            EditorGUILayout.PropertyField(_fadeInDuration, new GUIContent("Fade In Duration"));
            EditorGUILayout.PropertyField(_fadeColor, new GUIContent("Fade Color"));
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            
            // Loading Settings
            EditorGUILayout.LabelField("Loading Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(_loadAdditive, new GUIContent("Load Additive", "Keep previous scene loaded during transition"));
            
            if (_loadAdditive.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_unloadPreviousScene, new GUIContent("Unload Previous", "Unload previous scene after transition"));
                EditorGUILayout.PropertyField(_autoLoadFirstScene, new GUIContent("Auto Load First Scene", "Automatically load first scene on Start"));
                EditorGUI.indentLevel--;
                
                if (_autoLoadFirstScene.boolValue)
                {
                    EditorGUILayout.HelpBox("First scene will load additively when this scene starts. Useful for persistent manager scenes.", MessageType.Info);
                }
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            
            // UI Toolkit References
            EditorGUILayout.LabelField("UI Toolkit References", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(_fadeUIDocument, new GUIContent("Fade UI Document", "UIDocument with fade overlay"));
            EditorGUILayout.PropertyField(_fadeOverlayName, new GUIContent("Fade Overlay Name", "Name of overlay element in UXML"));
            
            if (_fadeUIDocument.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No UIDocument assigned. Transitions will be instant.\n\nCreate: GameObject > UI Toolkit > UI Document\nAssign UXML: Assets/_Project/Features/UI/SceneTransitionOverlay.uxml", MessageType.Warning);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            
            // Utility Buttons
            DrawUtilityButtons(controller);
            
            // Play Mode Controls
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                DrawPlayModeControls(controller);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawPlayModeStatus(AnimatedSceneController controller)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Current Scene", controller.CurrentSceneName ?? "None");
            EditorGUILayout.IntField("Scene Index", controller.CurrentSceneIndex);
            EditorGUILayout.Toggle("Is Transitioning", controller.IsTransitioning);
            EditorGUILayout.IntField("Total Scenes", controller.SceneCount);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawUtilityButtons(AnimatedSceneController controller)
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Detect Animated Scenes", GUILayout.Height(30)))
            {
                DetectAnimatedScenes();
            }
            
            if (GUILayout.Button("Add to Build Settings", GUILayout.Height(30)))
            {
                AddScenesToBuildSettings();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear Scene List"))
            {
                if (EditorUtility.DisplayDialog("Clear Scene List", "Are you sure you want to clear all scenes?", "Yes", "Cancel"))
                {
                    _animatedScenes.ClearArray();
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            if (GUILayout.Button("Sort Scenes"))
            {
                SortSceneList();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPlayModeControls(AnimatedSceneController controller)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Play Mode Controls", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(controller.IsTransitioning);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("◄ Previous Scene", GUILayout.Height(35)))
            {
                controller.TransitionToPreviousScene();
            }
            
            if (GUILayout.Button("Next Scene ►", GUILayout.Height(35)))
            {
                controller.TransitionToNextScene();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Scene selection dropdown
            if (controller.SceneCount > 0)
            {
                EditorGUILayout.Space(5);
                
                string[] sceneNames = new string[controller.SceneCount];
                for (int i = 0; i < controller.SceneCount; i++)
                {
                    sceneNames[i] = $"{i}: {controller.GetSceneNameAtIndex(i)}";
                }
                
                int selectedIndex = controller.CurrentSceneIndex >= 0 ? controller.CurrentSceneIndex : 0;
                int newIndex = EditorGUILayout.Popup("Jump to Scene", selectedIndex, sceneNames);
                
                if (newIndex != selectedIndex && newIndex >= 0)
                {
                    controller.TransitionToSceneByIndex(newIndex);
                }
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DetectAnimatedScenes()
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes/Animated_Scenes" });
            
            _animatedScenes.ClearArray();
            
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                _animatedScenes.InsertArrayElementAtIndex(i);
                _animatedScenes.GetArrayElementAtIndex(i).stringValue = sceneName;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"[AnimatedSceneController] Detected {guids.Length} animated scenes.");
        }
        
        private void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int addedCount = 0;
            
            for (int i = 0; i < _animatedScenes.arraySize; i++)
            {
                string sceneName = _animatedScenes.GetArrayElementAtIndex(i).stringValue;
                string[] foundScenes = AssetDatabase.FindAssets($"{sceneName} t:Scene");
                
                if (foundScenes.Length > 0)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(foundScenes[0]);
                    
                    bool alreadyAdded = scenes.Exists(s => s.path == scenePath);
                    
                    if (!alreadyAdded)
                    {
                        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                        addedCount++;
                        Debug.Log($"Added to build settings: {scenePath}");
                    }
                }
            }
            
            EditorBuildSettings.scenes = scenes.ToArray();
            
            if (addedCount > 0)
            {
                EditorUtility.DisplayDialog("Success", $"Added {addedCount} scene(s) to build settings.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "All scenes are already in build settings.", "OK");
            }
        }
        
        private void SortSceneList()
        {
            List<string> sceneList = new List<string>();
            
            for (int i = 0; i < _animatedScenes.arraySize; i++)
            {
                sceneList.Add(_animatedScenes.GetArrayElementAtIndex(i).stringValue);
            }
            
            sceneList.Sort();
            
            for (int i = 0; i < sceneList.Count; i++)
            {
                _animatedScenes.GetArrayElementAtIndex(i).stringValue = sceneList[i];
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
