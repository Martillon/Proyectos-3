// --- START OF FILE AudioSettingsPanel.cs ---
using Scripts.Core; // For GameConstants, SettingsManager
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Scripts.Core.Audio;

namespace Scripts.UI.Options
{
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider sld_Master;
        [SerializeField] private Slider sld_Music;
        [SerializeField] private Slider sld_SFX;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer gameAudioMixer;

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback uiInteractionSoundFeedback;

        private bool isInitialized = false;

        private void Start()
        {
            if (gameAudioMixer == null) Debug.LogError("AudioSettingsPanel: GameAudioMixer is not assigned!", this);
            if (SettingsManager.Instance == null) Debug.LogError("AudioSettingsPanel: SettingsManager.Instance is null!", this);

            LoadAndApplyInitialAudioSettings();

            sld_Master?.onValueChanged.AddListener(OnMasterVolumeChanged);
            sld_Music?.onValueChanged.AddListener(OnMusicVolumeChanged);
            sld_SFX?.onValueChanged.AddListener(OnSFXVolumeChanged);
            
            isInitialized = true;
        }

        private void LoadAndApplyInitialAudioSettings()
        {
            if (SettingsManager.Instance == null || gameAudioMixer == null) return;

            float masterVol = SettingsManager.Instance.GetVolume(GameConstants.PrefsMasterVolume, 1f);
            float musicVol = SettingsManager.Instance.GetVolume(GameConstants.PrefsMusicVolume, 1f);
            float sfxVol = SettingsManager.Instance.GetVolume(GameConstants.PrefsSfxVolume, 1f);

            if (sld_Master != null) sld_Master.SetValueWithoutNotify(masterVol);
            if (sld_Music != null) sld_Music.SetValueWithoutNotify(musicVol);
            if (sld_SFX != null) sld_SFX.SetValueWithoutNotify(sfxVol);

            ApplyVolumeToMixer(GameConstants.MixerMasterVolume, masterVol);
            ApplyVolumeToMixer(GameConstants.MixerMusicVolume, musicVol);
            ApplyVolumeToMixer(GameConstants.MixerSfxVolume, sfxVol);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (!isInitialized) return;
            ApplyVolumeToMixerAndSave(GameConstants.MixerMasterVolume, GameConstants.PrefsMasterVolume, value);
            uiInteractionSoundFeedback?.PlayClick();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (!isInitialized) return;
            ApplyVolumeToMixerAndSave(GameConstants.MixerMusicVolume, GameConstants.PrefsMusicVolume, value);
            uiInteractionSoundFeedback?.PlayClick();
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (!isInitialized) return;
            ApplyVolumeToMixerAndSave(GameConstants.MixerSfxVolume, GameConstants.PrefsSfxVolume, value);
            uiInteractionSoundFeedback?.PlayClick();
        }

        private void ApplyVolumeToMixerAndSave(string mixerParameter, string prefsKey, float linearValue)
        {
            if (SettingsManager.Instance == null) return;
            ApplyVolumeToMixer(mixerParameter, linearValue);
            SettingsManager.Instance.SetVolume(prefsKey, linearValue);
            SettingsManager.Instance.SaveAll(); // Save immediately when audio slider changes
        }

        private void ApplyVolumeToMixer(string mixerParameter, float linearValue)
        {
            if (gameAudioMixer == null) return;
            float clampedValue = Mathf.Max(linearValue, 0.0001f);
            float dBValue = Mathf.Log10(clampedValue) * 20f;
            gameAudioMixer.SetFloat(mixerParameter, dBValue);
        }
    }
}
// --- END OF FILE AudioSettingsPanel.cs ---