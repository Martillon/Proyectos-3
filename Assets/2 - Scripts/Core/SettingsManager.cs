using UnityEngine;
using UnityEngine.Audio;

namespace Scripts.Core
{
    /// <summary>
    /// Manages player settings for audio and video.
    /// Persists settings using PlayerPrefs and applies them to the relevant systems.
    /// Should exist in a persistent scene.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Audio Mixer Reference")]
        [Tooltip("The main AudioMixer for controlling game audio levels. Must have exposed 'MasterVolume', 'MusicVolume', and 'SFXVolume' parameters.")]
        [SerializeField] private AudioMixer gameAudioMixer;

        // --- Unity Lifecycle ---

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // This object should be in a persistent scene.
            // DontDestroyOnLoad(gameObject); // Uncomment if not already handled by scene persistence.
        }

        // --- Public API ---

        /// <summary>
        /// Loads all saved settings and applies them.
        /// Typically called by a ProgramInitializer at the very start of the game.
        /// </summary>
        public void ApplyAllSettings()
        {
            // Debug.Log("SettingsManager: Applying all saved settings..."); // For debugging
            ApplyAudioSettings();
            ApplyVideoSettings();
        }

        /// <summary>
        /// Explicitly saves all current PlayerPrefs data to disk.
        /// Call this after making changes that need to be persisted immediately (e.g., from an 'Apply' button in UI).
        /// </summary>
        public void SaveAll()
        {
            PlayerPrefs.Save();
            // Debug.Log("SettingsManager: Settings saved to disk via PlayerPrefs.Save()."); // For debugging
        }

        #region Audio Settings

        /// <summary>
        /// Sets and saves the volume for a specific audio channel.
        /// </summary>
        /// <param name="prefsKey">The PlayerPrefs key for this setting (e.g., GameConstants.PrefsMasterVolume).</param>
        /// <param name="linearValue">The volume value from 0.0 to 1.0.</param>
        public void SetVolume(string prefsKey, float linearValue)
        {
            PlayerPrefs.SetFloat(prefsKey, Mathf.Clamp01(linearValue));
            // Note: Applying to the mixer and saving is now handled by the UI panels.
        }

        /// <summary>
        /// Gets the saved volume for a specific audio channel.
        /// </summary>
        /// <param name="prefsKey">The PlayerPrefs key.</param>
        /// <param name="defaultValue">The value to return if the key doesn't exist.</param>
        /// <returns>The saved volume (0.0 to 1.0).</returns>
        public float GetVolume(string prefsKey, float defaultValue = 1.0f)
        {
            return PlayerPrefs.GetFloat(prefsKey, Mathf.Clamp01(defaultValue));
        }

        #endregion

        #region Video Settings

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
        /// Gets the saved VSync setting. Defaults to true if not set.
        /// </summary>
        public bool GetVSync() => PlayerPrefs.GetInt(GameConstants.PrefsVSync, 1) == 1;

        /// <summary>
        /// Sets and saves the display mode (Fullscreen, Windowed, Borderless).
        /// </summary>
        /// <param name="modeIndex">0: Fullscreen, 1: Windowed, 2: Borderless</param>
        public void SetDisplayMode(int modeIndex)
        {
            PlayerPrefs.SetInt(GameConstants.PrefsDisplayMode, modeIndex);
        }

        /// <summary>
        /// Gets the saved display mode index. Defaults to Fullscreen (0) if not set.
        /// </summary>
        public int GetDisplayMode() => PlayerPrefs.GetInt(GameConstants.PrefsDisplayMode, 0);

        #endregion

        // --- Private Implementation ---

        /// <summary>
        /// Applies all saved audio settings to the AudioMixer.
        /// </summary>
        private void ApplyAudioSettings()
        {
            if (gameAudioMixer == null)
            {
                Debug.LogError("SettingsManager: Cannot apply audio settings; GameAudioMixer is not assigned.", this);
                return;
            }

            ApplyVolumeToMixer(GameConstants.MixerMasterVolume, GetVolume(GameConstants.PrefsMasterVolume));
            ApplyVolumeToMixer(GameConstants.MixerMusicVolume, GetVolume(GameConstants.PrefsMusicVolume));
            ApplyVolumeToMixer(GameConstants.MixerSfxVolume, GetVolume(GameConstants.PrefsSfxVolume));
        }

        /// <summary>
        /// Applies all saved video settings to the game screen.
        /// </summary>
        private void ApplyVideoSettings()
        {
            (int width, int height) = GetResolution();
            bool vsync = GetVSync();
            FullScreenMode targetMode = GetFullScreenModeFromIndex(GetDisplayMode());

            QualitySettings.vSyncCount = vsync ? 1 : 0;
            
            // Only apply resolution if it's different from the current settings to avoid unnecessary screen flicker.
            if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != targetMode)
            {
                Screen.SetResolution(width, height, targetMode);
                // Debug.Log($"SettingsManager: Applied video settings - {width}x{height}, Mode: {targetMode}, VSync: {vsync}"); // For debugging
            }
        }

        /// <summary>
        /// Helper to convert a linear volume value (0-1) to decibels for the AudioMixer.
        /// </summary>
        private void ApplyVolumeToMixer(string mixerParameter, float linearValue)
        {
            // Ensure we don't take Log10 of zero, which is negative infinity.
            float clampedValue = Mathf.Max(linearValue, 0.0001f);
            float dBValue = Mathf.Log10(clampedValue) * 20f;
            gameAudioMixer.SetFloat(mixerParameter, dBValue);
        }

        /// <summary>
        /// Helper to convert the saved display mode index to Unity's FullScreenMode enum.
        /// </summary>
        private FullScreenMode GetFullScreenModeFromIndex(int index)
        {
            switch (index)
            {
                case 1: return FullScreenMode.Windowed;
                case 2: return FullScreenMode.FullScreenWindow; // Borderless
                case 0:
                default:
                    return FullScreenMode.ExclusiveFullScreen;
            }
        }
    }
}