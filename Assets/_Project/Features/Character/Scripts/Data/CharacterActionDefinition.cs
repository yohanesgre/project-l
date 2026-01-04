using System;
using UnityEngine;

namespace MyGame.Features.Character.Data
{
    public enum ActionType
    {
        Idle,
        Ride,
        WalkTo
    }

    [Serializable]
    public class CharacterActionDefinition
    {
        public string actionName = "New Action";
        public ActionType actionType;
        
        [Header("Transition")]
        public bool fadeOut = false;
        public bool fadeIn = false;
        public float fadeDuration = 0.5f;
        
        [Header("Settings")]
        [Tooltip("Duration for Idle action")]
        public float duration = 2f;
        
        [Tooltip("Key to find the target object (Path or Walk Target) in the scene bindings.")]
        public string targetKey;
        
        [Header("Ride Specific")]
        [Range(0f, 1f)] public float startProgress = 0f;
        [Range(0f, 1f)] public float finishProgress = 1f;
        
        [Header("Walk Specific")]
        public float walkSpeed = 2f;
        public float stopDistance = 0.1f;
        
        [Header("Events")]
        [Tooltip("Optional: Trigger a scene transition command on completion (e.g. 'SceneA_to_SceneB')")]
        public string onCompleteCommand;
    }
}
