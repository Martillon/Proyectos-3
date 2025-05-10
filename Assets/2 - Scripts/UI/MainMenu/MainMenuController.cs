// --- START OF FILE MainMenuController.cs ---
using UnityEngine;
using UnityEngine.UI;
using Scripts.Core; // For SceneLoader, GameConstants, SettingsManager
using Scripts.Core.Audio;
using Scripts.UI.LevelSelection; // Namespace for LevelSelectorController

namespace Scripts.UI.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main Menu UI Panels")]
        [Tooltip("The main panel containing the primary menu buttons.")]
        [SerializeField] private GameObject mainMenuPanel;
        // pnl_LevelSelection is now managed by LevelSelectorController
        [Tooltip("The panel for game options.")]
        [SerializeField] private GameObject optionsPanel;
        [Tooltip("The panel for game credits.")]
        [SerializeField] private GameObject creditsPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button btn_Play; // This will open the Level Selector
        [SerializeField] private Button btn_Options;
        [SerializeField] private Button btn_Credits;
        [SerializeField] private Button btn_Quit;

        [Header("Submenu Return Buttons")]
        [SerializeField] private Button btn_ReturnFromOptions;
        [SerializeField] private Button btn_ReturnFromCredits;
        // The return button from Level Selector is handled by LevelSelectorController

        [Header("Options Panel Focus")]
        [SerializeField] private Button firstOptionsButtonToSelect;

        [Header("Level Selector")]
        [Tooltip("Reference to the LevelSelectorController component/GameObject.")]
        [SerializeField] private LevelSelectorController levelSelectorController;

        [Header("UI Sounds")]
        [SerializeField] private UIAudioFeedback uiSoundPlayer;

        private void Awake()
        {
            // Validate references
            if (mainMenuPanel == null) Debug.LogError("MainMenuController: MainMenuPanel is not assigned!", this);
            if (optionsPanel == null) Debug.LogError("MainMenuController: OptionsPanel is not assigned!", this);
            if (creditsPanel == null) Debug.LogError("MainMenuController: CreditsPanel is not assigned!", this);
            if (levelSelectorController == null) Debug.LogError("MainMenuController: LevelSelectorController is not assigned!", this);
        }

        private void Start()
        {
            // Ensure LevelSelectorController knows about this MainMenuController
            levelSelectorController?.Initialize(this);

            // Bind button events
            btn_Play?.onClick.AddListener(OnPlayButtonPressed);
            btn_Options?.onClick.AddListener(OnOptionsPressed);
            btn_Credits?.onClick.AddListener(OnCreditsPressed);
            btn_Quit?.onClick.AddListener(OnQuitPressed);

            btn_ReturnFromOptions?.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); ShowThisMenuAgain(); });
            btn_ReturnFromCredits?.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); ShowThisMenuAgain(); });

            ShowThisMenuAgain(); // Start with the main menu panel active
        }

        private void OnPlayButtonPressed()
        {
            uiSoundPlayer?.PlayClick();
            // Debug.Log("MainMenuController: Play button pressed. Opening Level Selector."); // Uncomment for debugging
            mainMenuPanel?.SetActive(false);
            // optionsPanel?.SetActive(false); // Ensure other panels are off
            // creditsPanel?.SetActive(false);
            levelSelectorController?.ShowPanel(); // Tell LevelSelectorController to show itself
        }

        private void OnOptionsPressed()
        {
            uiSoundPlayer?.PlayClick();
            // Debug.Log("MainMenuController: Options button pressed."); // Uncomment for debugging
            mainMenuPanel?.SetActive(false);
            // levelSelectorController?.gameObject.SetActive(false); // Ensure other panels are off
            // creditsPanel?.SetActive(false);
            optionsPanel?.SetActive(true);

            firstOptionsButtonToSelect?.Select();
        }

        private void OnCreditsPressed()
        {
            uiSoundPlayer?.PlayClick();
            // Debug.Log("MainMenuController: Credits button pressed."); // Uncomment for debugging
            mainMenuPanel?.SetActive(false);
            // levelSelectorController?.gameObject.SetActive(false);
            // optionsPanel?.SetActive(false);
            creditsPanel?.SetActive(true);
            btn_ReturnFromCredits?.Select();
        }

        private void OnQuitPressed()
        {
            uiSoundPlayer?.PlayClick();
            // Debug.Log("MainMenuController: Quit button pressed. Exiting application."); // Uncomment for debugging
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Activates the main menu panel and deactivates other primary panels.
        /// Called internally or by other controllers (like LevelSelectorController) when returning.
        /// </summary>
        public void ShowThisMenuAgain()
        {
            // Debug.Log("MainMenuController: Showing main menu panel."); // Uncomment for debugging
            mainMenuPanel?.SetActive(true);
            optionsPanel?.SetActive(false);
            creditsPanel?.SetActive(false);
            // Level Selector panel is managed by LevelSelectorController, it will hide itself.
            // If LevelSelectorController's panel object isn't a child of the controller, explicitly hide:
            // if(levelSelectorController != null) levelSelectorController.gameObject.GetComponentInChildren<CanvasGroup>(true)?.gameObject.SetActive(false); // Example
            
            btn_Play?.Select(); // Select the Play button by default
        }
    }
}
// --- END OF FILE MainMenuController.cs ---

