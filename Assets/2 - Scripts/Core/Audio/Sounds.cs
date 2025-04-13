using UnityEngine;

namespace Scripts.Core.Audio
{
    /// <summary>
    /// Sounds
    /// 
    /// Serializable class for handling configurable audio playback settings.
    /// Can be reused in any system (weapons, footsteps, UI) with AudioSource support.
    /// </summary>
    [System.Serializable]
    public class Sounds
    {
        [Tooltip("Internal audio name (not the file name)")]
        public string name;

        [Tooltip("The audio clip to play")]
        public AudioClip clip;

        [Tooltip("Whether this sound loops")]
        public bool loop = false;

        [Tooltip("Enable random pitch variation")]
        public bool randomPitch = false;
        [Range(0.3f, 3f)] public float pitch = 1f;

        [Tooltip("Enable random volume variation")]
        public bool randomVolume = false;
        [Range(0.3f, 3f)] public float volume = 1f;
    }
}