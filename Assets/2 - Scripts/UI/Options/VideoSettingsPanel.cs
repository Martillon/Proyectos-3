using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Scripts.UI.Options
{
    public class VideoSettingsPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Dropdown drp_Resolution;
        [SerializeField] private Dropdown drp_DisplayMode;
        [SerializeField] private Toggle tgl_VSync;
        [SerializeField] private Button btn_Apply;

        private Resolution[] availableResolutions;
        private int currentResolutionIndex;

        private void Start()
        {
            PopulateResolutionDropdown();
            PopulateDisplayModeDropdown();
            LoadCurrentSettings();

            btn_Apply.onClick.AddListener(ApplyVideoSettings);
        }

        /// <summary>
        /// Loads compatible and clean resolutions into the dropdown.
        /// Filters by current aspect ratio, eliminates duplicates, and ignores very small resolutions.
        /// </summary>
        private void PopulateResolutionDropdown()
        {
            availableResolutions = Screen.resolutions;

            float currentAspect = (float)Screen.currentResolution.width / Screen.currentResolution.height;
            const float aspectTolerance = 0.05f;
            const int minWidth = 800;   // Ignore very low resolutions
            const int minHeight = 600;

            HashSet<string> seen = new HashSet<string>();
            List<string> options = new List<string>();
            List<Resolution> filteredResolutions = new List<Resolution>();

            currentResolutionIndex = 0;

            for (int i = 0; i < availableResolutions.Length; i++)
            {
                Resolution res = availableResolutions[i];

                // Filter by minimum size
                if (res.width < minWidth || res.height < minHeight)
                    continue;

                // Filter by aspect ratio
                float aspect = (float)res.width / res.height;
                if (Mathf.Abs(aspect - currentAspect) > aspectTolerance)
                    continue;

                // Remove near-duplicate resolutions (e.g. 1280x720 vs 1280x719)
                string key = res.width + "x" + res.height;
                if (!seen.Add(key))
                    continue;

                filteredResolutions.Add(res);
                options.Add(key);

                // Mark current resolution index
                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }

            availableResolutions = filteredResolutions.ToArray();

            drp_Resolution.ClearOptions();
            drp_Resolution.AddOptions(options);
            drp_Resolution.value = currentResolutionIndex;
            drp_Resolution.RefreshShownValue();
        }

        /// <summary>
        /// Loads available display modes into the dropdown.
        /// </summary>
        private void PopulateDisplayModeDropdown()
        {
            drp_DisplayMode.ClearOptions();

            List<string> modes = new List<string>
            {
                "Fullscreen",
                "Windowed",
                "Borderless"
            };

            drp_DisplayMode.AddOptions(modes);
        }

        /// <summary>
        /// Loads the current system settings into the UI elements.
        /// </summary>
        private void LoadCurrentSettings()
        {
            tgl_VSync.isOn = QualitySettings.vSyncCount > 0;

            switch (Screen.fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen:
                    drp_DisplayMode.value = 0;
                    break;
                case FullScreenMode.Windowed:
                    drp_DisplayMode.value = 1;
                    break;
                case FullScreenMode.FullScreenWindow:
                    drp_DisplayMode.value = 2;
                    break;
            }

            drp_DisplayMode.RefreshShownValue();
        }

        /// <summary>
        /// Applies the selected video settings to the system.
        /// </summary>
        private void ApplyVideoSettings()
        {
            Resolution selectedResolution = availableResolutions[drp_Resolution.value];

            FullScreenMode selectedMode = FullScreenMode.ExclusiveFullScreen;
            switch (drp_DisplayMode.value)
            {
                case 0: selectedMode = FullScreenMode.ExclusiveFullScreen; break;
                case 1: selectedMode = FullScreenMode.Windowed; break;
                case 2: selectedMode = FullScreenMode.FullScreenWindow; break;
            }

            QualitySettings.vSyncCount = tgl_VSync.isOn ? 1 : 0;

            Screen.SetResolution(selectedResolution.width, selectedResolution.height, selectedMode);

            Debug.Log($"Applied: {selectedResolution.width}x{selectedResolution.height}, Mode: {selectedMode}, VSync: {tgl_VSync.isOn}");
        }
    }
}
