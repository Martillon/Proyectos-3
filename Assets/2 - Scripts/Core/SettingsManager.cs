// --- START OF FILE SettingsManager.cs ---
using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer

namespace Scripts.Core
{
    /// <summary>
    /// Centralized manager for handling player settings (audio and video).
    /// Uses PlayerPrefs for persistence and applies settings to relevant systems.
    /// Should exist in the Program scene and persist.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Audio Mixer Reference")]
        [Tooltip("The main AudioMixer used for controlling game audio levels.")]
        [SerializeField] private AudioMixer gameAudioMixer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); // Consider adding if this isn't in a scene that persists naturally
            }
            else
            {
                // Debug.LogWarning("SettingsManager: Instance already exists. Destroying duplicate.", this); // Uncomment for debugging
                Destroy(gameObject);
            }
        }

        #region Audio

        /// <summary>
        /// Sets and saves the volume for a specific audio channel.
        /// </summary>
        /// <param name="prefsKey">The PlayerPrefs key for this volume setting (e.g., GameConstants.PrefsMasterVolume).</param>
        /// <param name="linearValue">The volume value (0.0 to 1.0).</param>
        public void SetVolume(string prefsKey, float linearValue)
        {
            PlayerPrefs.SetFloat(prefsKey, Mathf.Clamp01(linearValue));
            // Saving is now handled by the UI panels when changes are made/applied.
            // SaveAll(); // Or call SaveAll() here if you prefer saving on every Set call.
        }

        /// <summary>
        /// Gets the saved volume for a specific audio channel.
        /// </summary>
        /// <param name="prefsKey">The PlayerPrefs key.</param>
        /// <param name="defaultValue">The value to return if the key doesn't exist.</param>
        /// <returns>The saved volume (0.0 to 1.0).</returns>
        public float GetVolume(string prefsKey, float defaultValue = 1f)
        {
            return PlayerPrefs.GetFloat(prefsKey, Mathf.Clamp01(defaultValue));
        }

        /// <summary>
        /// Applies all saved audio settings to the AudioMixer. Usually called on game start.
        /// </summary>
        private void ApplyAudioSettings()
        {
            if (gameAudioMixer == null)
            {
                // Debug.LogError("SettingsManager: Cannot apply audio settings, GameAudioMixer is not assigned.", this); // Uncomment for debugging
                return;
            }

            ApplyVolumeToMixer(GameConstants.MixerMasterVolume, GetVolume(GameConstants.PrefsMasterVolume));
            ApplyVolumeToMixer(GameConstants.MixerMusicVolume, GetVolume(GameConstants.PrefsMusicVolume));
            ApplyVolumeToMixer(GameConstants.MixerSfxVolume, GetVolume(GameConstants.PrefsSfxVolume));

            // Debug.Log("SettingsManager: Audio settings applied from saved data."); // Uncomment for debugging
        }

        /// <summary>
        /// Helper to apply a linear volume value to the mixer as decibels.
        /// </summary>
        private void ApplyVolumeToMixer(string mixerParameter, float linearValue)
        {
            float clampedValue = Mathf.Max(linearValue, 0.0001f); // Avoid Log10(0)
            float dBValue = Mathf.Log10(clampedValue) * 20f;
            gameAudioMixer.SetFloat(mixerParameter, dBValue);
        }

        #endregion

        #region Video

        /// <summary>
        /// Sets and saves the screen resolution.
        /// </summary>
        public void SetResolution(int width, int height)
        {
            PlayerPrefs.SetInt(GameConstants.PrefsResolutionWidth, width);
            PlayerPrefs.SetInt(GameConstants.PrefsResolutionHeight, height);
        }

        /// <summary>
        /// Gets the saved screen resolution.
        /// </summary>
        /// <returns>A tuple containing the saved width and height.</returns>
        public (int width, int height) GetResolution()
        {
            int width = PlayerPrefs.GetInt(GameConstants.PrefsResolutionWidth, Screen.currentResolution.width);
            int height = PlayerPrefs.GetInt(GameConstants.PrefsResolutionHeight, Screen.currentResolution.height);
            return (width, height);
        }

        /// <summary>
        /// Sets and saves the VSync setting.
        /// </summary>
        public void SetVSync(bool enabled)
        {
            PlayerPrefs.SetInt(GameConstants.PrefsVSync, enabled ? 1 : 0);
        }

        /// <summary>
        /// Gets the saved VSync setting.
        /// </summary>
        public bool GetVSync() => PlayerPrefs.GetInt(GameConstants.PrefsVSync, 1) == 1; // Default VSync ON

        /// <summary>
        /// Sets and saves the display mode (Fullscreen, Windowed, Borderless).
        /// </summary>
        /// <param name="modeIndex">0: Fullscreen, 1: Windowed, 2: Borderless</param>
        public void SetDisplayMode(int modeIndex)
        {
            PlayerPrefs.SetInt(GameConstants.PrefsDisplayMode, modeIndex);
        }

        /// <summary>
        /// Gets the saved display mode index.
        /// </summary>
        public int GetDisplayMode() => PlayerPrefs.GetInt(GameConstants.PrefsDisplayMode, 0); // Default Fullscreen

        /// <summary>
        /// Applies all saved video settings to the game. Usually called on game start.
        /// </summary>
        private void ApplyVideoSettings()
        {
            (int width, int height) = GetResolution();
            bool vsync = GetVSync();
            int modeIndex = GetDisplayMode();

            FullScreenMode targetFullScreenMode = FullScreenMode.ExclusiveFullScreen;
            if (modeIndex == 1) targetFullScreenMode = FullScreenMode.Windowed;
            else if (modeIndex == 2) targetFullScreenMode = FullScreenMode.FullScreenWindow;

            QualitySettings.vSyncCount = vsync ? 1 : 0;
            // Check if resolution is actually different before applying to avoid unnecessary screen flicker
            if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != targetFullScreenMode)
            {
                 Screen.SetResolution(width, height, targetFullScreenMode);
                 // Debug.Log($"SettingsManager: Applied video settings - {width}x{height}, Mode: {targetFullScreenMode}, VSync: {vsync}"); // Uncomment for debugging
            }
            // else Debug.Log("SettingsManager: Current video settings match saved settings. No change applied."); // Uncomment for debugging
        }

        #endregion

        /// <summary>
        /// Saves all current PlayerPrefs data to disk.
        /// Call this after making changes you want persisted immediately.
        /// </summary>
        public void SaveAll()
        {
            PlayerPrefs.Save();
            // Debug.Log("SettingsManager: Settings explicitly saved to disk via PlayerPrefs.Save()"); // Uncomment for debugging
        }

        /// <summary>
        /// Loads and applies all saved settings (Audio and Video).
        /// Typically called by ProgramInitializer at the start of the game.
        /// </summary>
        public void ApplyAllSettings()
        {
            // Debug.Log("SettingsManager: Applying all saved settings..."); // Uncomment for debugging
            ApplyAudioSettings();
            ApplyVideoSettings();
            // Debug.Log("SettingsManager: All settings applied."); // Uncomment for debugging
        }
    }
}
// --- END OF FILE SettingsManager.cs ---
