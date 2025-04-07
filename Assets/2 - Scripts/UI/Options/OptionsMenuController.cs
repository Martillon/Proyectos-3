using UnityEngine;
using UnityEngine.UI;


namespace Scripts.UI.Options
{
    /// <summary>
    /// OptionsMenuController
    /// 
    /// Manages the visibility and navigation between video and audio settings panels.
    /// Allows switching via UI buttons (no input system shoulder navigation).
    /// </summary>
    public class OptionsMenuController : MonoBehaviour
    {
        [Header("Option Panels")]
        [SerializeField] private GameObject gp_01_VideoOptions;
        [SerializeField] private GameObject gp_02_AudioOptions;

        [Header("Tab Buttons")]
        [SerializeField] private Button bt_video;
        [SerializeField] private Button bt_audio;

        [Header("First Selectable Elements")]
        [SerializeField] private Button firstVideoOptionButton;
        [SerializeField] private Button firstAudioOptionButton;

        private int currentTabIndex = 0; // 0 = video, 1 = audio

        private void Start()
        {
            bt_video.onClick.AddListener(() => SwitchToTab(0));
            bt_audio.onClick.AddListener(() => SwitchToTab(1));

            SwitchToTab(0); // Default to video tab
        }

        /// <summary>
        /// Switches to the specified tab index.
        /// 0 = Video, 1 = Audio.
        /// </summary>
        private void SwitchToTab(int index)
        {
            currentTabIndex = index;
            UpdateTab();
        }

        /// <summary>
        /// Activates the correct panel and selects the first element.
        /// </summary>
        private void UpdateTab()
        {
            bool videoActive = currentTabIndex == 0;

            gp_01_VideoOptions.SetActive(videoActive);
            gp_02_AudioOptions.SetActive(!videoActive);

            if (videoActive && firstVideoOptionButton != null)
                firstVideoOptionButton.Select();
            else if (!videoActive && firstAudioOptionButton != null)
                firstAudioOptionButton.Select();
        }
    }
}
