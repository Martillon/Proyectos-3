// --- START OF FILE PauseMenuController.cs ---
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Scripts.Core; // For InputManager, SceneLoader, GameConstants
using Scripts.Core.Audio;
using Scripts.Checkpoints;
using Scripts.Items.Checkpoint; // Corrected namespace for CheckpointManager

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Handles pause menu logic: toggling visibility, pausing time, switching input maps,
    /// navigating menu options (including opening an options submenu), and playing UI sounds.
    /// Assumes the Options Panel is an existing GameObject in the scene, initially disabled.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The main panel for the pause menu.")]
        [SerializeField] private GameObject pauseMenuPanel;
        [Tooltip("The button initially selected when the pause menu opens.")]
        [SerializeField] private Button firstSelectedButtonPauseMenu;

        [Header("Options Submenu")]
        [Tooltip("Reference to the GameObject in the scene that IS the options panel. Should be initially disabled.")]
        [SerializeField] private GameObject optionsPanelObject; // Renamed from optionsPanelPrefab
        [Tooltip("Button IN THE OPTIONS PANEL used to return to the pause menu. Assign from the Options Panel hierarchy.")]
        [SerializeField] private Button optionsPanelReturnButton; 
        [Tooltip("The button to be selected first when the options panel is opened from the pause menu (e.g., 'Video' tab button).")]
        [SerializeField] private Button firstSelectedButtonOptionsMenu;

        [Header("Pause Menu Buttons")]
        [SerializeField] private Button btn_Resume;
        [SerializeField] private Button btn_Options;
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;

        [Header("State")]
        [SerializeField] private bool isPaused = false; // Serialized for debugging, primarily internal state
        private bool isOptionsPanelOpen = false; // Renamed for clarity

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        // No longer need activeOptionsPanelInstance if optionsPanelObject is the direct reference
        // private GameObject activeOptionsPanelInstance; 

        private void Awake()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (optionsPanelObject != null)
            {
                optionsPanelObject.SetActive(false); // Ensure options panel is initially hidden
            }
            else
            {
                // Only log error if the btn_Options exists, implying an options panel was intended.
                if (btn_Options != null)
                    Debug.LogError("PauseMenuController: 'Options Panel Object' is not assigned, but an 'Options' button exists. Options functionality will be broken.", this);
            }

            if (btn_Options != null && optionsPanelReturnButton == null)
            {
                Debug.LogError("PauseMenuController: 'Options Panel Return Button' is not assigned. Returning from options will not work correctly.", this);
            }
        }

        private void OnEnable()
        {
            if (InputManager.Instance?.Controls?.Player.PauseMenu != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed += OnPauseInputPerformed;
            }

            btn_Resume?.onClick.AddListener(HandleResumeClicked);
            btn_Options?.onClick.AddListener(HandleOptionsClicked);
            btn_RestartLevel?.onClick.AddListener(HandleRestartLevelClicked);
            btn_MainMenu?.onClick.AddListener(HandleMainMenuClicked);
            optionsPanelReturnButton?.onClick.AddListener(HandleReturnFromOptionsClicked);
        }

        private void OnDisable()
        {
            if (InputManager.Instance?.Controls?.Player.PauseMenu != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed -= OnPauseInputPerformed;
            }

            btn_Resume?.onClick.RemoveListener(HandleResumeClicked);
            btn_Options?.onClick.RemoveListener(HandleOptionsClicked);
            btn_RestartLevel?.onClick.RemoveListener(HandleRestartLevelClicked);
            btn_MainMenu?.onClick.RemoveListener(HandleMainMenuClicked);
            optionsPanelReturnButton?.onClick.RemoveListener(HandleReturnFromOptionsClicked);
        }

        private void OnPauseInputPerformed(InputAction.CallbackContext context)
        {
            if (isOptionsPanelOpen)
            {
                HandleReturnFromOptionsClicked(); // Escape closes Options if open
            }
            else
            {
                TogglePauseMenu(); // Else, toggle Pause Menu
            }
        }

        private void TogglePauseMenu()
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        private void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            InputManager.Instance?.EnableUIControls();
            isOptionsPanelOpen = false; // Reset options state
            optionsPanelObject?.SetActive(false); // Ensure options panel is hidden
            
            pauseMenuPanel?.SetActive(true);
            firstSelectedButtonPauseMenu?.Select();
            uiSoundFeedback?.PlayOpen();
        }

        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            InputManager.Instance?.EnablePlayerControls();
            isOptionsPanelOpen = false; // Reset options state
            optionsPanelObject?.SetActive(false); // Ensure options panel is hidden
            pauseMenuPanel?.SetActive(false);

            uiSoundFeedback?.PlayClose();
        }

        private void HandleResumeClicked()
        {
            uiSoundFeedback?.PlayClick();
            ResumeGame();
        }

        private void HandleRestartLevelClicked()
        {
            uiSoundFeedback?.PlayClick();
            RestartLevel();
        }

        private void HandleMainMenuClicked()
        {
            uiSoundFeedback?.PlayClick();
            GoToMainMenu();
        }

        // --- Options Submenu Logic ---
        private void HandleOptionsClicked()
        {
            uiSoundFeedback?.PlayClick();
            OpenOptionsPanel();
        }

        private void OpenOptionsPanel()
        {
            if (optionsPanelObject == null)
            {
                Debug.LogError("PauseMenuController: Cannot open Options Panel because 'Options Panel Object' is not assigned.", this);
                return;
            }

            isOptionsPanelOpen = true;
            pauseMenuPanel?.SetActive(false); // Hide main pause menu
            optionsPanelObject.SetActive(true); // Show the options panel

            firstSelectedButtonOptionsMenu?.Select();
        }

        private void HandleReturnFromOptionsClicked()
        {
            if (!isOptionsPanelOpen) return; // Only act if options panel was actually open

            uiSoundFeedback?.PlayClick();
            isOptionsPanelOpen = false;
            
            optionsPanelObject?.SetActive(false); // Hide options panel
            pauseMenuPanel?.SetActive(true); // Show main pause menu again
            
            firstSelectedButtonPauseMenu?.Select(); // Reselect button on main pause menu
        }

        // --- Level Loading Logic ---
        private void RestartLevel()
        {
            Time.timeScale = 1f;
            InputManager.Instance?.EnablePlayerControls(); 
            CheckpointManager.ResetCheckpointData();

            int currentSceneBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            SceneLoader.Instance?.LoadSceneByBuildIndex(currentSceneBuildIndex); 
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            InputManager.Instance?.EnablePlayerControls();
            CheckpointManager.ResetCheckpointData(); 
            SceneLoader.Instance?.LoadMenu();
        }

        public void PlayButtonHighlightSound() => uiSoundFeedback?.PlayHighlight();
        public void PlayButtonSelectSound() => uiSoundFeedback?.PlaySelect();
    }
}
// --- END OF FILE PauseMenuController.cs ---