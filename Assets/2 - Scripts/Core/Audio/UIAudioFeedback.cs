using UnityEngine;

namespace Scripts.Core.Audio
{
    /// <summary>
    /// A centralized component for playing standardized UI sound effects.
    /// Can be attached to a UI canvas or manager object.
    /// </summary>
    public class UIAudioFeedback : MonoBehaviour
    {
        [Header("Audio Source")]
        [Tooltip("The AudioSource used to play all UI sounds. If null, one will be added to this GameObject.")]
        [SerializeField] private AudioSource audioSource;

        [Header("Sound Definitions")]
        [Tooltip("Sound played on button click.")]
        [SerializeField] private Sounds clickSound;
        [Tooltip("Sound played when a UI element is selected via navigation (keyboard/controller).")]
        [SerializeField] private Sounds selectSound;
        [Tooltip("Sound played when a UI element is highlighted (mouse hover).")]
        [SerializeField] private Sounds highlightSound;
        [Tooltip("Sound played when a menu or panel opens.")]
        [SerializeField] private Sounds openSound;
        [Tooltip("Sound played when a menu or panel closes.")]
        [SerializeField] private Sounds closeSound;

        private void Awake()
        {
            // Ensure we have an AudioSource to work with.
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    // It's good practice to configure the new AudioSource for UI sounds.
                    audioSource.playOnAwake = false;
                    // Assign to a UI-specific AudioMixer group if you have one.
                    // audioSource.outputAudioMixerGroup = ...; 
                }
            }
        }

        public void PlayClick() => clickSound?.Play(audioSource);
        public void PlaySelect() => selectSound?.Play(audioSource);
        public void PlayHighlight() => highlightSound?.Play(audioSource);
        public void PlayOpen() => openSound?.Play(audioSource);
        public void PlayClose() => closeSound?.Play(audioSource);
    }
}

