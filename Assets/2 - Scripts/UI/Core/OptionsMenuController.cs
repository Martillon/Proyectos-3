using UnityEngine;
using UnityEngine.UI;
using Scripts.Core;
using Scripts.Core.Audio;

namespace Scripts.UI.MainMenu
{
    /// <summary>
    /// Manages the tab navigation within the Options menu (e.g., between Video and Audio settings).
    /// </summary>
    public class OptionsMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject videoPanel;
        [SerializeField] private GameObject audioPanel;

        [Header("Tab Buttons")]
        [SerializeField] private Button videoTabButton;
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button backButton;

        [Header("Default Selections")]
        [SerializeField] private Button firstVideoOption;
        [SerializeField] private Button firstAudioOption;

        [Header("Audio")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;
        
        private MainMenuController _mainMenuController;
        private enum OptionsTab { Video, Audio }
        private OptionsTab _currentTab;

        public void Initialize(MainMenuController mainMenu)
        {
            _mainMenuController = mainMenu;
        }

        private void OnEnable()
        {
            // Subscribe to tab navigation inputs
            if (InputManager.Instance?.Controls.UI != null)
            {
                InputManager.Instance.Controls.UI.NextTab.performed += ctx => NavigateTabs(1);
                InputManager.Instance.Controls.UI.PreviousTab.performed += ctx => NavigateTabs(-1);
            }
            
            videoTabButton?.onClick.AddListener(ShowVideoPanel);
            audioTabButton?.onClick.AddListener(ShowAudioPanel);
            backButton?.onClick.AddListener(OnBackPressed);
        }

        private void OnDisable()
        {
            // Unsubscribe
            if (InputManager.Instance?.Controls?.UI != null)
            {
                InputManager.Instance.Controls.UI.NextTab.performed -= ctx => NavigateTabs(1);
                InputManager.Instance.Controls.UI.PreviousTab.performed -= ctx => NavigateTabs(-1);
            }
        }
        
        public void ShowDefaultPanel()
        {
            ShowVideoPanel();
        }

        private void NavigateTabs(int direction)
        {
            uiSoundFeedback?.PlayClick();
            if (_currentTab == OptionsTab.Video && direction > 0) ShowAudioPanel();
            else if (_currentTab == OptionsTab.Audio && direction < 0) ShowVideoPanel();
        }

        private void ShowVideoPanel()
        {
            _currentTab = OptionsTab.Video;
            videoPanel?.SetActive(true);
            audioPanel?.SetActive(false);
            firstVideoOption?.Select();
        }

        private void ShowAudioPanel()
        {
            _currentTab = OptionsTab.Audio;
            videoPanel?.SetActive(false);
            audioPanel?.SetActive(true);
            firstAudioOption?.Select();
        }
        
        private void OnBackPressed()
        {
            uiSoundFeedback?.PlayClick();
            _mainMenuController?.ShowMainMenu();
        }
    }
}

