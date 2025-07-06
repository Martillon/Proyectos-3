using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI.Core
{
    public class ChangePanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject videoPanel;
        [SerializeField] private GameObject audioPanel;
        
        [Header("Button References")]
        [SerializeField] private Button audioButton;
        [SerializeField] private Button videoButton;

        void Awake()
        {
            if (audioButton != null)
            {
                audioButton.onClick.AddListener(ChangeAudioPanel);
            }
            else
            {
                Debug.LogWarning("Audio Button is not assigned in the inspector.");
            }
            
            if (videoButton != null)
            {
                videoButton.onClick.AddListener(ChangeVideoPanel);
            }
            else
            {
                Debug.LogWarning("Video Button is not assigned in the inspector.");
            }
            
            if(videoPanel == null || audioPanel == null)
            {
                Debug.LogError("One or both panels are not assigned in the inspector.");
            }
            
            ChangeVideoPanel();
        }
        
        private void ChangeAudioPanel()
        {
            videoPanel.SetActive(false);
            audioPanel.SetActive(true);
        }

        private void ChangeVideoPanel()
        {
            audioPanel.SetActive(false);
            videoPanel.SetActive(true);
        }
    }
}