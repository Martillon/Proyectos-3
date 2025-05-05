using UnityEngine;
using UnityEngine.UI;
using Scripts.Core;
using Scripts.Core.Audio;

namespace Scripts.UI.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject pnl_MainMenu;
        [SerializeField] private GameObject pnl_LevelSelection;
        [SerializeField] private GameObject pnl_Options;
        [SerializeField] private GameObject pnl_Credits;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button btn_Play;
        [SerializeField] private Button btn_Options;
        [SerializeField] private Button btn_Credits;
        [SerializeField] private Button btn_Quit;

        [Header("Submenu Return Buttons")]
        [SerializeField] private Button btn_ReturnFromOptions;
        [SerializeField] private Button btn_ReturnFromCredits;

        [Header("Options")]
        [SerializeField] private Button firstOptionsButton;

        [Header("UI Sounds")]
        [SerializeField] private UIAudioFeedback uiSoundPlayer;

        private void Start()
        {
            // Bind button events
            btn_Play.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); OnPlayPressed(); });
            btn_Options.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); OnOptionsPressed(); });
            btn_Credits.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); OnCreditsPressed(); });
            btn_Quit.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); OnQuitPressed(); });
            btn_ReturnFromOptions.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); ShowMainMenu(); });
            btn_ReturnFromCredits.onClick.AddListener(() => { uiSoundPlayer?.PlayClick(); ShowMainMenu(); });

            // Start in main menu
            ShowMainMenu();
        }

        /// <summary>
        /// Called when the Play button is pressed.
        /// Opens the level selection interface.
        /// </summary>
        private void OnPlayPressed()
        {
            Debug.Log("Play pressed. Opening level selection panel.");
            pnl_MainMenu.SetActive(false);
            pnl_LevelSelection.SetActive(true);
        }

        /// <summary>
        /// Called when the Options button is pressed.
        /// Opens the options menu and sets the first button.
        /// </summary>
        private void OnOptionsPressed()
        {
            Debug.Log("Options pressed. Opening options panel.");
            pnl_MainMenu.SetActive(false);
            pnl_Options.SetActive(true);

            if (firstOptionsButton != null)
                firstOptionsButton.Select();
        }

        /// <summary>
        /// Called when the Credits button is pressed.
        /// Opens the credits panel.
        /// </summary>
        private void OnCreditsPressed()
        {
            Debug.Log("Credits pressed. Opening credits panel.");
            pnl_MainMenu.SetActive(false);
            pnl_Credits.SetActive(true);
            btn_ReturnFromCredits.Select();
        }

        /// <summary>
        /// Called when the Quit button is pressed.
        /// Exits the application or stops play mode in editor.
        /// </summary>
        private void OnQuitPressed()
        {
            Debug.Log("Quit pressed. Exiting application.");
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }

        /// <summary>
        /// Shows the main menu and hides all other panels.
        /// </summary>
        private void ShowMainMenu()
        {
            pnl_MainMenu.SetActive(true);
            pnl_LevelSelection.SetActive(false);
            pnl_Options.SetActive(false);
            pnl_Credits.SetActive(false);

            btn_Play.Select();
        }
    }
}

