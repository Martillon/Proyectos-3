using Scripts.Core;
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

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_MUSIC = "MusicVolume";
        private const string PARAM_SFX = "SFXVolume";

        private void Start()
        {
            // Set up live listeners to apply volume immediately
            sld_Master.onValueChanged.AddListener((value) => ApplyVolume(PARAM_MASTER, value));
            sld_Music.onValueChanged.AddListener((value) => ApplyVolume(PARAM_MUSIC, value));
            sld_SFX.onValueChanged.AddListener((value) => ApplyVolume(PARAM_SFX, value));

            // Optional: Set default slider values to 1.0f (max)
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
            
            // Save the volume setting
            SettingsManager.Instance.SetVolume(parameter, value);
        }
    }

}
