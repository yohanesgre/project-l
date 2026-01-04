using UnityEngine;
using UnityEditor;
using MyGame.Core;

namespace MyGame.Core.Editor
{
    /// <summary>
    /// Custom editor for CameraController providing proper IPathProvider field handling.
    /// </summary>
    [CustomEditor(typeof(CameraController))]
    public class CameraControllerEditor : UnityEditor.Editor
    {
        private CameraController _controller;
        
        // Serialized properties
        private SerializedProperty _pathProviderComponentProp;
        private SerializedProperty _targetProp;
        private SerializedProperty _targetPathFollowerComponentProp;
        private SerializedProperty _progressOffsetProp;
        private SerializedProperty _heightOffsetProp;
        private SerializedProperty _lateralOffsetProp;
        private SerializedProperty _lookAtHeightOffsetProp;
        private SerializedProperty _rotationSmoothSpeedProp;
        private SerializedProperty _framingOffsetXProp;
        private SerializedProperty _framingOffsetYProp;
        private SerializedProperty _rollAngleProp;
        private SerializedProperty _pitchOffsetProp;
        private SerializedProperty _positionSmoothTimeProp;
        private SerializedProperty _snapOnStartProp;
        private SerializedProperty _manualProgressProp;
        private SerializedProperty _showGizmosProp;
        private SerializedProperty _gizmoColorProp;
        private SerializedProperty _gizmoSizeProp;
        
        private void OnEnable()
        {
            _controller = (CameraController)target;
            
            _pathProviderComponentProp = serializedObject.FindProperty("_pathProviderComponent");
            _targetProp = serializedObject.FindProperty("_target");
            _targetPathFollowerComponentProp = serializedObject.FindProperty("_targetPathFollowerComponent");
            _progressOffsetProp = serializedObject.FindProperty("_progressOffset");
            _heightOffsetProp = serializedObject.FindProperty("_heightOffset");
            _lateralOffsetProp = serializedObject.FindProperty("_lateralOffset");
            _lookAtHeightOffsetProp = serializedObject.FindProperty("_lookAtHeightOffset");
            _rotationSmoothSpeedProp = serializedObject.FindProperty("_rotationSmoothSpeed");
            _framingOffsetXProp = serializedObject.FindProperty("_framingOffsetX");
            _framingOffsetYProp = serializedObject.FindProperty("_framingOffsetY");
            _rollAngleProp = serializedObject.FindProperty("_rollAngle");
            _pitchOffsetProp = serializedObject.FindProperty("_pitchOffset");
            _positionSmoothTimeProp = serializedObject.FindProperty("_positionSmoothTime");
            _snapOnStartProp = serializedObject.FindProperty("_snapOnStart");
            _manualProgressProp = serializedObject.FindProperty("_manualProgress");
            _showGizmosProp = serializedObject.FindProperty("_showGizmos");
            _gizmoColorProp = serializedObject.FindProperty("_gizmoColor");
            _gizmoSizeProp = serializedObject.FindProperty("_gizmoSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // References Section
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            
            // Custom path provider field
            DrawPathProviderField();
            
            EditorGUILayout.PropertyField(_targetProp);
            EditorGUILayout.PropertyField(_targetPathFollowerComponentProp);
            
            // Show path provider info
            var pathProvider = _pathProviderComponentProp.objectReferenceValue as IPathProvider;
            if (pathProvider != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var comp = _pathProviderComponentProp.objectReferenceValue as Component;
                EditorGUILayout.LabelField("Path Type:", comp.GetType().Name);
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
                    "Assign a path provider (CustomPath or RoadGenerator) for the camera to follow.",
                    MessageType.Info
                );
            }
            
            EditorGUILayout.Space(10);
            
            // Path Position Settings
            EditorGUILayout.LabelField("Path Position Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_progressOffsetProp);
            EditorGUILayout.PropertyField(_heightOffsetProp);
            EditorGUILayout.PropertyField(_lateralOffsetProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                {
                    _controller.SnapToPosition();
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Look At Settings
            EditorGUILayout.LabelField("Look At Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_lookAtHeightOffsetProp);
            EditorGUILayout.PropertyField(_rotationSmoothSpeedProp);
            EditorGUILayout.PropertyField(_framingOffsetXProp);
            EditorGUILayout.PropertyField(_framingOffsetYProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                {
                    _controller.SnapToPosition();
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Downhill Illusion
            EditorGUILayout.LabelField("Downhill Illusion (Camera Tilt)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_rollAngleProp);
            EditorGUILayout.PropertyField(_pitchOffsetProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                {
                    _controller.SnapToPosition();
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Smoothing
            EditorGUILayout.LabelField("Smoothing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_positionSmoothTimeProp);
            EditorGUILayout.PropertyField(_snapOnStartProp);
            
            EditorGUILayout.Space(10);
            
            // Manual Control
            EditorGUILayout.LabelField("Manual Control", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_manualProgressProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                {
                    _controller.SnapToPosition();
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Gizmo Settings
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showGizmosProp);
            EditorGUILayout.PropertyField(_gizmoColorProp);
            EditorGUILayout.PropertyField(_gizmoSizeProp);
            
            EditorGUILayout.Space(10);
            
            // Controls
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Snap to Position", GUILayout.Height(25)))
            {
                Undo.RecordObject(_controller.transform, "Snap Camera");
                _controller.SnapToPosition();
                EditorUtility.SetDirty(_controller);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Custom field for IPathProvider that properly finds the component when dragging a GameObject.
        /// </summary>
        private void DrawPathProviderField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Path Provider");
            
            Component currentComponent = _pathProviderComponentProp.objectReferenceValue as Component;
            
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
                    if (newValue is IPathProvider)
                    {
                        resolvedComponent = newValue as Component;
                    }
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
                    _controller.SnapToPosition();
                    SceneView.RepaintAll();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private Component FindPathProviderOnGameObject(GameObject go)
        {
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
    }
}
