using System;
using UnityEngine;
using MyGame.Core;

namespace MyGame.Features.Character.Data
{
    public enum ActionType
    {
        Idle,
        Ride,
        Teleport
    }

    [Serializable]
    public class CharacterActionDefinition
    {
        public string actionName = "New Action";
        public ActionType actionType;
        
        [Header("Camera")]
        [Tooltip("Camera mode to use during this action.")]
        public CameraController.CameraMode cameraMode = CameraController.CameraMode.PathFollow;

        [Header("Transition")]
        public bool fadeOut = false;
        public bool fadeIn = false;
        public float fadeDuration = 0.5f;
        
        [Header("Settings")]
        [Tooltip("Duration for Idle action")]
        public float duration = 2f;
        
        [Tooltip("Key to find the target object (Path or Teleport Target) in the scene bindings.")]
        public string targetKey;
        
        [Header("Ride Specific")]
        [Range(0f, 1f)] public float startProgress = 0f;
        [Range(0f, 1f)] public float finishProgress = 1f;
        public bool loop = false;
        
        [Header("Events")]
        [Tooltip("Optional: Trigger a scene transition command on completion (e.g. 'SceneA_to_SceneB')")]
        public string onCompleteCommand;
    }
}
