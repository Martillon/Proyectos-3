using Scripts.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Scripts.Core.Audio;

namespace Scripts.UI.Options
{
    /// <summary>
    /// Handles real-time audio settings via sliders, applies changes to the AudioMixer,
    /// and plays feedback sounds on interaction.
    /// </summary>
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider sld_Master;
        [SerializeField] private Slider sld_Music;
        [SerializeField] private Slider sld_SFX;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback audioFeedback;

        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_MUSIC = "MusicVolume";
        private const string PARAM_SFX = "SFXVolume";

        private void Start()
        {
            sld_Master.onValueChanged.AddListener((value) =>
            {
                ApplyVolume(PARAM_MASTER, value);
                audioFeedback?.PlayClick();
            });

            sld_Music.onValueChanged.AddListener((value) =>
            {
                ApplyVolume(PARAM_MUSIC, value);
                audioFeedback?.PlayClick();
            });

            sld_SFX.onValueChanged.AddListener((value) =>
            {
                ApplyVolume(PARAM_SFX, value);
                audioFeedback?.PlayClick();
            });

            sld_Master.value = 1.0f;
            sld_Music.value = 1.0f;
            sld_SFX.value = 1.0f;
        }

        /// <summary>
        /// Converts a linear volume [0–1] to decibels and applies it to the AudioMixer.
        /// </summary>
        private void ApplyVolume(string parameter, float value)
        {
            float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            audioMixer.SetFloat(parameter, dB);

            SettingsManager.Instance.SetVolume(parameter, value);
        }
    }
}
