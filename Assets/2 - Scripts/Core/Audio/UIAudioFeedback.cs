using UnityEngine;
using UnityEngine.Serialization;

namespace Scripts.Core.Audio
{
    public class UIAudioFeedback : MonoBehaviour
    {
        [Header("Sound Options")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Sounds clickSound;
        [SerializeField] private Sounds selectSound;
        [SerializeField] private Sounds highlightSound;

        public void PlayClick() => clickSound?.Play(audioSource);
        public void PlaySelect() => selectSound?.Play(audioSource);
        public void PlayHighlight() => highlightSound?.Play(audioSource);
    }
}

