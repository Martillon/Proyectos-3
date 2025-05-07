using UnityEngine;

namespace Scripts.Core.Audio
{
    /// <summary>
    /// Represents a configurable sound effect with properties for clip, looping, pitch, and volume.
    /// Designed to be used with an AudioSource for playback.
    /// </summary>
    [System.Serializable]
    public class Sounds
    {
        [Tooltip("Descriptive name for this sound configuration (e.g., 'UI_Button_Click', 'Player_Footstep_Grass'). Not the audio file name.")]
        public string name;

        [Tooltip("The audio clip to be played.")]
        public AudioClip clip;

        [Tooltip("Should this sound loop when played?")]
        public bool loop = false;

        [Tooltip("Enable random pitch variation for this sound. If true, 'Pitch' acts as the base.")]
        public bool randomPitch = false;
        [Range(0.1f, 3f)]
        [Tooltip("Base pitch for the sound. If Random Pitch is enabled, actual pitch will vary around this value.")]
        public float pitch = 1f;

        [Tooltip("Enable random volume variation for this sound. If true, 'Volume' acts as the base.")]
        public bool randomVolume = false;
        [Range(0f, 1f)] // Volume typically ranges from 0 to 1 for AudioSource
        [Tooltip("Base volume for the sound. If Random Volume is enabled, actual volume will vary around this value.")]
        public float volume = 1f;

        /// <summary>
        /// Configures the provided AudioSource with this sound's settings and plays the sound.
        /// </summary>
        /// <param name="source">The AudioSource component that will play this sound.</param>
        public void Play(AudioSource source)
        {
            if (clip == null)
            {
                // Debug.LogWarning($"Sounds ({name}): AudioClip is null. Cannot play sound.");
                return;
            }
            if (source == null)
            {
                // Debug.LogWarning($"Sounds ({name}): AudioSource is null. Cannot play sound via this source.");
                return;
            }

            source.clip = clip;
            source.loop = loop;

            source.pitch = randomPitch
                ? Random.Range(Mathf.Max(0.1f, pitch * 0.90f), Mathf.Min(3f, pitch * 1.10f)) // Ensure pitch stays within reasonable bounds
                : pitch;

            source.volume = randomVolume
                ? Random.Range(Mathf.Max(0f, volume * 0.90f), Mathf.Min(1f, volume * 1.10f)) // Ensure volume stays within 0-1
                : volume;

            // Debug.Log($"Sounds ({name}): Playing on AudioSource '{source.gameObject.name}'. Clip: {clip.name}, Pitch: {source.pitch}, Volume: {source.volume}, Loop: {source.loop}");
            source.Play();
        }
    }
}