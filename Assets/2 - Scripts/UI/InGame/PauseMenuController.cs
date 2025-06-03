// --- START OF FILE PauseMenuController.cs ---
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Scripts.Core; 
using Scripts.Core.Audio;
using Scripts.Checkpoints;
using Scripts.Items.Checkpoint; // Namespace correcto para CheckpointManager

namespace Scripts.UI.InGame
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The main panel for the pause menu.")]
        [SerializeField] private GameObject pauseMenuPanel;
        [Tooltip("The button initially selected when the pause menu opens.")]
        [SerializeField] private Button firstSelectedButtonPauseMenu;

        [Header("Pause Menu Buttons")]
        [SerializeField] private Button btn_Resume;
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;

        [Header("State")]
        [SerializeField] private bool isPaused = false; 

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        private void Awake()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }

        private void Start()
        {
            SubscribeToPauseInput();
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null) {
                 SubscribeToPauseInput(); 
            }

            btn_Resume?.onClick.AddListener(HandleResumeClicked);
            btn_RestartLevel?.onClick.AddListener(HandleRestartLevelClicked);
            btn_MainMenu?.onClick.AddListener(HandleMainMenuClicked);
        }

        private void OnDisable()
        {
            if (InputManager.Instance?.Controls?.Player.PauseMenu != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed -= OnPauseInputPerformed;
            }

            btn_Resume?.onClick.RemoveListener(HandleResumeClicked);
            btn_RestartLevel?.onClick.RemoveListener(HandleRestartLevelClicked);
            btn_MainMenu?.onClick.RemoveListener(HandleMainMenuClicked);
        }
        
        private void SubscribeToPauseInput()
        {
            if (InputManager.Instance != null && InputManager.Instance.Controls?.Player.PauseMenu != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed -= OnPauseInputPerformed;
                InputManager.Instance.Controls.Player.PauseMenu.performed += OnPauseInputPerformed;
            }
        }

        private void OnPauseInputPerformed(InputAction.CallbackContext context)
        {
            TogglePauseMenu();
        }

        private void TogglePauseMenu() 
        { 
            if (isPaused) ResumeGame(); 
            else PauseGame(); 
        }

        private void PauseGame() 
        { 
            isPaused = true; Time.timeScale = 0f; 
            InputManager.Instance?.EnableUIControls(); 
            pauseMenuPanel?.SetActive(true); 
            firstSelectedButtonPauseMenu?.Select(); 
            uiSoundFeedback?.PlayOpen(); 
        }

        private void ResumeGame() 
        { 
            isPaused = false; Time.timeScale = 1f; 
            InputManager.Instance?.EnablePlayerControls(); 
            pauseMenuPanel?.SetActive(false); 
            uiSoundFeedback?.PlayClose(); 
        }

        private void HandleResumeClicked() 
        { 
            Debug.Log("PauseMenuController: Game resumed.", this); // For debugging
            uiSoundFeedback?.PlayClick(); 
            ResumeGame(); 
        }

        private void HandleRestartLevelClicked() 
        { 
            Debug.Log("PauseMenuController: Level restarted.", this); // For debugging
            uiSoundFeedback?.PlayClick(); 
            RestartLevel(); 
        }

        private void HandleMainMenuClicked() 
        { 
            Debug.Log("PauseMenuController: Navigating to Main Menu.", this); // For debugging
            uiSoundFeedback?.PlayClick(); 
            GoToMainMenu(); 
        }

        // --- Level Loading Logic ---
        private void RestartLevel() 
        { 
            Time.timeScale = 1f; 
            InputManager.Instance?.EnablePlayerControls(); 
            CheckpointManager.ResetCheckpointData(); 
            SceneLoader.Instance?.ReloadCurrentLevelScene();
        }
        private void GoToMainMenu() 
        { 
            Time.timeScale = 1f; 
            InputManager.Instance?.EnablePlayerControls(); 
            CheckpointManager.ResetCheckpointData(); 
            SceneLoader.Instance?.LoadMenu(); 
        }
    }
}