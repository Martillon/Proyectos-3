using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace Scripts.UI.Options
{
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider sld_Master;
        [SerializeField] private Slider sld_Music;
        [SerializeField] private Slider sld_SFX;

        [Header("Apply Button")]
        [SerializeField] private Button btn_Apply;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_MUSIC = "MusicVolume";
        private const string PARAM_SFX = "SFXVolume";

        private void Start()
        {
            // Initialize sliders (can be loaded from saved data later)
            sld_Master.value = 1.0f;
            sld_Music.value = 1.0f;
            sld_SFX.value = 1.0f;

            btn_Apply.onClick.AddListener(ApplyAudioSettings);
        }

        /// <summary>
        /// Applies the current slider values to the AudioMixer.
        /// Converts from linear [0–1] to decibels.
        /// </summary>
        private void ApplyAudioSettings()
        {
            ApplyVolume(PARAM_MASTER, sld_Master.value);
            ApplyVolume(PARAM_MUSIC, sld_Music.value);
            ApplyVolume(PARAM_SFX, sld_SFX.value);

            Debug.Log("Audio settings applied.");
        }

        /// <summary>
        /// Converts a linear volume [0–1] to decibel scale and applies it to the mixer.
        /// </summary>
        private void ApplyVolume(string parameter, float value)
        {
            // Convert from linear (0–1) to decibels
            float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            audioMixer.SetFloat(parameter, dB);
        }
    }
}
