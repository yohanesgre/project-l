using UnityEngine;
using UnityEditor;
using MyGame.Core;
using MyGame.Features.World;

namespace MyGame.Features.Character.Editor
{
    /// <summary>
    /// Custom editor for CharacterPathFollower providing convenient controls and visualization.
    /// </summary>
    [CustomEditor(typeof(CharacterPathFollower))]
    public class CharacterPathFollowerEditor : UnityEditor.Editor
    {
        private CharacterPathFollower _follower;
        
        // Serialized properties
        private SerializedProperty _pathProviderComponentProp;
        private SerializedProperty _speedProp;
        private SerializedProperty _startingProgressProp;
        private SerializedProperty _finishProgressProp;
        private SerializedProperty _progressProp;
        private SerializedProperty _endBehaviorProp;
        private SerializedProperty _isFollowingProp;
        private SerializedProperty _autoStartFollowingProp;
        private SerializedProperty _rotationModeProp;
        private SerializedProperty _rotationSpeedProp;
        private SerializedProperty _heightOffsetProp;
        private SerializedProperty _lateralOffsetProp;
        
        // Gizmo properties
        private SerializedProperty _showGizmosProp;
        private SerializedProperty _gizmoColorProp;
        private SerializedProperty _gizmoSizeProp;
        
        private void OnEnable()
        {
            _follower = (CharacterPathFollower)target;
            
            _pathProviderComponentProp = serializedObject.FindProperty("_pathProviderComponent");
            _speedProp = serializedObject.FindProperty("_speed");
            _startingProgressProp = serializedObject.FindProperty("_startingProgress");
            _finishProgressProp = serializedObject.FindProperty("_finishProgress");
            _progressProp = serializedObject.FindProperty("_progress");
            _endBehaviorProp = serializedObject.FindProperty("_endBehavior");
            _isFollowingProp = serializedObject.FindProperty("_isFollowing");
            _autoStartFollowingProp = serializedObject.FindProperty("_autoStartFollowing");
            _rotationModeProp = serializedObject.FindProperty("_rotationMode");
            _rotationSpeedProp = serializedObject.FindProperty("_rotationSpeed");
            _heightOffsetProp = serializedObject.FindProperty("_heightOffset");
            _lateralOffsetProp = serializedObject.FindProperty("_lateralOffset");
            
            _showGizmosProp = serializedObject.FindProperty("_showGizmos");
            _gizmoColorProp = serializedObject.FindProperty("_gizmoColor");
            _gizmoSizeProp = serializedObject.FindProperty("_gizmoSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Path Reference Section
            EditorGUILayout.LabelField("Path Reference", EditorStyles.boldLabel);
            
            // Custom object field that properly handles IPathProvider
            DrawPathProviderField();
            
            // Show type info if assigned
            var pathProvider = _pathProviderComponentProp.objectReferenceValue as IPathProvider;
            if (pathProvider != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var comp = _pathProviderComponentProp.objectReferenceValue as Component;
                EditorGUILayout.LabelField("Type:", comp.GetType().Name);
                EditorGUILayout.LabelField("Valid Path:", pathProvider.HasValidPath ? "Yes" : "No");
                if (pathProvider.HasValidPath)
                {
                    EditorGUILayout.LabelField("Path Length:", $"{pathProvider.TotalPathLength:F2} units");
                    EditorGUILayout.LabelField("Is Loop:", pathProvider.IsLoop ? "Yes" : "No");
                }
                EditorGUILayout.EndVertical();
            }
            else if (_pathProviderComponentProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a path provider (CustomPath or RoadGenerator) to define the path to follow.",
                    MessageType.Warning
                );
            }
            
            EditorGUILayout.Space(10);
            
            // Movement Settings Section
            EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_speedProp);
            EditorGUILayout.PropertyField(_endBehaviorProp);
            
            EditorGUILayout.Space(5);
            
            // Starting/Finishing progress
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_startingProgressProp, new GUIContent("Starting Progress"));
            EditorGUILayout.PropertyField(_finishProgressProp, new GUIContent("Finish Progress"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
            
            // Progress slider
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_progressProp, new GUIContent("Current Progress"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                if (!Application.isPlaying)
                {
                    _follower.SetProgress(_progressProp.floatValue);
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.PropertyField(_autoStartFollowingProp, new GUIContent("Auto Start Following"));
            
            // Runtime following state
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUILayout.PropertyField(_isFollowingProp, new GUIContent("Is Following (Runtime)"));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Rotation Settings Section
            EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_rotationModeProp);
            EditorGUILayout.PropertyField(_rotationSpeedProp);
            
            EditorGUILayout.Space(10);
            
            // Offset Settings Section
            EditorGUILayout.LabelField("Offset Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_heightOffsetProp);
            EditorGUILayout.PropertyField(_lateralOffsetProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                if (!Application.isPlaying)
                {
                    _follower.SetProgress(_follower.Progress);
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Gizmo Settings Section
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showGizmosProp);
            EditorGUILayout.PropertyField(_gizmoColorProp);
            EditorGUILayout.PropertyField(_gizmoSizeProp);
            
            EditorGUILayout.Space(10);
            
            // Control Buttons Section
            DrawControlButtons();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawControlButtons()
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Reset buttons
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button($"Start ({_startingProgressProp.floatValue:P0})", GUILayout.Height(25)))
            {
                Undo.RecordObject(_follower, "Reset Progress");
                _follower.ResetProgress(); // Uses StartingProgress internally
                EditorUtility.SetDirty(_follower);
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f); // Light blue
            if (GUILayout.Button($"Finish ({_finishProgressProp.floatValue:P0})", GUILayout.Height(25)))
            {
                Undo.RecordObject(_follower, "Set Progress to Finish");
                _follower.SetProgress(_finishProgressProp.floatValue);
                EditorUtility.SetDirty(_follower);
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("End (100%)", GUILayout.Height(25)))
            {
                Undo.RecordObject(_follower, "Set Progress to End");
                _follower.SetProgress(1f);
                EditorUtility.SetDirty(_follower);
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // Play mode controls
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                
                if (_follower.IsFollowing)
                {
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Stop", GUILayout.Height(30)))
                    {
                        _follower.StopFollowing();
                    }
                }
                else
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Start Following", GUILayout.Height(30)))
                    {
                        _follower.StartFollowing();
                    }
                }
                
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Reverse", GUILayout.Height(30)))
                {
                    _follower.ReverseDirection();
                }
                
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to use Start/Stop controls.",
                    MessageType.Info
                );
            }
            
            EditorGUILayout.EndVertical();
            
            // Quick progress buttons
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("0%")) SetProgressWithUndo(0f);
            if (GUILayout.Button("25%")) SetProgressWithUndo(0.25f);
            if (GUILayout.Button("50%")) SetProgressWithUndo(0.5f);
            if (GUILayout.Button("75%")) SetProgressWithUndo(0.75f);
            if (GUILayout.Button("100%")) SetProgressWithUndo(1f);
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Custom field for IPathProvider that properly finds the component when dragging a GameObject.
        /// </summary>
        private void DrawPathProviderField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Path Provider");
            
            // Get current value
            Component currentComponent = _pathProviderComponentProp.objectReferenceValue as Component;
            
            // Create object field that accepts any UnityEngine.Object
            EditorGUI.BeginChangeCheck();
            Object newValue = EditorGUILayout.ObjectField(
                currentComponent,
                typeof(Component),
                true
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                Component resolvedComponent = null;
                
                if (newValue != null)
                {
                    // If it's already an IPathProvider component, use it directly
                    if (newValue is IPathProvider)
                    {
                        resolvedComponent = newValue as Component;
                    }
                    // If it's a GameObject, find an IPathProvider component on it
                    else if (newValue is GameObject go)
                    {
                        resolvedComponent = FindPathProviderOnGameObject(go);
                        
                        if (resolvedComponent == null)
                        {
                            EditorUtility.DisplayDialog(
                                "No Path Provider Found",
                                $"The GameObject '{go.name}' does not have any IPathProvider component.\n\nValid components:\n- CustomPath\n- RoadGenerator",
                                "OK"
                            );
                        }
                    }
                    // If it's a Component but not IPathProvider, check its GameObject
                    else if (newValue is Component comp)
                    {
                        if (comp is IPathProvider)
                        {
                            resolvedComponent = comp;
                        }
                        else
                        {
                            resolvedComponent = FindPathProviderOnGameObject(comp.gameObject);
                            
                            if (resolvedComponent == null)
                            {
                                EditorUtility.DisplayDialog(
                                    "Invalid Component",
                                    $"'{comp.GetType().Name}' is not an IPathProvider.\n\nValid components:\n- CustomPath\n- RoadGenerator",
                                    "OK"
                                );
                            }
                        }
                    }
                }
                
                _pathProviderComponentProp.objectReferenceValue = resolvedComponent;
                serializedObject.ApplyModifiedProperties();
                
                if (!Application.isPlaying && resolvedComponent != null)
                {
                    _follower.SetProgress(_follower.Progress);
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Finds the first IPathProvider component on a GameObject.
        /// </summary>
        private Component FindPathProviderOnGameObject(GameObject go)
        {
            // Try common types first for better performance
            var customPath = go.GetComponent<CustomPath>();
            if (customPath != null) return customPath;
            
            var roadGenerator = go.GetComponent<RoadGenerator>();
            if (roadGenerator != null) return roadGenerator;
            
            // Fallback: search all components for IPathProvider
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp is IPathProvider)
                {
                    return comp;
                }
            }
            
            return null;
        }
        
        private void SetProgressWithUndo(float progress)
        {
            Undo.RecordObject(_follower, "Set Progress");
            _follower.SetProgress(progress);
            EditorUtility.SetDirty(_follower);
            SceneView.RepaintAll();
        }
        
        private void OnSceneGUI()
        {
            var pathProvider = _pathProviderComponentProp?.objectReferenceValue as IPathProvider;
            if (pathProvider == null || !pathProvider.HasValidPath) return;
            
            var point = pathProvider.GetPointAlongPath(_follower.Progress);
            if (!point.HasValue) return;
            
            var pathPoint = point.Value;
            
            // Get offset values
            float heightOffset = _heightOffsetProp?.floatValue ?? 0f;
            float lateralOffset = _lateralOffsetProp?.floatValue ?? 0f;
            
            Vector3 position = pathPoint.Position;
            position += pathPoint.Up * heightOffset;
            position += pathPoint.Right * lateralOffset;
            
            float handleSize = HandleUtility.GetHandleSize(position) * 0.5f;
            
            // Draw clickable handle
            Handles.color = Color.magenta;
            if (Handles.Button(position, Quaternion.identity, handleSize, handleSize * 0.5f, Handles.SphereHandleCap))
            {
                Selection.activeGameObject = _follower.gameObject;
            }
            
            // Draw direction indicator
            Vector3 forward = _follower.Direction > 0 ? pathPoint.Forward : -pathPoint.Forward;
            Handles.color = Color.blue;
            Handles.ArrowHandleCap(
                0,
                position,
                Quaternion.LookRotation(forward),
                handleSize * 2f,
                EventType.Repaint
            );
            
            // Draw connection to path
            Handles.color = new Color(1f, 0f, 1f, 0.3f);
            Handles.DrawLine(position, pathPoint.Position);
            
            // Draw progress label
            Handles.Label(
                position + Vector3.up * (handleSize + 0.5f),
                $"{_follower.Progress:P0}",
                new GUIStyle
                {
                    normal = { textColor = Color.white },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }
    }
}
