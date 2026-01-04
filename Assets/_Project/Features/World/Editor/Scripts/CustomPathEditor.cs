using UnityEngine;
using UnityEditor;

namespace MyGame.Features.World.Editor
{
    /// <summary>
    /// Custom editor for CustomPath providing scene handles for control point manipulation.
    /// </summary>
    [CustomEditor(typeof(CustomPath))]
    public class CustomPathEditor : UnityEditor.Editor
    {
        private CustomPath _path;
        private Tool _lastTool = Tool.None;
        
        // Serialized properties
        private SerializedProperty _controlPointsProp;
        private SerializedProperty _isLoopProp;
        private SerializedProperty _segmentsPerPointProp;
        private SerializedProperty _autoRecalculateProp;
        private SerializedProperty _showGizmosProp;
        private SerializedProperty _pathColorProp;
        private SerializedProperty _pointColorProp;
        private SerializedProperty _pointSizeProp;
        
        private void OnEnable()
        {
            _path = (CustomPath)target;
            
            _controlPointsProp = serializedObject.FindProperty("_controlPoints");
            _isLoopProp = serializedObject.FindProperty("_isLoop");
            _segmentsPerPointProp = serializedObject.FindProperty("_segmentsPerPoint");
            _autoRecalculateProp = serializedObject.FindProperty("_autoRecalculate");
            _showGizmosProp = serializedObject.FindProperty("_showGizmos");
            _pathColorProp = serializedObject.FindProperty("_pathColor");
            _pointColorProp = serializedObject.FindProperty("_pointColor");
            _pointSizeProp = serializedObject.FindProperty("_pointSize");
        }
        
        private void OnDisable()
        {
            // Restore the last used tool
            if (_lastTool != Tool.None)
            {
                Tools.current = _lastTool;
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Path Points Section
            EditorGUILayout.LabelField("Path Points", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_controlPointsProp, true);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Collect Children"))
            {
                Undo.RecordObject(_path, "Collect Child Points");
                _path.CollectChildPoints();
                EditorUtility.SetDirty(_path);
            }
            
            if (GUILayout.Button("Add Point"))
            {
                AddNewPoint();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Path Settings Section
            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_isLoopProp);
            EditorGUILayout.PropertyField(_segmentsPerPointProp);
            EditorGUILayout.PropertyField(_autoRecalculateProp);
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _path.MarkDirty();
                SceneView.RepaintAll();
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Recalculate Path", GUILayout.Height(25)))
            {
                Undo.RecordObject(_path, "Recalculate Path");
                _path.RecalculatePath();
                EditorUtility.SetDirty(_path);
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Points", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Points", "Are you sure you want to clear all control points?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(_path, "Clear Points");
                    _path.ClearPoints();
                    EditorUtility.SetDirty(_path);
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Gizmo Settings Section
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showGizmosProp);
            EditorGUILayout.PropertyField(_pathColorProp);
            EditorGUILayout.PropertyField(_pointColorProp);
            EditorGUILayout.PropertyField(_pointSizeProp);
            
            EditorGUILayout.Space(10);
            
            // Status Section
            DrawStatusSection();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            int pointCount = _path.ControlPoints?.Count ?? 0;
            if (pointCount == 0)
            {
                // Check for children
                pointCount = _path.transform.childCount;
            }
            
            EditorGUILayout.LabelField($"Control Points: {pointCount}");
            EditorGUILayout.LabelField($"Is Loop: {_path.IsLoop}");
            EditorGUILayout.LabelField($"Has Valid Path: {_path.HasValidPath}");
            
            if (_path.HasValidPath)
            {
                EditorGUILayout.LabelField($"Total Length: {_path.TotalPathLength:F2} units");
            }
            
            EditorGUILayout.EndVertical();
            
            if (pointCount < 2)
            {
                EditorGUILayout.HelpBox(
                    "Add at least 2 control points to create a valid path. You can add child GameObjects or use the 'Add Point' button.",
                    MessageType.Info
                );
            }
        }
        
        private void AddNewPoint()
        {
            Vector3 newPosition = _path.transform.position;
            
            // If there are existing points, place new one after the last
            if (_path.ControlPoints.Count > 0)
            {
                var lastPoint = _path.ControlPoints[_path.ControlPoints.Count - 1];
                if (lastPoint != null)
                {
                    newPosition = lastPoint.position + lastPoint.forward * 3f;
                }
            }
            else if (_path.transform.childCount > 0)
            {
                var lastChild = _path.transform.GetChild(_path.transform.childCount - 1);
                newPosition = lastChild.position + lastChild.forward * 3f;
            }
            
            Undo.RecordObject(_path, "Add Control Point");
            Transform newPoint = _path.AddControlPoint(newPosition);
            Undo.RegisterCreatedObjectUndo(newPoint.gameObject, "Add Control Point");
            
            Selection.activeGameObject = newPoint.gameObject;
            EditorUtility.SetDirty(_path);
            SceneView.RepaintAll();
        }
        
        private void OnSceneGUI()
        {
            if (_path == null) return;
            
            // Draw handles for each control point
            DrawControlPointHandles();
            
            // Draw "Add Point" button in scene view
            DrawSceneViewUI();
        }
        
        private void DrawControlPointHandles()
        {
            var controlPoints = _path.ControlPoints;
            
            // If no explicit points, use children
            if (controlPoints.Count == 0)
            {
                for (int i = 0; i < _path.transform.childCount; i++)
                {
                    Transform child = _path.transform.GetChild(i);
                    DrawPointHandle(child, i);
                }
            }
            else
            {
                for (int i = 0; i < controlPoints.Count; i++)
                {
                    if (controlPoints[i] != null)
                    {
                        DrawPointHandle(controlPoints[i], i);
                    }
                }
            }
        }
        
        private void DrawPointHandle(Transform point, int index)
        {
            float handleSize = HandleUtility.GetHandleSize(point.position) * 0.15f;
            
            // Position handle
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(point.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(point, "Move Control Point");
                point.position = newPosition;
                _path.MarkDirty();
                EditorUtility.SetDirty(_path);
            }
            
            // Draw clickable sphere
            Handles.color = Color.yellow;
            if (Handles.Button(point.position, Quaternion.identity, handleSize, handleSize * 0.5f, Handles.SphereHandleCap))
            {
                Selection.activeGameObject = point.gameObject;
            }
            
            // Draw index label
            Handles.Label(
                point.position + Vector3.up * (handleSize * 3f),
                $"[{index}]",
                new GUIStyle
                {
                    normal = { textColor = Color.white },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }
        
        private void DrawSceneViewUI()
        {
            Handles.BeginGUI();
            
            // Position the UI in the scene view
            Rect rect = new Rect(10, 10, 150, 60);
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            GUILayout.Label("Custom Path", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Point"))
            {
                AddNewPoint();
            }
            
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
    }
}
