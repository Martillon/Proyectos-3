using UnityEngine;

namespace Scripts.Core.Audio
{
    public class UIAudioFeedback : MonoBehaviour
    {
        [Header("Sound Options")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Sounds clickSound;
        [SerializeField] private Sounds selectSound;
        [SerializeField] private Sounds disabledSound;

        public void PlayClick() => clickSound?.Play(audioSource);
        public void PlaySelect() => selectSound?.Play(audioSource);
    }
}

