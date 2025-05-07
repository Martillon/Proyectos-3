using UnityEngine;
// using UnityEngine.Serialization; // Not strictly needed if you don't use FormerlySerializedAs

namespace Scripts.Core.Audio
{
    /// <summary>
    /// Provides standardized audio feedback for UI interactions.
    /// Requires an AudioSource component on the same GameObject or assigned.
    /// </summary>
    public class UIAudioFeedback : MonoBehaviour
    {
        [Header("Sound Options")]
        [Tooltip("The AudioSource used to play UI sounds. If null, will try to get one from this GameObject.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Sound played on button click.")]
        [SerializeField] private Sounds clickSound;

        [Tooltip("Sound played when a UI element is selected (e.g., via keyboard/controller navigation).")]
        [SerializeField] private Sounds selectSound;

        [Tooltip("Sound played when a UI element is highlighted (e.g., mouse hover).")]
        [SerializeField] private Sounds highlightSound;

        [Tooltip("Sound played when a menu or UI panel opens.")]
        [SerializeField] private Sounds openSound;

        [Tooltip("Sound played when a menu or UI panel closes.")]
        [SerializeField] private Sounds closeSound;

        private void Awake()
        {
            // Debug.Log("UIAudioFeedback: Awake called.");
            if (audioSource == null)
            {
                // Debug.Log("UIAudioFeedback: AudioSource not assigned, attempting to get it from this GameObject.");
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    // Debug.LogWarning("UIAudioFeedback: No AudioSource found on this GameObject. Adding one.");
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        /// <summary>
        /// Plays the configured click sound.
        /// </summary>
        public void PlayClick()
        {
            // Debug.Log($"UIAudioFeedback: Playing Click Sound (Sound Name: {clickSound?.name ?? "N/A"})");
            clickSound?.Play(audioSource);
        }

        /// <summary>
        /// Plays the configured select sound.
        /// </summary>
        public void PlaySelect()
        {
            // Debug.Log($"UIAudioFeedback: Playing Select Sound (Sound Name: {selectSound?.name ?? "N/A"})");
            selectSound?.Play(audioSource);
        }

        /// <summary>
        /// Plays the configured highlight sound.
        /// </summary>
        public void PlayHighlight()
        {
            // Debug.Log($"UIAudioFeedback: Playing Highlight Sound (Sound Name: {highlightSound?.name ?? "N/A"})");
            highlightSound?.Play(audioSource);
        }

        /// <summary>
        /// Plays the configured open sound.
        /// </summary>
        public void PlayOpen()
        {
            // Debug.Log($"UIAudioFeedback: Playing Open Sound (Sound Name: {openSound?.name ?? "N/A"})");
            openSound?.Play(audioSource);
        }

        /// <summary>
        /// Plays the configured close sound.
        /// </summary>
        public void PlayClose()
        {
            // Debug.Log($"UIAudioFeedback: Playing Close Sound (Sound Name: {closeSound?.name ?? "N/A"})");
            closeSound?.Play(audioSource);
        }
    }
}

