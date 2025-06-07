using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.UI.LevelSelection;

namespace Scripts.UI.MainMenu
{
    /// <summary>
    /// Manages the main menu screen, handling navigation between its primary panels
    /// (Main, Options, Credits, Level Select).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [Tooltip("The panel containing the primary menu buttons.")]
        [SerializeField] private GameObject mainPanel;
        [Tooltip("The panel for game options.")]
        [SerializeField] private GameObject optionsPanel;
        [Tooltip("The panel for game credits.")]
        [SerializeField] private GameObject creditsPanel;

        [Header("Controllers")]
        [Tooltip("Reference to the LevelSelectorController component.")]
        [SerializeField] private LevelSelectorController levelSelectorController;
        [Tooltip("Reference to the OptionsMenuController component.")]
        [SerializeField] private OptionsMenuController optionsMenuController; // Added reference

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("UI Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundPlayer;
        [Tooltip("Optional text element to display the game version.")]
        [SerializeField] private TMP_Text versionText;

        private void Awake()
        {
            // Validate critical references
            if (!mainPanel) Debug.LogError("MMC: MainPanel is not assigned!", this);
            if (!optionsPanel) Debug.LogError("MMC: OptionsPanel is not assigned!", this);
            if (!creditsPanel) Debug.LogError("MMC: CreditsPanel is not assigned!", this);
            if (!levelSelectorController) Debug.LogError("MMC: LevelSelectorController is not assigned!", this);
            if (!optionsMenuController) Debug.LogError("MMC: OptionsMenuController is not assigned!", this);
        }

        private void Start()
        {
            // Pass a reference of this controller to sub-controllers that need to call back to it.
            levelSelectorController?.Initialize(this);
            optionsMenuController?.Initialize(this); // Options needs it to return

            // Bind button events
            playButton?.onClick.AddListener(OnPlayPressed);
            optionsButton?.onClick.AddListener(OnOptionsPressed);
            creditsButton?.onClick.AddListener(OnCreditsPressed);
            quitButton?.onClick.AddListener(OnQuitPressed);

            // Display version info
            if (versionText)
            {
                versionText.text = Application.isEditor ? "vDEV" : $"v{Application.version}";
            }

            // Start with the main menu visible.
            ShowMainMenu();
            InputManager.Instance?.EnableUIControls();
            ScreenFader.Instance?.FadeToClear();
        }

        public void ShowMainMenu()
        {
            mainPanel?.SetActive(true);
            optionsPanel?.SetActive(false);
            creditsPanel?.SetActive(false);
            levelSelectorController?.gameObject.SetActive(false);
            playButton?.Select();
        }

        private void OnPlayPressed()
        {
            uiSoundPlayer?.PlayClick();
            mainPanel?.SetActive(false);
            levelSelectorController?.ShowPanel();
        }

        private void OnOptionsPressed()
        {
            uiSoundPlayer?.PlayClick();
            mainPanel?.SetActive(false);
            optionsPanel?.SetActive(true);
            optionsMenuController.ShowDefaultPanel(); // Tell options menu to select its first tab
        }

        private void OnCreditsPressed()
        {
            uiSoundPlayer?.PlayClick();
            mainPanel?.SetActive(false);
            creditsPanel?.SetActive(true);
        }

        private void OnQuitPressed()
        {
            uiSoundPlayer?.PlayClick();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}

