using UnityEngine;
using MyGame.Features.Dialogue;
using System.Collections.Generic;

namespace MyGame.Features.UI
{
    /// <summary>
    /// The central orchestrator for high-level UI states.
    /// It listens to the GameManager and tells specific UIViews to show or hide.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager gameManager;

        [Header("Views")]
        [Tooltip("The view containing the Main Menu")]
        [SerializeField] private UIView mainMenuView;

        [Tooltip("The parent view containing Dialogue, History, etc.")]
        [SerializeField] private UIView gameplayView;

        // Future proofing: Add PauseMenuView here later
        // [SerializeField] private UIView pauseMenuView;

        private void OnEnable()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
                // Sync immediate state
                OnGameStateChanged(gameManager.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Title:
                    ShowMainMenu();
                    break;

                case GameState.Loading:
                    // Optionally show a loading view, hide others
                    mainMenuView?.Hide();
                    gameplayView?.Hide();
                    break;

                case GameState.Playing:
                    ShowGameplay();
                    break;

                case GameState.Paused:
                    // Keep gameplay visible (or dimmed)
                    // If we had a pause menu:
                    // pauseMenuView?.Show();
                    break;
            }
        }

        private void ShowMainMenu()
        {
            gameplayView?.Hide();
            mainMenuView?.Show();
        }

        private void ShowGameplay()
        {
            mainMenuView?.Hide();
            gameplayView?.Show();
        }
    }
}
