using UnityEngine;

namespace Scripts.Core.Audio
{
    /// <summary>
    /// A serializable class that holds configuration for a single sound effect.
    /// It can be used in arrays to define a collection of sounds for an action.
    /// </summary>
    [System.Serializable]
    public class Sounds
    {
        [Tooltip("A descriptive name for the sound (e.g., 'UI_Click', 'Weapon_Shotgun_Fire'). Not used in code, just for organization.")]
        public string name;

        [Tooltip("The audio clip to be played.")]
        public AudioClip clip;

        [Tooltip("If true, the sound will loop when played.")]
        public bool loop = false;

        [Header("Variation")]
        [Tooltip("If true, pitch will be slightly randomized around the base pitch value.")]
        public bool randomPitch = false;
        [Range(0.1f, 3f)]
        [Tooltip("The base pitch of the sound.")]
        public float pitch = 1f;
        [Tooltip("The range of pitch variation (e.g., 0.1 means pitch will be between 0.9 and 1.1).")]
        [SerializeField] private float pitchVariation = 0.1f;

        [Tooltip("If true, volume will be slightly randomized around the base volume value.")]
        public bool randomVolume = false;
        [Range(0f, 1f)]
        [Tooltip("The base volume of the sound.")]
        public float volume = 1f;
        [Tooltip("The range of volume variation (e.g., 0.1 means volume will be between 0.9 and 1.1).")]
        [SerializeField] private float volumeVariation = 0.1f;

        /// <summary>
        /// Configures and plays this sound on the provided AudioSource.
        /// </summary>
        /// <param name="source">The AudioSource that will play the sound.</param>
        public void Play(AudioSource source)
        {
            if (clip == null || source == null)
            {
                // Debug.LogWarning($"Sounds ({name}): Cannot play sound. AudioClip or AudioSource is null."); // For debugging
                return;
            }

            source.clip = clip;
            source.loop = loop;

            source.pitch = randomPitch
                ? Random.Range(pitch - pitchVariation, pitch + pitchVariation)
                : pitch;

            source.volume = randomVolume
                ? Random.Range(volume - volumeVariation, volume + volumeVariation)
                : volume;

            source.Play();
        }
    }
}