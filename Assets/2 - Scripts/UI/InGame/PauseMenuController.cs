using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Scripts.Core;
using Scripts.Core.Audio;

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Handles pause menu logic: toggling visibility, pausing time, switching input maps, and playing UI sounds.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button firstSelectedButton;

        [Header("Menu Buttons")]
        [SerializeField] private Button btn_Resume;
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;

        [Header("Settings")]
        [SerializeField] private bool isPaused = false;

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        private void Awake()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

        private void OnEnable()
        {
            InputManager.Instance.Controls.Player.PauseMenu.performed += OnPausePressed;

            btn_Resume?.onClick.AddListener(() =>
            {
                uiSoundFeedback?.PlayClick();
                ResumeGame();
            });

            btn_RestartLevel?.onClick.AddListener(() =>
            {
                uiSoundFeedback?.PlayClick();
                RestartLevel();
            });

            btn_MainMenu?.onClick.AddListener(() =>
            {
                uiSoundFeedback?.PlayClick();
                GoToMainMenu();
            });
        }

        private void OnDisable()
        {
            InputManager.Instance.Controls.Player.PauseMenu.performed -= OnPausePressed;

            btn_Resume?.onClick.RemoveAllListeners();
            btn_RestartLevel?.onClick.RemoveAllListeners();
            btn_MainMenu?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Toggles the pause menu when the pause input is triggered.
        /// </summary>
        private void OnPausePressed(InputAction.CallbackContext ctx)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        private void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            InputManager.Instance.EnableUIControls();
            pauseMenuPanel?.SetActive(true);
            firstSelectedButton?.Select();

            // uiSoundFeedback?.PlayOpen(); // Optional sound when opening pause menu
        }

        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            InputManager.Instance.EnablePlayerControls();
            pauseMenuPanel?.SetActive(false);

            // uiSoundFeedback?.PlayClose(); // Optional sound when closing pause menu
        }

        private void RestartLevel()
        {
            Time.timeScale = 1f;
            InputManager.Instance.EnablePlayerControls();

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