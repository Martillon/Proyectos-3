// --- START OF FILE VideoSettingsPanel.cs ---
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Scripts.Core; // For GameConstants, SettingsManager
using TMPro;
using Scripts.Core.Audio;

namespace Scripts.UI.Options
{
    public class VideoSettingsPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Dropdown drp_Resolution;
        [SerializeField] private TMP_Dropdown drp_DisplayMode;
        [SerializeField] private Toggle tgl_VSync;
        [SerializeField] private Button btn_ApplyVideoSettings;

        [Header("UI Sounds")]
        [SerializeField] private UIAudioFeedback uiInteractionSoundFeedback;

        private List<Resolution> availableFilteredResolutions;
        private bool isInitialized = false;

        private void Start()
        {
            if (SettingsManager.Instance == null) Debug.LogError("VideoSettingsPanel: SettingsManager.Instance is null!", this);

            PopulateResolutionDropdown();
            PopulateDisplayModeDropdown();
            LoadAndApplyInitialVideoSettings();

            btn_ApplyVideoSettings?.onClick.AddListener(OnApplySettingsClicked);
            drp_Resolution?.onValueChanged.AddListener(OnDropdownValueChanged);
            drp_DisplayMode?.onValueChanged.AddListener(OnDropdownValueChanged);
            tgl_VSync?.onValueChanged.AddListener(OnToggleValueChanged);
            
            isInitialized = true;
        }
        
        private void OnDropdownValueChanged(int index)
        {
            if (!isInitialized) return;
            uiInteractionSoundFeedback?.PlayClick();
            // Note: Changes here are only applied when Apply button is clicked.
        }

        private void OnToggleValueChanged(bool value)
        {
            if (!isInitialized) return;
            uiInteractionSoundFeedback?.PlayClick();
            // Note: Changes here are only applied when Apply button is clicked.
        }

        private void PopulateResolutionDropdown()
        {
            // ... (Lógica de PopulateResolutionDropdown sin cambios significativos, ya era buena) ...
            Resolution[] allResolutions = Screen.resolutions;
            availableFilteredResolutions = new List<Resolution>();
            List<string> resolutionOptions = new List<string>();
            float currentAspect = (float)Screen.currentResolution.width / Screen.currentResolution.height;
            const float aspectTolerance = 0.05f;
            const int minWidth = 800;
            const int minHeight = 600;

            foreach (Resolution res in allResolutions
                .Where(r => r.width >= minWidth && r.height >= minHeight)
                .Where(r => Mathf.Abs(((float)r.width / r.height) - currentAspect) <= aspectTolerance)
                .OrderByDescending(r => r.width * r.height)
                .ThenByDescending(r => r.refreshRateRatio.value)
                .GroupBy(r => new { r.width, r.height }) // Group by unique width/height combinations
                .Select(g => g.First())) // Select the first one from each group (which has the highest refresh rate due to prior sorting)
            {
                availableFilteredResolutions.Add(res);
                resolutionOptions.Add($"{res.width} x {res.height}");
            }
            
            drp_Resolution.ClearOptions();
            if (resolutionOptions.Count > 0) drp_Resolution.AddOptions(resolutionOptions);
            else // Fallback
            {
                resolutionOptions.Add($"{Screen.currentResolution.width} x {Screen.currentResolution.height}");
                availableFilteredResolutions.Add(Screen.currentResolution);
                drp_Resolution.AddOptions(resolutionOptions);
            }
        }

        private void PopulateDisplayModeDropdown()
        {
            // ... (Sin cambios) ...
            drp_DisplayMode.ClearOptions();
            List<string> modes = new List<string> { "Fullscreen", "Windowed", "Borderless Fullscreen" };
            drp_DisplayMode.AddOptions(modes);
        }

        private void LoadAndApplyInitialVideoSettings()
        {
            if (SettingsManager.Instance == null) return;

            (int savedWidth, int savedHeight) = SettingsManager.Instance.GetResolution();
            bool savedVSync = SettingsManager.Instance.GetVSync();
            int savedDisplayModeIndex = SettingsManager.Instance.GetDisplayMode();

            if (tgl_VSync != null) tgl_VSync.SetIsOnWithoutNotify(savedVSync);
            if (drp_DisplayMode != null) drp_DisplayMode.SetValueWithoutNotify(savedDisplayModeIndex);
            
            if (drp_Resolution != null && availableFilteredResolutions != null)
            {
                int resolutionIndexToSelect = -1;
                for (int i = 0; i < availableFilteredResolutions.Count; i++)
                {
                    if (availableFilteredResolutions[i].width == savedWidth && availableFilteredResolutions[i].height == savedHeight)
                    {
                        resolutionIndexToSelect = i; break;
                    }
                }
                if (resolutionIndexToSelect != -1) drp_Resolution.SetValueWithoutNotify(resolutionIndexToSelect);
                else if (availableFilteredResolutions.Count > 0) drp_Resolution.SetValueWithoutNotify(0);
            }
            drp_Resolution?.RefreshShownValue();
            drp_DisplayMode?.RefreshShownValue();
        }

        private void OnApplySettingsClicked()
        {
            if (!isInitialized) return;
            uiInteractionSoundFeedback?.PlayClick();
            ApplyAndSaveChanges();
        }

        private void ApplyAndSaveChanges()
        {
             if (SettingsManager.Instance == null || availableFilteredResolutions == null || availableFilteredResolutions.Count == 0) return;

            Resolution selectedResolution = availableFilteredResolutions[drp_Resolution.value];
            bool selectedVSync = tgl_VSync.isOn;
            int selectedDisplayModeIndex = drp_DisplayMode.value;

            FullScreenMode targetFullScreenMode = FullScreenMode.ExclusiveFullScreen;
            if (selectedDisplayModeIndex == 1) targetFullScreenMode = FullScreenMode.Windowed;
            else if (selectedDisplayModeIndex == 2) targetFullScreenMode = FullScreenMode.FullScreenWindow;

            QualitySettings.vSyncCount = selectedVSync ? 1 : 0;
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, targetFullScreenMode);

            SettingsManager.Instance.SetResolution(selectedResolution.width, selectedResolution.height);
            SettingsManager.Instance.SetVSync(selectedVSync);
            SettingsManager.Instance.SetDisplayMode(selectedDisplayModeIndex);
            SettingsManager.Instance.SaveAll(); // Save to PlayerPrefs immediately upon applying video changes
        }
    }
}
// --- END OF FILE VideoSettingsPanel.cs ---