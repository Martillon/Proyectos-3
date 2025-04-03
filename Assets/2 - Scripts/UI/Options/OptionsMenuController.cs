using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Scripts.Player.Inputs;


namespace Scripts.UI.Options
{
    public class OptionsMenuController : MonoBehaviour
    {
        [Header("Settings Panels")]
        [SerializeField] private GameObject gp_VideoOptions;
        [SerializeField] private GameObject gp_AudioOptions;

        [Header("Navigation")]
        [SerializeField] private Button firstVideoOptionButton;
        [SerializeField] private Button firstAudioOptionButton;

        private PlayerControls controls;

        private int currentTabIndex = 0;
        private const int totalTabs = 2;

        private void Awake()
        {
            controls = new PlayerControls();
        }

        private void Start()
        {
            // Set initial tab to Video
            currentTabIndex = 0;
            UpdateTab();
        }

        private void OnEnable()
        {
            controls.UI.Enable();
            controls.UI.NextTab.performed += OnNextTab;
            controls.UI.PreviousTab.performed += OnPreviousTab;
        }

        private void OnDisable()
        {
            controls.UI.NextTab.performed -= OnNextTab;
            controls.UI.PreviousTab.performed -= OnPreviousTab;
            controls.UI.Disable();
        }

        /// <summary>
        /// Handles input for moving to the next tab (Right Shoulder).
        /// </summary>
        private void OnNextTab(InputAction.CallbackContext ctx)
        {
            currentTabIndex = (currentTabIndex + 1) % totalTabs;
            UpdateTab();
        }

        /// <summary>
        /// Handles input for moving to the previous tab (Left Shoulder).
        /// </summary>
        private void OnPreviousTab(InputAction.CallbackContext ctx)
        {
            currentTabIndex = (currentTabIndex - 1 + totalTabs) % totalTabs;
            UpdateTab();
        }

        /// <summary>
        /// Updates the currently active tab based on the currentTabIndex value.
        /// </summary>
        private void UpdateTab()
        {
            bool videoActive = currentTabIndex == 0;

            gp_VideoOptions.SetActive(videoActive);
            gp_AudioOptions.SetActive(!videoActive);

            if (videoActive && firstVideoOptionButton != null)
                firstVideoOptionButton.Select();
            else if (!videoActive && firstAudioOptionButton != null)
                firstAudioOptionButton.Select();
        }
    }
}
