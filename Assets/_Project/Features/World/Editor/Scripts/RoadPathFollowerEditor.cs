using UnityEngine;
using UnityEditor;

namespace MyGame.Features.World.Editor
{
    /// <summary>
    /// Custom editor for RoadPathFollower providing convenient controls and visualization.
    /// </summary>
    [CustomEditor(typeof(RoadPathFollower))]
    public class RoadPathFollowerEditor : UnityEditor.Editor
    {
        private RoadPathFollower _follower;
        
        // Serialized properties for proper undo support
        private SerializedProperty _roadGeneratorProp;
        private SerializedProperty _speedProp;
        private SerializedProperty _startingProgressProp;
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
            _follower = (RoadPathFollower)target;
            
            // Cache serialized properties
            _roadGeneratorProp = serializedObject.FindProperty("_roadGenerator");
            _speedProp = serializedObject.FindProperty("_speed");
            _startingProgressProp = serializedObject.FindProperty("_startingProgress");
            _progressProp = serializedObject.FindProperty("_progress");
            _endBehaviorProp = serializedObject.FindProperty("_endBehavior");
            _isFollowingProp = serializedObject.FindProperty("_isFollowing");
            _autoStartFollowingProp = serializedObject.FindProperty("_autoStartFollowing");
            _rotationModeProp = serializedObject.FindProperty("_rotationMode");
            _rotationSpeedProp = serializedObject.FindProperty("_rotationSpeed");
            _heightOffsetProp = serializedObject.FindProperty("_heightOffset");
            _lateralOffsetProp = serializedObject.FindProperty("_lateralOffset");
            
            // Gizmo properties
            _showGizmosProp = serializedObject.FindProperty("_showGizmos");
            _gizmoColorProp = serializedObject.FindProperty("_gizmoColor");
            _gizmoSizeProp = serializedObject.FindProperty("_gizmoSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Path Reference Section
            EditorGUILayout.LabelField("Path Reference", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_roadGeneratorProp);
            
            if (_roadGeneratorProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a RoadGenerator to define the path to follow.",
                    MessageType.Warning
                );
            }
            
            EditorGUILayout.Space(10);
            
            // Movement Settings Section
            EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_speedProp);
            EditorGUILayout.PropertyField(_endBehaviorProp);
            
            EditorGUILayout.Space(5);
            
            // Starting progress
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_startingProgressProp, new GUIContent("Starting Progress", "Initial position when game starts and on reset"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
            
            // Progress slider with special handling
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_progressProp, new GUIContent("Current Progress"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                // Force position update when progress changes
                if (!Application.isPlaying)
                {
                    _follower.SetProgress(_progressProp.floatValue);
                    SceneView.RepaintAll();
                }
            }
            
            // Auto start option
            EditorGUILayout.PropertyField(_autoStartFollowingProp, new GUIContent("Auto Start Following"));
            
            // Show current following state
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
                
                // Force position update when offsets change
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
            
            // Status Section
            EditorGUILayout.Space(10);
            DrawStatusSection();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawControlButtons()
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Reset button (works in both edit and play mode)
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button($"Reset to Start ({_startingProgressProp.floatValue:P0})", GUILayout.Height(25)))
            {
                Undo.RecordObject(_follower, "Reset Progress");
                _follower.ResetProgress();
                EditorUtility.SetDirty(_follower);
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("Go to End", GUILayout.Height(25)))
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
            
            if (GUILayout.Button("0%"))
            {
                SetProgressWithUndo(0f);
            }
            if (GUILayout.Button("25%"))
            {
                SetProgressWithUndo(0.25f);
            }
            if (GUILayout.Button("50%"))
            {
                SetProgressWithUndo(0.5f);
            }
            if (GUILayout.Button("75%"))
            {
                SetProgressWithUndo(0.75f);
            }
            if (GUILayout.Button("100%"))
            {
                SetProgressWithUndo(1f);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void SetProgressWithUndo(float progress)
        {
            Undo.RecordObject(_follower, "Set Progress");
            _follower.SetProgress(progress);
            EditorUtility.SetDirty(_follower);
            SceneView.RepaintAll();
        }
        
        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Show road generator status
            if (_follower.RoadGenerator != null)
            {
                EditorGUILayout.LabelField("Road Generator:", _follower.RoadGenerator.name);
                EditorGUILayout.LabelField("Is Loop:", _follower.RoadGenerator.IsLoop.ToString());
            }
            else
            {
                EditorGUILayout.LabelField("Road Generator:", "Not Assigned", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.Space(5);
            
            // Show current state
            EditorGUILayout.LabelField($"Starting Progress: {_follower.StartingProgress:P1}");
            EditorGUILayout.LabelField($"Current Progress: {_follower.Progress:P1}");
            EditorGUILayout.LabelField($"Speed: {_follower.Speed} units/sec");
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField($"Following: {(_follower.IsFollowing ? "Yes" : "No")}");
                EditorGUILayout.LabelField($"Direction: {(_follower.Direction > 0 ? "Forward" : "Backward")}");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void OnSceneGUI()
        {
            if (_follower.RoadGenerator == null) return;
            
            // Draw a handle at the current position for easy manipulation
            var point = _follower.RoadGenerator.GetPointAlongPath(_follower.Progress);
            if (!point.HasValue) return;
            
            var roadPoint = point.Value;
            
            // Calculate position with offsets (matching the follower's calculation)
            Vector3 position = roadPoint.Position;
            
            // Get offset values via reflection since they're private
            SerializedObject so = new SerializedObject(_follower);
            float heightOffset = so.FindProperty("_heightOffset").floatValue;
            float lateralOffset = so.FindProperty("_lateralOffset").floatValue;
            
            position += roadPoint.Up * heightOffset;
            position += roadPoint.Right * lateralOffset;
            
            // Draw a slider handle to adjust progress along the path
            float handleSize = HandleUtility.GetHandleSize(position) * 0.5f;
            
            Handles.color = Color.yellow;
            if (Handles.Button(position, Quaternion.identity, handleSize, handleSize * 0.5f, Handles.SphereHandleCap))
            {
                Selection.activeGameObject = _follower.gameObject;
            }
            
            // Draw direction indicator
            Vector3 forward = _follower.Direction > 0 ? roadPoint.Forward : -roadPoint.Forward;
            Handles.color = Color.blue;
            Handles.ArrowHandleCap(
                0,
                position,
                Quaternion.LookRotation(forward),
                handleSize * 2f,
                EventType.Repaint
            );
            
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
