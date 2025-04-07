using UnityEngine;

namespace Scripts.Core
{
    
    /// <summary>
    /// SettingsManager
    /// 
    /// Centralized manager for handling player settings (audio and video).
    /// Features:
    /// - Stores and retrieves settings using PlayerPrefs.
    /// - Applies settings to the system (AudioMixer, Resolution, Display Mode, VSync).
    /// - Used from any part of the game to save/load settings.
    /// 
    /// This manager is expected to exist in the Program scene and persist during the session.
    /// </summary>
    
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Audio Mixer (Optional)")]
        [SerializeField] private UnityEngine.Audio.AudioMixer audioMixer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Audio

        public void SetVolume(string channel, float value)
        {
            PlayerPrefs.SetFloat($"Volume_{channel}", value);
        }

        public float GetVolume(string channel, float defaultValue = 1f)
        {
            return PlayerPrefs.GetFloat($"Volume_{channel}", defaultValue);
        }

        private void ApplyAudioSettings()
        {
            float master = GetVolume("Master");
            float music = GetVolume("Music");
            float sfx = GetVolume("SFX");

            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(master, 0.0001f)) * 20);
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(music, 0.0001f)) * 20);
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(sfx, 0.0001f)) * 20);
            }

            Debug.Log("Audio settings applied.");
        }

        #endregion

        #region Video

        public void SetResolution(int width, int height)
        {
            PlayerPrefs.SetInt("Resolution_Width", width);
            PlayerPrefs.SetInt("Resolution_Height", height);
        }

        public (int width, int height) GetResolution()
        {
            int width = PlayerPrefs.GetInt("Resolution_Width", Screen.currentResolution.width);
            int height = PlayerPrefs.GetInt("Resolution_Height", Screen.currentResolution.height);
            return (width, height);
        }

        public void SetVSync(bool enabled)
        {
            PlayerPrefs.SetInt("VSync", enabled ? 1 : 0);
        }

        public bool GetVSync() => PlayerPrefs.GetInt("VSync", 1) == 1;

        public void SetDisplayMode(int modeIndex)
        {
            PlayerPrefs.SetInt("DisplayMode", modeIndex);
        }

        public int GetDisplayMode() => PlayerPrefs.GetInt("DisplayMode", 0);

        private void ApplyVideoSettings()
        {
            (int width, int height) = GetResolution();
            bool vsync = GetVSync();
            int modeIndex = GetDisplayMode();

            FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
            if (modeIndex == 1) mode = FullScreenMode.Windowed;
            else if (modeIndex == 2) mode = FullScreenMode.FullScreenWindow;

            QualitySettings.vSyncCount = vsync ? 1 : 0;
            Screen.SetResolution(width, height, mode);

            Debug.Log("Video settings applied.");
        }

        #endregion

        public void SaveAll()
        {
            PlayerPrefs.Save();
            Debug.Log("Settings saved to disk.");
        }

        public void ApplyAllSettings()
        {
            ApplyAudioSettings();
            ApplyVideoSettings();
        }
    }
}
