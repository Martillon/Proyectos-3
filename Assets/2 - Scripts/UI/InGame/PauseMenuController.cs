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

        [Header("Options Submenu")]
        [Tooltip("Reference to the GameObject in the scene that IS the options panel. Should be initially disabled.")]
        [SerializeField] private GameObject optionsPanelObject; 
        [Tooltip("Button IN THE OPTIONS PANEL used to return to the pause menu. Assign from the Options Panel hierarchy.")]
        [SerializeField] private Button optionsPanelReturnButton; 
        [Tooltip("The button to be selected first when the options panel is opened from the pause menu (e.g., 'Video' tab button).")]
        [SerializeField] private Button firstSelectedButtonOptionsMenu;

        [Header("Pause Menu Buttons")]
        [SerializeField] private Button btn_Resume;
        [SerializeField] private Button btn_Options; // Botón para abrir opciones
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;

        [Header("State")]
        [SerializeField] private bool isPaused = false; 
        private bool isOptionsPanelOpen = false; 

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        private void Awake()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (optionsPanelObject != null)
            {
                optionsPanelObject.SetActive(false); 
            }
            else if (btn_Options != null) // Solo es un problema si hay un botón de opciones
            {
                Debug.LogError("PauseMenuController: 'Options Panel Object' is not assigned, but an 'Options' button exists. Options functionality will be broken.", this);
            }

            if (btn_Options != null && optionsPanelObject != null && optionsPanelReturnButton == null)
            {
                // Es un problema si tenemos panel de opciones pero no cómo volver de él.
                Debug.LogWarning("PauseMenuController: 'Options Panel Return Button' is not assigned. Consider assigning it for proper navigation back from options.", this);
            }
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
            btn_Options?.onClick.AddListener(HandleOptionsClicked); // RESTAURADO
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
            btn_Options?.onClick.RemoveListener(HandleOptionsClicked); // RESTAURADO
            btn_RestartLevel?.onClick.RemoveListener(HandleRestartLevelClicked);
            btn_MainMenu?.onClick.RemoveListener(HandleMainMenuClicked);
            optionsPanelReturnButton?.onClick.RemoveListener(HandleReturnFromOptionsClicked);
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
            if (isOptionsPanelOpen)
            {
                HandleReturnFromOptionsClicked();
            }
            else
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
            isPaused = true; Time.timeScale = 0f; 
            InputManager.Instance?.EnableUIControls(); 
            isOptionsPanelOpen = false; 
            optionsPanelObject?.SetActive(false); 
            pauseMenuPanel?.SetActive(true); 
            firstSelectedButtonPauseMenu?.Select(); 
            uiSoundFeedback?.PlayOpen(); 
        }

        private void ResumeGame() 
        { 
            isPaused = false; Time.timeScale = 1f; 
            InputManager.Instance?.EnablePlayerControls(); 
            isOptionsPanelOpen = false; 
            optionsPanelObject?.SetActive(false); 
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
        private void HandleOptionsClicked() // ESTA FUNCIÓN FALTABA O ESTABA MAL
        {
            uiSoundFeedback?.PlayClick();
            OpenOptionsPanel();
        }

        private void OpenOptionsPanel() // ESTA FUNCIÓN FALTABA O ESTABA MAL
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
            // Debug.Log("PauseMenuController: Options panel opened.", this); // Uncomment for debugging
        }

        private void HandleReturnFromOptionsClicked() 
        { 
            if (!isOptionsPanelOpen) return; 
            uiSoundFeedback?.PlayClick(); 
            isOptionsPanelOpen = false; 
            optionsPanelObject?.SetActive(false); 
            pauseMenuPanel?.SetActive(true); 
            firstSelectedButtonPauseMenu?.Select(); 
            // Debug.Log("PauseMenuController: Returned from options to pause menu.", this); // Uncomment for debugging
        }

        // --- Level Loading Logic ---
        private void RestartLevel() 
        { 
            Time.timeScale = 1f; 
            InputManager.Instance?.EnablePlayerControls(); 
            CheckpointManager.ResetCheckpointData(); 
            SceneLoader.Instance?.LoadSceneByBuildIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex); 
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