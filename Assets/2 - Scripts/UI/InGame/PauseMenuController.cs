// --- START OF FILE PauseMenuController.cs ---
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Scripts.Core; // For InputManager, SceneLoader, GameConstants
using Scripts.Core.Audio;
using Scripts.Items.Checkpoint;


namespace Scripts.UI.InGame
{
    /// <summary>
    /// Handles pause menu logic: toggling visibility, pausing time, switching input maps,
    /// navigating menu options (including opening an options submenu), and playing UI sounds.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The main panel for the pause menu.")]
        [SerializeField] private GameObject pauseMenuPanel;
        [Tooltip("The button initially selected when the pause menu opens.")]
        [SerializeField] private Button firstSelectedButtonPauseMenu;

        [Header("Options Submenu")]
        [Tooltip("The GameObject containing the options panel (likely the same prefab used by Main Menu).")]
        [SerializeField] private GameObject optionsPanelPrefab; // Or reference if already in scene hierarchy
        [Tooltip("Button in the options panel to return to the pause menu.")]
        [SerializeField] private Button optionsPanelReturnButton; // Needs to be configured in the options panel prefab/instance
        [Tooltip("The button to be selected first when the options panel is opened from the pause menu.")]
        [SerializeField] private Button firstSelectedButtonOptionsMenu; // e.g., the 'Video' tab button

        [Header("Pause Menu Buttons")]
        [SerializeField] private Button btn_Resume;
        [SerializeField] private Button btn_Options; // New button for options
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;

        [Header("Settings")]
        [SerializeField] private bool isPaused = false;
        private bool isOptionsOpen = false; // Track if options submenu is open

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        private GameObject activeOptionsPanelInstance; // To hold the instantiated/activated options panel

        private void Awake()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            // Ensure the options panel reference (if prefab) is initially inactive or handled externally
            if (optionsPanelPrefab != null && optionsPanelPrefab.scene.name == null) // Check if it's a prefab asset
            {
                // Prefab logic is handled on demand
            }
            else if (optionsPanelPrefab != null) // If it's an object in the scene hierarchy
            {
                optionsPanelPrefab.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (InputManager.Instance?.Controls?.Player.PauseMenu != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed += OnPauseInputPerformed;
            }

            // Add listeners for pause menu buttons
            btn_Resume?.onClick.AddListener(HandleResumeClicked);
            btn_Options?.onClick.AddListener(HandleOptionsClicked); // Listener for new options button
            btn_RestartLevel?.onClick.AddListener(HandleRestartLevelClicked);
            btn_MainMenu?.onClick.AddListener(HandleMainMenuClicked);

            // Add listener for the return button *within* the options panel
            optionsPanelReturnButton?.onClick.AddListener(HandleReturnFromOptionsClicked);
        }

        private void OnDisable()
        {
            if (InputManager.Instance?.Controls?.Player.PauseMenu != null)
            {
                InputManager.Instance.Controls.Player.PauseMenu.performed -= OnPauseInputPerformed;
            }

            // Remove listeners
            btn_Resume?.onClick.RemoveListener(HandleResumeClicked);
            btn_Options?.onClick.RemoveListener(HandleOptionsClicked);
            btn_RestartLevel?.onClick.RemoveListener(HandleRestartLevelClicked);
            btn_MainMenu?.onClick.RemoveListener(HandleMainMenuClicked);
            optionsPanelReturnButton?.onClick.RemoveListener(HandleReturnFromOptionsClicked);

            // Clean up instantiated options panel if necessary
            if (activeOptionsPanelInstance != null && optionsPanelPrefab != null && optionsPanelPrefab.scene.name == null)
            {
                 Destroy(activeOptionsPanelInstance);
            }
        }

        private void OnPauseInputPerformed(InputAction.CallbackContext context)
        {
            if (isOptionsOpen) // If options are open, Escape should close options first
            {
                HandleReturnFromOptionsClicked();
            }
            else // Otherwise, toggle the main pause menu
            {
                TogglePauseMenu();
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
            Time.timeScale = 0f; // Pause game time

            InputManager.Instance?.EnableUIControls();
            pauseMenuPanel?.SetActive(true);
            isOptionsOpen = false; // Ensure options are marked as closed
            if(activeOptionsPanelInstance != null) activeOptionsPanelInstance.SetActive(false); // Hide options if returning

            firstSelectedButtonPauseMenu?.Select();
            uiSoundFeedback?.PlayOpen();
        }

        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f; // Resume game time

            InputManager.Instance?.EnablePlayerControls();
            pauseMenuPanel?.SetActive(false);
            isOptionsOpen = false;
            if (activeOptionsPanelInstance != null) activeOptionsPanelInstance.SetActive(false); // Ensure options panel is hidden

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
            isOptionsOpen = true;
            pauseMenuPanel?.SetActive(false); // Hide main pause menu

            if (optionsPanelPrefab != null)
            {
                // If prefab, instantiate; otherwise, just activate
                if (optionsPanelPrefab.scene.name == null) // It's a prefab asset
                {
                     if (activeOptionsPanelInstance == null) // Instantiate only if not already done
                     {
                        activeOptionsPanelInstance = Instantiate(optionsPanelPrefab, transform.parent); // Instantiate under same parent as pause menu
                        // Find the return button on the *instantiated* panel
                        // This requires the return button to have a specific tag or be findable by path/name.
                        // It's often easier if the Options Panel prefab has its own controller script that handles its internal state.
                        // For now, we assume optionsPanelReturnButton was pre-assigned from the prefab asset (less robust).
                     }
                     activeOptionsPanelInstance.SetActive(true);
                }
                else // It's an object already in the scene
                {
                    activeOptionsPanelInstance = optionsPanelPrefab; // Assign reference
                    activeOptionsPanelInstance.SetActive(true);
                }

                // Select the first button in the options menu
                firstSelectedButtonOptionsMenu?.Select();
                 // TODO: Ensure the Return button listener is correctly wired up, especially if instantiating.
                 // It might be better for the Options Panel itself to handle its 'Return' action.
            }
            else
            {
                Debug.LogError("PauseMenuController: OptionsPanelPrefab is not assigned!", this);
            }
        }

        private void HandleReturnFromOptionsClicked()
        {
             if (!isOptionsOpen) return; // Only act if options were open

            uiSoundFeedback?.PlayClick();
            isOptionsOpen = false;
            
            if (activeOptionsPanelInstance != null)
            {
                 activeOptionsPanelInstance.SetActive(false); // Hide options panel
                 // Optional: Destroy instance if it was instantiated from prefab and not needed anymore
                 // if (optionsPanelPrefab.scene.name == null) Destroy(activeOptionsPanelInstance);
            }

            // Show main pause menu again and select its first button
            pauseMenuPanel?.SetActive(true);
            firstSelectedButtonPauseMenu?.Select();
        }


        // --- Level Loading Logic ---

        private void RestartLevel()
        {
            Time.timeScale = 1f;
            InputManager.Instance?.EnablePlayerControls(); 
            CheckpointManager.ResetCheckpointData(); // Crucial for a fresh start of the level's checkpoints

            // Get the build index of the currently active scene
            int currentSceneBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
    
            // Use the new method in SceneLoader
            SceneLoader.Instance?.LoadSceneByBuildIndex(currentSceneBuildIndex); 
            // Debug.Log($"PauseMenuController: Restarting current level (Build Index: {currentSceneBuildIndex})."); // Uncomment for debugging
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            InputManager.Instance?.EnablePlayerControls();
            CheckpointManager.ResetCheckpointData(); // Reset checkpoints before going to menu
            SceneLoader.Instance?.LoadMenu(); // Assumes LoadMenu uses GameConstants.MainMenuSceneName
        }

        // Public methods for EventTriggers on pause menu buttons (Highlight/Select sounds)
        public void PlayButtonHighlightSound() => uiSoundFeedback?.PlayHighlight();
        public void PlayButtonSelectSound() => uiSoundFeedback?.PlaySelect();
    }
}
// --- END OF FILE PauseMenuController.cs ---