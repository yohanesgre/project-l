using UnityEngine;
using UnityEngine.UIElements;
using MyGame.Core.Save;
using MyGame.Core.Audio;
using MyGame.Features.Dialogue;

namespace MyGame.Features.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIDocument uiDocument;

        [Header("UI Names")]
        [SerializeField] private string newGameButtonName = "btn-new-game";
        [SerializeField] private string continueButtonName = "btn-continue";
        [SerializeField] private string quitButtonName = "btn-quit";
        [SerializeField] private string containerName = "main-menu-container";

        private VisualElement _container;
        private Button _newGameButton;
        private Button _continueButton;
        private Button _quitButton;

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();

            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
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
            if (newState == GameState.Title)
            {
                UpdateContinueButtonState();
            }
        }

        private void Start()
        {
            InitializeUI();
            
            // Initial button state check
            if (gameManager != null)
            {
                UpdateContinueButtonState();
            }
        }

        private void InitializeUI()
        {
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            // No container logic needed here, handled by UIManager/UIView
            
            _newGameButton = root.Q<Button>(newGameButtonName);
            _continueButton = root.Q<Button>(continueButtonName);
            _quitButton = root.Q<Button>(quitButtonName);

            if (_newGameButton != null)
                _newGameButton.clicked += OnNewGameClicked;

            if (_continueButton != null)
            {
                _continueButton.clicked += OnContinueClicked;
                UpdateContinueButtonState();
            }

            if (_quitButton != null)
                _quitButton.clicked += OnQuitClicked;
        }

        public void RefreshState()
        {
            UpdateContinueButtonState();
        }

        private void UpdateContinueButtonState()
        {
            if (_continueButton == null) return;

            // Check if any save data exists
            bool hasSave = gameManager != null && gameManager.CanContinue();
            _continueButton.SetEnabled(hasSave);
            
            if (hasSave)
                _continueButton.RemoveFromClassList("button-disabled");
            else
                _continueButton.AddToClassList("button-disabled");
        }

        private void OnNewGameClicked()
        {
            PlayButtonClickSFX();
            if (gameManager != null)
            {
                gameManager.StartNewGame();
            }
        }

        private void OnContinueClicked()
        {
            PlayButtonClickSFX();
            if (gameManager != null)
            {
                // Continue from the most recent save
                gameManager.ContinueFromLastSave();
            }
        }

        private void OnQuitClicked()
        {
            PlayButtonClickSFX();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void PlayButtonClickSFX()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("button_click");
            }
        }
    }
}
