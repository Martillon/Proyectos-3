using UnityEngine;
using UnityEngine.UI;
using Scripts.Core;
using Scripts.Core.Audio;

namespace Scripts.UI.MainMenu
{
    public class OptionsMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pnl_Video;
        [SerializeField] private GameObject pnl_Audio;

        [Header("Navigation")]
        [SerializeField] private Button btn_Video;
        [SerializeField] private Button btn_Audio;
        [SerializeField] private Button firstVideoButton;
        [SerializeField] private Button firstAudioButton;

        [Header("UI Sounds")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        private enum Tab { Video, Audio }
        private Tab currentTab = Tab.Video;

        private void OnEnable()
        {
            InputManager.Instance.Controls.UI.NextTab.performed += ctx => NavigateToNextTab();
            InputManager.Instance.Controls.UI.PreviousTab.performed += ctx => NavigateToPreviousTab();
        }

        private void OnDisable()
        {
            InputManager.Instance.Controls.UI.NextTab.performed -= ctx => NavigateToNextTab();
            InputManager.Instance.Controls.UI.PreviousTab.performed -= ctx => NavigateToPreviousTab();
        }

        private void Start()
        {
            ShowVideoPanel();

            btn_Video.onClick.AddListener(() =>
            {
                uiSoundFeedback?.PlayClick();
                ShowVideoPanel();
            });

            btn_Audio.onClick.AddListener(() =>
            {
                uiSoundFeedback?.PlayClick();
                ShowAudioPanel();
            });
        }

        private void NavigateToNextTab()
        {
            if (currentTab == Tab.Video)
            {
                uiSoundFeedback?.PlayClick();
                ShowAudioPanel();
            }
        }

        private void NavigateToPreviousTab()
        {
            if (currentTab == Tab.Audio)
            {
                uiSoundFeedback?.PlayClick();
                ShowVideoPanel();
            }
        }

        private void ShowVideoPanel()
        {
            pnl_Video.SetActive(true);
            pnl_Audio.SetActive(false);
            currentTab = Tab.Video;

            if (firstVideoButton != null)
                firstVideoButton.Select();
        }

        private void ShowAudioPanel()
        {
            pnl_Video.SetActive(false);
            pnl_Audio.SetActive(true);
            currentTab = Tab.Audio;

            if (firstAudioButton != null)
                firstAudioButton.Select();
        }
    }
}

