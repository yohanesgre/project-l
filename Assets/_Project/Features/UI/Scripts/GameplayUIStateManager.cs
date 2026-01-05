using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using MyGame.Features.Dialogue;

namespace MyGame.Features.UI
{
    /// <summary>
    /// Manages the visibility of the entire Gameplay UI by toggling the display style
    /// of UIDocuments. This ensures visual trees remain valid for external controllers.
    /// Attached to the parent "UI" GameObject.
    /// </summary>
    public class GameplayUIStateManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        
        // Cache UIDocuments to avoid GetComponent calls at runtime
        private List<UIDocument> _uiDocuments = new List<UIDocument>();

        private void Awake()
        {
            // Find all UIDocuments in children (Dialogue UI, Choice UI, etc.)
            GetComponentsInChildren(true, _uiDocuments);
        }

        private void OnEnable()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(gameManager.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Title:
                case GameState.Loading:
                    SetUIVisibility(false);
                    break;
                case GameState.Playing:
                case GameState.Paused:
                    SetUIVisibility(true);
                    break;
            }
        }

        private void SetUIVisibility(bool visible)
        {
            foreach (var doc in _uiDocuments)
            {
                if (doc == null || doc.rootVisualElement == null) continue;

                // Toggle display style instead of GameObject activity
                // This keeps the VisualElement tree alive so UIControllers don't lose references
                doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
