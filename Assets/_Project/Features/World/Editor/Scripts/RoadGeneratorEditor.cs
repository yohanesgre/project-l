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
        private bool _showDirtLayerSection = true;
        private bool _showRailsSection = true;
        private bool _showSignsSection = true;
        private bool _showGrassSection = true;
        
        // Serialized properties for custom drawing
        private SerializedProperty _waypointsProp;
        private SerializedProperty _isLoopProp;
        private SerializedProperty _roadWidthProp;
        private SerializedProperty _segmentsPerWaypointProp;
        private SerializedProperty _roadMaterialProp;
        private SerializedProperty _uvTilingProp;
        private SerializedProperty _enableDirtLayerProp;
        private SerializedProperty _dirtMaterialProp;
        private SerializedProperty _dirtLayerExtraWidthProp;
        private SerializedProperty _dirtLayerOffsetProp;
        private SerializedProperty _dirtUvTilingProp;
        private SerializedProperty _enableRailsProp;
        private SerializedProperty _railPrefabProp;
        private SerializedProperty _railSpacingProp;
        private SerializedProperty _railEdgeOffsetProp;
        private SerializedProperty _railVerticalOffsetProp;
        private SerializedProperty _railScaleProp;
        private SerializedProperty _railRotationOffsetProp;
        private SerializedProperty _railForwardAxisProp;
        private SerializedProperty _leftRailsProp;
        private SerializedProperty _rightRailsProp;
        
        // Sign properties
        private SerializedProperty _enableSignsProp;
        private SerializedProperty _signPrefabProp;
        private SerializedProperty _signSpacingProp;
        private SerializedProperty _signSpacingVariationProp;
        private SerializedProperty _signMinEdgeOffsetProp;
        private SerializedProperty _signMaxEdgeOffsetProp;
        private SerializedProperty _signVerticalOffsetProp;
        private SerializedProperty _signScaleProp;
        private SerializedProperty _signScaleVariationProp;
        private SerializedProperty _signsFaceRoadProp;
        private SerializedProperty _signRotationVariationProp;
        private SerializedProperty _leftSignsProp;
        private SerializedProperty _rightSignsProp;
        private SerializedProperty _signRandomSeedProp;
        
        // Grass properties
        private SerializedProperty _enableGrassProp;
        private SerializedProperty _grassPrefabProp;
        private SerializedProperty _grassSpacingProp;
        private SerializedProperty _grassSpacingVariationProp;
        private SerializedProperty _grassMinEdgeOffsetProp;
        private SerializedProperty _grassMaxEdgeOffsetProp;
        private SerializedProperty _grassVerticalOffsetProp;
        private SerializedProperty _grassScaleProp;
        private SerializedProperty _grassScaleVariationProp;
        private SerializedProperty _grassRotationVariationProp;
        private SerializedProperty _leftGrassProp;
        private SerializedProperty _rightGrassProp;
        private SerializedProperty _grassRandomSeedProp;
        
        private SerializedProperty _autoRegenerateProp;
        private SerializedProperty _generateColliderProp;
        private SerializedProperty _showPathGizmosProp;
        private SerializedProperty _showRoadEdgesProp;
        private SerializedProperty _pathColorProp;
        private SerializedProperty _edgeColorProp;

        private void OnEnable()
        {
            _generator = (RoadGenerator)target;
            
            // Cache serialized properties
            _waypointsProp = serializedObject.FindProperty("_waypoints");
            _isLoopProp = serializedObject.FindProperty("_isLoop");
            _roadWidthProp = serializedObject.FindProperty("_roadWidth");
            _segmentsPerWaypointProp = serializedObject.FindProperty("_segmentsPerWaypoint");
            _roadMaterialProp = serializedObject.FindProperty("_roadMaterial");
            _uvTilingProp = serializedObject.FindProperty("_uvTiling");
            _enableDirtLayerProp = serializedObject.FindProperty("_enableDirtLayer");
            _dirtMaterialProp = serializedObject.FindProperty("_dirtMaterial");
            _dirtLayerExtraWidthProp = serializedObject.FindProperty("_dirtLayerExtraWidth");
            _dirtLayerOffsetProp = serializedObject.FindProperty("_dirtLayerOffset");
            _dirtUvTilingProp = serializedObject.FindProperty("_dirtUvTiling");
            _enableRailsProp = serializedObject.FindProperty("_enableRails");
            _railPrefabProp = serializedObject.FindProperty("_railPrefab");
            _railSpacingProp = serializedObject.FindProperty("_railSpacing");
            _railEdgeOffsetProp = serializedObject.FindProperty("_railEdgeOffset");
            _railVerticalOffsetProp = serializedObject.FindProperty("_railVerticalOffset");
            _railScaleProp = serializedObject.FindProperty("_railScale");
            _railRotationOffsetProp = serializedObject.FindProperty("_railRotationOffset");
            _railForwardAxisProp = serializedObject.FindProperty("_railForwardAxis");
            _leftRailsProp = serializedObject.FindProperty("_leftRails");
            _rightRailsProp = serializedObject.FindProperty("_rightRails");
            
            // Sign properties
            _enableSignsProp = serializedObject.FindProperty("_enableSigns");
            _signPrefabProp = serializedObject.FindProperty("_signPrefab");
            _signSpacingProp = serializedObject.FindProperty("_signSpacing");
            _signSpacingVariationProp = serializedObject.FindProperty("_signSpacingVariation");
            _signMinEdgeOffsetProp = serializedObject.FindProperty("_signMinEdgeOffset");
            _signMaxEdgeOffsetProp = serializedObject.FindProperty("_signMaxEdgeOffset");
            _signVerticalOffsetProp = serializedObject.FindProperty("_signVerticalOffset");
            _signScaleProp = serializedObject.FindProperty("_signScale");
            _signScaleVariationProp = serializedObject.FindProperty("_signScaleVariation");
            _signsFaceRoadProp = serializedObject.FindProperty("_signsFaceRoad");
            _signRotationVariationProp = serializedObject.FindProperty("_signRotationVariation");
            _leftSignsProp = serializedObject.FindProperty("_leftSigns");
            _rightSignsProp = serializedObject.FindProperty("_rightSigns");
            _signRandomSeedProp = serializedObject.FindProperty("_signRandomSeed");
            
            // Grass properties
            _enableGrassProp = serializedObject.FindProperty("_enableGrass");
            _grassPrefabProp = serializedObject.FindProperty("_grassPrefab");
            _grassSpacingProp = serializedObject.FindProperty("_grassSpacing");
            _grassSpacingVariationProp = serializedObject.FindProperty("_grassSpacingVariation");
            _grassMinEdgeOffsetProp = serializedObject.FindProperty("_grassMinEdgeOffset");
            _grassMaxEdgeOffsetProp = serializedObject.FindProperty("_grassMaxEdgeOffset");
            _grassVerticalOffsetProp = serializedObject.FindProperty("_grassVerticalOffset");
            _grassScaleProp = serializedObject.FindProperty("_grassScale");
            _grassScaleVariationProp = serializedObject.FindProperty("_grassScaleVariation");
            _grassRotationVariationProp = serializedObject.FindProperty("_grassRotationVariation");
            _leftGrassProp = serializedObject.FindProperty("_leftGrass");
            _rightGrassProp = serializedObject.FindProperty("_rightGrass");
            _grassRandomSeedProp = serializedObject.FindProperty("_grassRandomSeed");
            
            _autoRegenerateProp = serializedObject.FindProperty("_autoRegenerate");
            _generateColliderProp = serializedObject.FindProperty("_generateCollider");
            _showPathGizmosProp = serializedObject.FindProperty("_showPathGizmos");
            _showRoadEdgesProp = serializedObject.FindProperty("_showRoadEdges");
            _pathColorProp = serializedObject.FindProperty("_pathColor");
            _edgeColorProp = serializedObject.FindProperty("_edgeColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Road Path Section
            EditorGUILayout.LabelField("Road Path", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_waypointsProp);
            EditorGUILayout.PropertyField(_isLoopProp);
            
            EditorGUILayout.Space(5);
            
            // Road Dimensions Section
            EditorGUILayout.LabelField("Road Dimensions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_roadWidthProp);
            EditorGUILayout.PropertyField(_segmentsPerWaypointProp);
            
            EditorGUILayout.Space(5);
            
            // Material Section
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_roadMaterialProp);
            EditorGUILayout.PropertyField(_uvTilingProp);
            
            EditorGUILayout.Space(5);
            
            // Dirt Layer Section (collapsible with toggle)
            DrawDirtLayerSection();
            
            EditorGUILayout.Space(5);
            
            // Rails Section (collapsible with toggle)
            DrawRailsSection();
            
            EditorGUILayout.Space(5);
            
            // Signs Section (collapsible with toggle)
            DrawSignsSection();
            
            EditorGUILayout.Space(5);
            
            // Grass Section (collapsible with toggle)
            DrawGrassSection();
            
            EditorGUILayout.Space(5);
            
            // Generation Settings Section
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoRegenerateProp);
            EditorGUILayout.PropertyField(_generateColliderProp);
            
            EditorGUILayout.Space(5);
            
            // Gizmo Settings Section
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showPathGizmosProp);
            EditorGUILayout.PropertyField(_showRoadEdgesProp);
            EditorGUILayout.PropertyField(_pathColorProp);
            EditorGUILayout.PropertyField(_edgeColorProp);
            
            serializedObject.ApplyModifiedProperties();
            
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
        
        private void DrawDirtLayerSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            _showDirtLayerSection = EditorGUILayout.Foldout(_showDirtLayerSection, "Dirt Layer", true, EditorStyles.foldoutHeader);
            
            // Toggle on the right side
            EditorGUI.BeginChangeCheck();
            bool dirtEnabled = EditorGUILayout.Toggle(_enableDirtLayerProp.boolValue, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                _enableDirtLayerProp.boolValue = dirtEnabled;
            }
            EditorGUILayout.EndHorizontal();
            
            if (_showDirtLayerSection && _enableDirtLayerProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_dirtMaterialProp, new GUIContent("Material"));
                EditorGUILayout.PropertyField(_dirtLayerExtraWidthProp, new GUIContent("Extra Width"));
                EditorGUILayout.PropertyField(_dirtLayerOffsetProp, new GUIContent("Vertical Offset"));
                EditorGUILayout.PropertyField(_dirtUvTilingProp, new GUIContent("UV Tiling"));
                EditorGUI.indentLevel--;
            }
            else if (!_enableDirtLayerProp.boolValue)
            {
                EditorGUILayout.LabelField("Enable to add a dirt/ground layer beneath the road", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRailsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            _showRailsSection = EditorGUILayout.Foldout(_showRailsSection, "Road Rails", true, EditorStyles.foldoutHeader);
            
            // Toggle on the right side
            EditorGUI.BeginChangeCheck();
            bool railsEnabled = EditorGUILayout.Toggle(_enableRailsProp.boolValue, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                _enableRailsProp.boolValue = railsEnabled;
            }
            EditorGUILayout.EndHorizontal();
            
            if (_showRailsSection && _enableRailsProp.boolValue)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_railPrefabProp, new GUIContent("Rail Prefab"));
                
                if (_railPrefabProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Assign a rail prefab (e.g., Railing.fbx) to generate rails.", MessageType.Warning);
                }
                
                EditorGUILayout.PropertyField(_railForwardAxisProp, new GUIContent("Forward Axis", "Which axis of the prefab points along the road"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Placement", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.PropertyField(_railSpacingProp, new GUIContent("Spacing", "Set to 0 to auto-calculate from prefab size"));
                if (_railSpacingProp.floatValue <= 0)
                {
                    EditorGUILayout.HelpBox("Spacing = 0: Auto-detecting from prefab bounds", MessageType.Info);
                }
                
                EditorGUILayout.PropertyField(_railEdgeOffsetProp, new GUIContent("Edge Offset"));
                EditorGUILayout.PropertyField(_railVerticalOffsetProp, new GUIContent("Vertical Offset"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Transform", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.PropertyField(_railScaleProp, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(_railRotationOffsetProp, new GUIContent("Rotation Offset"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Sides");
                _leftRailsProp.boolValue = GUILayout.Toggle(_leftRailsProp.boolValue, "Left", EditorStyles.miniButtonLeft);
                _rightRailsProp.boolValue = GUILayout.Toggle(_rightRailsProp.boolValue, "Right", EditorStyles.miniButtonRight);
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            else if (!_enableRailsProp.boolValue)
            {
                EditorGUILayout.LabelField("Enable to spawn rail prefabs along road edges", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSignsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            _showSignsSection = EditorGUILayout.Foldout(_showSignsSection, "Road Signs", true, EditorStyles.foldoutHeader);
            
            // Toggle on the right side
            EditorGUI.BeginChangeCheck();
            bool signsEnabled = EditorGUILayout.Toggle(_enableSignsProp.boolValue, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                _enableSignsProp.boolValue = signsEnabled;
            }
            EditorGUILayout.EndHorizontal();
            
            if (_showSignsSection && _enableSignsProp.boolValue)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_signPrefabProp, new GUIContent("Sign Prefab"));
                
                if (_signPrefabProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Assign a sign prefab (e.g., Signage.fbx) to generate signs.", MessageType.Warning);
                }
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Placement", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.PropertyField(_signSpacingProp, new GUIContent("Spacing", "Average distance between signs"));
                EditorGUILayout.PropertyField(_signSpacingVariationProp, new GUIContent("Spacing Variation", "Random offset to spacing"));
                EditorGUILayout.PropertyField(_signMinEdgeOffsetProp, new GUIContent("Min Edge Offset"));
                EditorGUILayout.PropertyField(_signMaxEdgeOffsetProp, new GUIContent("Max Edge Offset"));
                EditorGUILayout.PropertyField(_signVerticalOffsetProp, new GUIContent("Vertical Offset"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Transform", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.PropertyField(_signScaleProp, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(_signScaleVariationProp, new GUIContent("Scale Variation"));
                EditorGUILayout.PropertyField(_signsFaceRoadProp, new GUIContent("Face Road", "If true, signs face toward road"));
                EditorGUILayout.PropertyField(_signRotationVariationProp, new GUIContent("Rotation Variation"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.PropertyField(_signRandomSeedProp, new GUIContent("Random Seed", "0 = random each time"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Sides");
                _leftSignsProp.boolValue = GUILayout.Toggle(_leftSignsProp.boolValue, "Left", EditorStyles.miniButtonLeft);
                _rightSignsProp.boolValue = GUILayout.Toggle(_rightSignsProp.boolValue, "Right", EditorStyles.miniButtonRight);
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            else if (!_enableSignsProp.boolValue)
            {
                EditorGUILayout.LabelField("Enable to spawn scattered signs along road edges", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGrassSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            _showGrassSection = EditorGUILayout.Foldout(_showGrassSection, "Road Grass", true, EditorStyles.foldoutHeader);
            
            // Toggle on the right side
            EditorGUI.BeginChangeCheck();
            bool grassEnabled = EditorGUILayout.Toggle(_enableGrassProp.boolValue, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                _enableGrassProp.boolValue = grassEnabled;
            }
            EditorGUILayout.EndHorizontal();
            
            if (_showGrassSection && _enableGrassProp.boolValue)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_grassPrefabProp, new GUIContent("Grass Prefab"));
                
                if (_grassPrefabProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Assign a grass prefab (e.g., Grass_1.fbx) to generate grass.", MessageType.Warning);
                }
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Placement", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.PropertyField(_grassSpacingProp, new GUIContent("Spacing", "Average distance between grass instances"));
                EditorGUILayout.PropertyField(_grassSpacingVariationProp, new GUIContent("Spacing Variation", "Random offset to spacing"));
                EditorGUILayout.PropertyField(_grassMinEdgeOffsetProp, new GUIContent("Min Edge Offset"));
                EditorGUILayout.PropertyField(_grassMaxEdgeOffsetProp, new GUIContent("Max Edge Offset"));
                EditorGUILayout.PropertyField(_grassVerticalOffsetProp, new GUIContent("Vertical Offset"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Transform", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.PropertyField(_grassScaleProp, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(_grassScaleVariationProp, new GUIContent("Scale Variation"));
                EditorGUILayout.PropertyField(_grassRotationVariationProp, new GUIContent("Rotation Variation", "360 = full random rotation"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.PropertyField(_grassRandomSeedProp, new GUIContent("Random Seed", "0 = random each time"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Sides");
                _leftGrassProp.boolValue = GUILayout.Toggle(_leftGrassProp.boolValue, "Left", EditorStyles.miniButtonLeft);
                _rightGrassProp.boolValue = GUILayout.Toggle(_rightGrassProp.boolValue, "Right", EditorStyles.miniButtonRight);
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            else if (!_enableGrassProp.boolValue)
            {
                EditorGUILayout.LabelField("Enable to spawn scattered grass along road edges", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
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
