using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Core.Checkpoint;

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Manages the pause menu functionality, including pausing/resuming the game,
    /// handling button actions, and managing input control schemes.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The main GameObject for the pause menu panel.")]
        [SerializeField] private GameObject pauseMenuPanel;
        [Tooltip("The button that should be selected by default when the menu opens.")]
        [SerializeField] private Button firstSelectedButton;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            // Ensure the menu is hidden on start.
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Subscribe to input and button events.
            if (InputManager.Instance?.Controls != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed += OnPauseInput;
            }
            resumeButton?.onClick.AddListener(ResumeGame);
            restartButton?.onClick.AddListener(RestartLevel);
            mainMenuButton?.onClick.AddListener(GoToMainMenu);
        }

        private void OnDisable()
        {
            // Unsubscribe from all events.
            if (InputManager.Instance?.Controls != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed -= OnPauseInput;
            }
            resumeButton?.onClick.RemoveListener(ResumeGame);
            restartButton?.onClick.RemoveListener(RestartLevel);
            mainMenuButton?.onClick.RemoveListener(GoToMainMenu);
        }
        
        private void OnPauseInput(InputAction.CallbackContext context)
        {
            TogglePause();
        }

        public void TogglePause()
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
        
        private void PauseGame()
        {
            if (IsPaused) return;
            IsPaused = true;
            
            Time.timeScale = 0f;
            InputManager.Instance?.EnableUIControls();
            
            pauseMenuPanel?.SetActive(true);
            firstSelectedButton?.Select();
            uiSoundFeedback?.PlayOpen();
        }

        private void ResumeGame()
        {
            if (!IsPaused) return;
            IsPaused = false;
            
            Time.timeScale = 1f;
            InputManager.Instance?.EnablePlayerControls();
            
            pauseMenuPanel?.SetActive(false);
            uiSoundFeedback?.PlayClose();
        }

        private void RestartLevel()
        {
            uiSoundFeedback?.PlayClick();
            // Important: Reset timescale before loading a new scene.
            Time.timeScale = 1f;
            CheckpointManager.ResetCheckpointData();
            SceneLoader.Instance?.ReloadCurrentLevel();
        }

        private void GoToMainMenu()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f;
            CheckpointManager.ResetCheckpointData();
            SceneLoader.Instance?.LoadMenu();
        }
    }
}