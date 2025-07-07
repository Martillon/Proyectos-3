using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI.MainMenu
{
    [RequireComponent(typeof(Button))]
    public class DesktopIcon : MonoBehaviour
    {
        [Header("App To Launch")]
        [Tooltip("The UIPanelAnimator of the window this icon should open.")]
        [SerializeField] private UIPanelAnimator panelToOpen;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnIconClicked);

            if (panelToOpen == null)
            {
                Debug.LogWarning($"Desktop Icon '{name}' has no panel assigned to open.", this);
            }
        }

        private void OnIconClicked()
        {
            // Call the central MainMenuController to handle the window opening logic.
            MainMenuController.Instance?.OpenPanel(panelToOpen);
        }

        private void OnDestroy()
        {
            // Clean up the listener when the object is destroyed.
            _button.onClick.RemoveListener(OnIconClicked);
        }
    }
}