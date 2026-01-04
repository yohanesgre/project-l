using UnityEngine;
using UnityEditor;

namespace MyGame.Features.World.Editor
{
    /// <summary>
    /// Custom editor for RoadGenerator providing convenient buttons and tools.
    /// </summary>
    [CustomEditor(typeof(RoadGenerator))]
    public class RoadGeneratorEditor : UnityEditor.Editor
    {
        private RoadGenerator _generator;
        private bool _showQuickAddSection = true;

        private void OnEnable()
        {
            _generator = (RoadGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // Generation buttons
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Road", GUILayout.Height(30)))
            {
                Undo.RecordObject(_generator, "Generate Road");
                _generator.GenerateRoad();
                EditorUtility.SetDirty(_generator);
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Road", GUILayout.Height(30)))
            {
                Undo.RecordObject(_generator, "Clear Road");
                _generator.ClearMesh();
                EditorUtility.SetDirty(_generator);
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Collect Child Waypoints"))
            {
                Undo.RecordObject(_generator, "Collect Waypoints");
                _generator.CollectChildWaypoints();
                EditorUtility.SetDirty(_generator);
            }
            
            EditorGUILayout.Space(10);
            
            // Quick add section
            _showQuickAddSection = EditorGUILayout.Foldout(_showQuickAddSection, "Quick Add Waypoints", true);
            
            if (_showQuickAddSection)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.HelpBox(
                    "Click 'Add Waypoint at Scene View' then click in the Scene view to place waypoints.\n" +
                    "Or use the button below to add a waypoint at the generator's position.",
                    MessageType.Info
                );
                
                if (GUILayout.Button("Add Waypoint Here"))
                {
                    AddWaypointAtPosition(_generator.transform.position + Vector3.forward * 5f * _generator.Waypoints.Count);
                }
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Create Circle Path (8 points)"))
                {
                    CreateCirclePath(8, 10f);
                }
                
                if (GUILayout.Button("Create Oval Path (12 points)"))
                {
                    CreateOvalPath(12, 15f, 8f);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            // Statistics
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Waypoints: {_generator.Waypoints.Count}");
            
            if (_generator.GeneratedMesh != null)
            {
                EditorGUILayout.LabelField($"Vertices: {_generator.GeneratedMesh.vertexCount}");
                EditorGUILayout.LabelField($"Triangles: {_generator.GeneratedMesh.triangles.Length / 3}");
            }
            else
            {
                EditorGUILayout.LabelField("No mesh generated", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void AddWaypointAtPosition(Vector3 position)
        {
            Undo.RecordObject(_generator, "Add Waypoint");
            RoadWaypoint waypoint = _generator.AddWaypoint(position);
            Undo.RegisterCreatedObjectUndo(waypoint.gameObject, "Add Waypoint");
            Selection.activeGameObject = waypoint.gameObject;
            EditorUtility.SetDirty(_generator);
        }

        private void CreateCirclePath(int pointCount, float radius)
        {
            // Clear existing child waypoints
            ClearChildWaypoints();
            
            Vector3 center = _generator.transform.position;
            
            for (int i = 0; i < pointCount; i++)
            {
                float angle = (float)i / pointCount * Mathf.PI * 2f;
                Vector3 position = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                
                AddWaypointAtPosition(position);
            }
            
            // Set to loop mode
            SerializedObject so = new SerializedObject(_generator);
            so.FindProperty("_isLoop").boolValue = true;
            so.ApplyModifiedProperties();
            
            _generator.GenerateRoad();
        }

        private void CreateOvalPath(int pointCount, float radiusX, float radiusZ)
        {
            // Clear existing child waypoints
            ClearChildWaypoints();
            
            Vector3 center = _generator.transform.position;
            
            for (int i = 0; i < pointCount; i++)
            {
                float angle = (float)i / pointCount * Mathf.PI * 2f;
                Vector3 position = center + new Vector3(
                    Mathf.Cos(angle) * radiusX,
                    0f,
                    Mathf.Sin(angle) * radiusZ
                );
                
                AddWaypointAtPosition(position);
            }
            
            // Set to loop mode
            SerializedObject so = new SerializedObject(_generator);
            so.FindProperty("_isLoop").boolValue = true;
            so.ApplyModifiedProperties();
            
            _generator.GenerateRoad();
        }

        private void ClearChildWaypoints()
        {
            var children = _generator.GetComponentsInChildren<RoadWaypoint>();
            foreach (var child in children)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
            
            _generator.CollectChildWaypoints();
        }

        private void OnSceneGUI()
        {
            // Draw handles for waypoints
            if (_generator.Waypoints == null) return;
            
            for (int i = 0; i < _generator.Waypoints.Count; i++)
            {
                var waypoint = _generator.Waypoints[i];
                if (waypoint == null) continue;
                
                EditorGUI.BeginChangeCheck();
                
                // Position handle
                Vector3 newPosition = Handles.PositionHandle(waypoint.Position, Quaternion.identity);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(waypoint.transform, "Move Waypoint");
                    waypoint.transform.position = newPosition;
                    
                    // Regenerate road if auto-regenerate is enabled
                    SerializedObject so = new SerializedObject(_generator);
                    if (so.FindProperty("_autoRegenerate").boolValue)
                    {
                        _generator.GenerateRoad();
                    }
                }
                
                // Draw label
                Handles.Label(waypoint.Position + Vector3.up * 2f, $"WP {i}", EditorStyles.whiteBoldLabel);
            }
        }
    }
}
