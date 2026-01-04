using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using MyGame.Core;
using MyGame.Features.Character;

namespace MyGame.Editor
{
    /// <summary>
    /// Editor utility to set up the Scene Transition Test scene.
    /// </summary>
    public static class SceneTransitionTestSetup
    {
        private const string TEST_SCENE_PATH = "Assets/_Project/Scenes/SceneTransitionTest.unity";
        private const string SCENE_RIDING_STOP = "Assets/_Project/Scenes/Animated_Scenes/Scene_Riding_Stop.unity";
        private const string SCENE_RIDING_LOOPING = "Assets/_Project/Scenes/Animated_Scenes/Scene_Riding_Looping.unity";
        private const string OVERLAY_UXML_PATH = "Assets/_Project/Features/UI/SceneTransitionOverlay.uxml";
        private const string PANEL_SETTINGS_PATH = "Assets/_Project/Features/Dialogue/UI/New Panel Settings.asset";

        [MenuItem("Tools/Scene Transition Test/Create Test Scene")]
        public static void CreateTestScene()
        {
            // Save current scene
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // Create new scene
                Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                
                // Set up the scene
                GameObject transitionManager = SetupTestScene();
                
                // Copy character and path from Scene_Riding_Stop
                CopyCharacterAndPathFromStopScene(transitionManager);
                
                // Save the scene
                EditorSceneManager.SaveScene(testScene, TEST_SCENE_PATH);
                
                Debug.Log($"<color=green>[Scene Transition Test]</color> Test scene created at: {TEST_SCENE_PATH}");
                Debug.Log($"<color=green>[Complete!]</color> Scene is ready to test. Press Play to see the transition!");
            }
        }

        [MenuItem("Tools/Scene Transition Test/Setup Current Scene")]
        public static void SetupCurrentScene()
        {
            GameObject transitionManager = SetupTestScene();
            CopyCharacterAndPathFromStopScene(transitionManager);
            Debug.Log("<color=green>[Scene Transition Test]</color> Current scene has been set up for transition testing.");
        }

        private static GameObject SetupTestScene()
        {
            // 1. Create SceneTransitionManager GameObject
            GameObject transitionManager = new GameObject("SceneTransitionManager");
            
            // Add AnimatedSceneController
            var sceneController = transitionManager.AddComponent<AnimatedSceneController>();
            
            // Add UIDocument
            var uiDocument = transitionManager.AddComponent<UIDocument>();
            
            // Load and assign UXML
            var overlayUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OVERLAY_UXML_PATH);
            if (overlayUxml != null)
            {
                uiDocument.visualTreeAsset = overlayUxml;
                Debug.Log("<color=cyan>[Setup]</color> Assigned UXML to UIDocument");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[Setup]</color> Could not find UXML at: {OVERLAY_UXML_PATH}");
            }
            
            // Load and assign PanelSettings
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PANEL_SETTINGS_PATH);
            if (panelSettings != null)
            {
                uiDocument.panelSettings = panelSettings;
                Debug.Log("<color=cyan>[Setup]</color> Assigned PanelSettings to UIDocument");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[Setup]</color> Could not find PanelSettings at: {PANEL_SETTINGS_PATH}");
            }
            
            // Configure AnimatedSceneController scenes via SerializedObject
            SerializedObject so = new SerializedObject(sceneController);
            SerializedProperty scenesProperty = so.FindProperty("_scenes");
            
            // Clear existing scenes
            scenesProperty.ClearArray();
            
            // Add Scene_Riding_Stop
            scenesProperty.InsertArrayElementAtIndex(0);
            SerializedProperty scene0 = scenesProperty.GetArrayElementAtIndex(0);
            scene0.FindPropertyRelative("_sceneName").stringValue = "Scene_Riding_Stop";
            scene0.FindPropertyRelative("_scenePath").stringValue = SCENE_RIDING_STOP;
            
            // Add Scene_Riding_Looping
            scenesProperty.InsertArrayElementAtIndex(1);
            SerializedProperty scene1 = scenesProperty.GetArrayElementAtIndex(1);
            scene1.FindPropertyRelative("_sceneName").stringValue = "Scene_Riding_Looping";
            scene1.FindPropertyRelative("_scenePath").stringValue = SCENE_RIDING_LOOPING;
            
            // Configure transition settings
            SerializedProperty transitionSettings = so.FindProperty("_transitionSettings");
            transitionSettings.FindPropertyRelative("_fadeDuration").floatValue = 0.5f;
            transitionSettings.FindPropertyRelative("_sceneLoadMode").enumValueIndex = 1; // Additive mode
            transitionSettings.FindPropertyRelative("_autoTransition").boolValue = false;
            
            Debug.Log("<color=cyan>[Setup]</color> Configured AnimatedSceneController with 2 scenes (Additive mode)");
            
            // Auto-load first scene
            SerializedProperty autoLoadFirstScene = so.FindProperty("_autoLoadFirstScene");
            if (autoLoadFirstScene != null)
            {
                autoLoadFirstScene.boolValue = true;
                Debug.Log("<color=cyan>[Setup]</color> Enabled auto-load for first scene");
            }
            
            so.ApplyModifiedProperties();
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            // Select the transition manager for easy inspection
            Selection.activeGameObject = transitionManager;
            
            Debug.Log("<color=green>[Setup]</color> SceneTransitionManager setup complete!");
            
            return transitionManager;
        }
        
        private static void CopyCharacterAndPathFromStopScene(GameObject transitionManager)
        {
            Debug.Log("<color=cyan>[Setup]</color> Loading Scene_Riding_Stop to copy character and path...");
            
            // Load Scene_Riding_Stop additively
            Scene stopScene = EditorSceneManager.OpenScene(SCENE_RIDING_STOP, OpenSceneMode.Additive);
            
            if (!stopScene.IsValid())
            {
                Debug.LogError("<color=red>[Setup]</color> Failed to load Scene_Riding_Stop!");
                return;
            }
            
            // Find the character with CharacterPathFollower
            CharacterPathFollower characterPathFollower = null;
            GameObject characterObject = null;
            GameObject pathObject = null;
            
            foreach (GameObject rootObj in stopScene.GetRootGameObjects())
            {
                var pathFollower = rootObj.GetComponentInChildren<CharacterPathFollower>();
                if (pathFollower != null)
                {
                    characterPathFollower = pathFollower;
                    characterObject = pathFollower.gameObject;
                    
                    // Get the path provider
                    if (characterPathFollower.PathProvider != null)
                    {
                        Component pathComponent = characterPathFollower.PathProvider as Component;
                        if (pathComponent != null)
                        {
                            pathObject = pathComponent.gameObject;
                        }
                    }
                    break;
                }
            }
            
            if (characterObject == null)
            {
                Debug.LogWarning("<color=yellow>[Setup]</color> Could not find character with CharacterPathFollower in Scene_Riding_Stop");
                EditorSceneManager.CloseScene(stopScene, true);
                return;
            }
            
            Debug.Log($"<color=cyan>[Setup]</color> Found character: {characterObject.name}");
            
            // Get the active scene (our test scene)
            Scene activeScene = SceneManager.GetActiveScene();
            
            // Copy the path first (if it exists and is a separate object)
            GameObject copiedPath = null;
            if (pathObject != null && pathObject != characterObject)
            {
                copiedPath = Object.Instantiate(pathObject);
                SceneManager.MoveGameObjectToScene(copiedPath, activeScene);
                Debug.Log($"<color=cyan>[Setup]</color> Copied path: {copiedPath.name}");
            }
            
            // Copy the character
            GameObject copiedCharacter = Object.Instantiate(characterObject);
            SceneManager.MoveGameObjectToScene(copiedCharacter, activeScene);
            Debug.Log($"<color=cyan>[Setup]</color> Copied character: {copiedCharacter.name}");
            
            // Update the path reference if we copied the path
            var copiedPathFollower = copiedCharacter.GetComponent<CharacterPathFollower>();
            if (copiedPathFollower != null && copiedPath != null)
            {
                var pathProvider = copiedPath.GetComponent<IPathProvider>();
                if (pathProvider != null)
                {
                    copiedPathFollower.PathProvider = pathProvider;
                    Debug.Log("<color=cyan>[Setup]</color> Updated path reference on copied character");
                }
            }
            
            // Configure CharacterPathFollower for the test
            if (copiedPathFollower != null)
            {
                SerializedObject pathFollowerSO = new SerializedObject(copiedPathFollower);
                
                // Set End Behavior to Stop
                pathFollowerSO.FindProperty("_endBehavior").enumValueIndex = 0; // Stop
                
                // Enable auto-start
                pathFollowerSO.FindProperty("_autoStartFollowing").boolValue = true;
                
                // Reset progress
                pathFollowerSO.FindProperty("_startingProgress").floatValue = 0f;
                pathFollowerSO.FindProperty("_progress").floatValue = 0f;
                
                pathFollowerSO.ApplyModifiedProperties();
                
                Debug.Log("<color=cyan>[Setup]</color> Configured CharacterPathFollower (EndBehavior=Stop, AutoStart=true)");
                
                // Wire up the OnPathComplete event
                WireUpPathCompleteEvent(copiedPathFollower, transitionManager);
            }
            
            // Close the Scene_Riding_Stop scene
            EditorSceneManager.CloseScene(stopScene, false);
            
            // Mark our scene as dirty
            EditorSceneManager.MarkSceneDirty(activeScene);
            
            Debug.Log("<color=green>[Setup]</color> Character and path setup complete!");
        }
        
        private static void WireUpPathCompleteEvent(CharacterPathFollower pathFollower, GameObject transitionManager)
        {
            var sceneController = transitionManager.GetComponent<AnimatedSceneController>();
            if (sceneController == null)
            {
                Debug.LogWarning("<color=yellow>[Setup]</color> Could not find AnimatedSceneController on transitionManager");
                return;
            }
            
            SerializedObject pathFollowerSO = new SerializedObject(pathFollower);
            SerializedProperty onPathCompleteProperty = pathFollowerSO.FindProperty("_onPathComplete");
            
            if (onPathCompleteProperty == null)
            {
                Debug.LogWarning("<color=yellow>[Setup]</color> Could not find _onPathComplete property");
                return;
            }
            
            // Clear existing listeners
            onPathCompleteProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
            
            // Add new listener
            int callIndex = onPathCompleteProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize;
            onPathCompleteProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").InsertArrayElementAtIndex(callIndex);
            
            SerializedProperty call = onPathCompleteProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").GetArrayElementAtIndex(callIndex);
            call.FindPropertyRelative("m_Target").objectReferenceValue = sceneController;
            call.FindPropertyRelative("m_MethodName").stringValue = "TransitionToNextScene";
            call.FindPropertyRelative("m_Mode").enumValueIndex = 1; // Void method
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly
            
            pathFollowerSO.ApplyModifiedProperties();
            
            Debug.Log("<color=green>[Setup]</color> Connected OnPathComplete → AnimatedSceneController.TransitionToNextScene()");
        }

        [MenuItem("Tools/Scene Transition Test/Add Scenes to Build Settings")]
        public static void AddScenesToBuildSettings()
        {
            // Get current build scenes
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            
            // Scenes to add
            string[] scenePaths = new string[]
            {
                TEST_SCENE_PATH,
                SCENE_RIDING_STOP,
                SCENE_RIDING_LOOPING
            };
            
            bool modified = false;
            
            foreach (string scenePath in scenePaths)
            {
                // Check if scene already exists in build settings
                bool exists = buildScenes.Exists(s => s.path == scenePath);
                
                if (!exists && System.IO.File.Exists(scenePath))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    Debug.Log($"<color=cyan>[Build Settings]</color> Added: {scenePath}");
                    modified = true;
                }
                else if (exists)
                {
                    Debug.Log($"<color=gray>[Build Settings]</color> Already exists: {scenePath}");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[Build Settings]</color> Scene not found: {scenePath}");
                }
            }
            
            if (modified)
            {
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Debug.Log("<color=green>[Build Settings]</color> Build settings updated!");
            }
            else
            {
                Debug.Log("<color=green>[Build Settings]</color> All scenes already in build settings.");
            }
        }

        [MenuItem("Tools/Scene Transition Test/Open Test Scene")]
        public static void OpenTestScene()
        {
            if (System.IO.File.Exists(TEST_SCENE_PATH))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(TEST_SCENE_PATH);
                    Debug.Log($"<color=green>[Scene Transition Test]</color> Opened: {TEST_SCENE_PATH}");
                }
            }
            else
            {
                Debug.LogError($"<color=red>[Scene Transition Test]</color> Test scene not found at: {TEST_SCENE_PATH}");
                Debug.Log("<color=yellow>[Hint]</color> Use 'Tools → Scene Transition Test → Create Test Scene' first.");
            }
        }
        
        [MenuItem("Tools/Scene Transition Test/Fix Existing Test Scene")]
        public static void FixExistingTestScene()
        {
            if (!System.IO.File.Exists(TEST_SCENE_PATH))
            {
                Debug.LogError($"<color=red>[Scene Transition Test]</color> Test scene not found at: {TEST_SCENE_PATH}");
                return;
            }
            
            // Open the scene
            Scene testScene = EditorSceneManager.OpenScene(TEST_SCENE_PATH, OpenSceneMode.Single);
            
            // Find the SceneTransitionManager
            GameObject transitionManager = GameObject.Find("SceneTransitionManager");
            if (transitionManager == null)
            {
                Debug.LogError("<color=red>[Fix]</color> SceneTransitionManager not found in scene!");
                return;
            }
            
            var sceneController = transitionManager.GetComponent<AnimatedSceneController>();
            if (sceneController == null)
            {
                Debug.LogError("<color=red>[Fix]</color> AnimatedSceneController not found on SceneTransitionManager!");
                return;
            }
            
            // Update the AnimatedSceneController settings
            SerializedObject so = new SerializedObject(sceneController);
            
            // Enable auto-load first scene
            SerializedProperty autoLoadProp = so.FindProperty("autoLoadFirstScene");
            if (autoLoadProp != null)
            {
                autoLoadProp.boolValue = true;
                Debug.Log("<color=green>[Fix]</color> Enabled auto-load first scene");
            }
            
            // Ensure loadAdditive is enabled
            SerializedProperty loadAdditiveProp = so.FindProperty("loadAdditive");
            if (loadAdditiveProp != null)
            {
                loadAdditiveProp.boolValue = true;
                Debug.Log("<color=green>[Fix]</color> Enabled additive loading");
            }
            
            so.ApplyModifiedProperties();
            
            // Save the scene
            EditorSceneManager.SaveScene(testScene);
            
            Debug.Log("<color=green>[Fix]</color> Test scene has been fixed! Press Play to test.");
        }
    }
}
