using UnityEngine;
using UnityEditor;
using MyGame.Features.Character;
using MyGame.Features.Character.Data;
using MyGame.Features.World;
using System.Collections.Generic;

namespace MyGame.Features.Character.Editor
{
    public class SceneSetupHelper
    {
        [MenuItem("MyGame/Setup/Link Action Sequence")]
        public static void LinkActionSequence()
        {
            GameObject directorGO = GameObject.Find("ActionDirector");
            if (directorGO == null)
            {
                Debug.LogError("ActionDirector not found!");
                return;
            }

            var manager = directorGO.GetComponent<CharacterActionManager>();
            if (manager == null)
            {
                Debug.LogError("CharacterActionManager component missing!");
                return;
            }

            // Load Asset
            string assetPath = "Assets/_Project/Features/Character/Data/Sequences/RidingStopSequence.asset";
            var sequence = AssetDatabase.LoadAssetAtPath<CharacterActionSequence>(assetPath);
            if (sequence == null)
            {
                Debug.LogError($"Could not load sequence asset at {assetPath}");
                return;
            }

            // Assign Asset (using SerializedObject for Undo support)
            SerializedObject so = new SerializedObject(manager);
            so.Update();
            
            SerializedProperty sequenceProp = so.FindProperty("_sequenceAsset");
            sequenceProp.objectReferenceValue = sequence;

            // Setup Bindings
            SerializedProperty bindingsProp = so.FindProperty("_sceneBindings");
            bindingsProp.ClearArray();
            
            // Find Path
            var path = GameObject.FindObjectOfType<CustomPath>();
            if (path != null)
            {
                bindingsProp.InsertArrayElementAtIndex(0);
                SerializedProperty element = bindingsProp.GetArrayElementAtIndex(0);
                element.FindPropertyRelative("key").stringValue = "MainPath";
                element.FindPropertyRelative("target").objectReferenceValue = path.gameObject;
                Debug.Log($"Bound 'MainPath' to '{path.name}'");
            }
            else
            {
                Debug.LogWarning("CustomPath not found in scene!");
            }

            so.ApplyModifiedProperties();
            
            // Clear legacy actions list to avoid confusion (optional, but cleaner)
            // SerializedProperty actionsProp = so.FindProperty("_actions");
            // actionsProp.ClearArray();
            // so.ApplyModifiedProperties();

            Debug.Log("Successfully linked Action Sequence to Director!");
        }
    }
}
