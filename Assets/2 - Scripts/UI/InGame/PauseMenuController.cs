using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Scripts.Core;

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Handles pause menu logic: toggling visibility, pausing time, and switching input maps.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button firstSelectedButton;

        [Header("Settings")]
        [SerializeField] private bool isPaused = false;
        
        [Header("Menu Buttons")]
        [SerializeField] private Button btn_Resume;
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;

        private void Awake()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

        private void OnEnable()
        {
            InputManager.Instance.Controls.Player.PauseMenu.performed += OnPausePressed;
            
            btn_Resume?.onClick.AddListener(ResumeGame);
            btn_RestartLevel?.onClick.AddListener(RestartLevel);
            btn_MainMenu?.onClick.AddListener(GoToMainMenu);
        }

        private void OnDisable()
        {
            InputManager.Instance.Controls.Player.PauseMenu.performed -= OnPausePressed;
            
            btn_Resume?.onClick.RemoveListener(ResumeGame);
            btn_RestartLevel?.onClick.RemoveListener(RestartLevel);
            btn_MainMenu?.onClick.RemoveListener(GoToMainMenu);
        }

        /// <summary>
        /// Toggles the pause menu when the pause input is triggered.
        /// </summary>
        /// <param name="ctx">Input context.</param>
        private void OnPausePressed(InputAction.CallbackContext ctx)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        /// <summary>
        /// Pauses the game, shows the UI, and switches to UI controls.
        /// </summary>
        private void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            InputManager.Instance.EnableUIControls();

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);

            if (firstSelectedButton != null)
                firstSelectedButton.Select();

            // Debug.Log("Game Paused");
        }

        /// <summary>
        /// Resumes gameplay, hides the UI, and switches back to gameplay controls.
        /// </summary>
        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            InputManager.Instance.EnablePlayerControls();

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            // Debug.Log("Game Resumed");
        }
        
        private void RestartLevel()
        {
            Time.timeScale = 1f;
            InputManager.Instance.EnablePlayerControls();

            // Probably the sceneLoader will maintain the scene intact.
            int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            SceneLoader.Instance.LoadLevel(currentSceneIndex);
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            InputManager.Instance.EnablePlayerControls();

            SceneLoader.Instance.LoadMenu();
        }
    }
}
