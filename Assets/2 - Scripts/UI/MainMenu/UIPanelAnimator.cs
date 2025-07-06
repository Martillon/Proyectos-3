// --- File: UIPanelAnimator.cs (ULTRA-SIMPLE VERSION) ---

using UnityEngine;

namespace Scripts.UI.MainMenu
{
    /// <summary>
    /// A very simple panel controller. Its only job is to activate and deactivate
    /// its own GameObject. All animation and transition logic has been removed.
    /// </summary>
    public class UIPanelAnimator : MonoBehaviour
    {
        /// <summary>
        /// Instantly shows the panel by activating its GameObject.
        /// </summary>
        public void Show()
        {
            // Set the GameObject this script is attached to as active in the scene.
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Instantly hides the panel by deactivating its GameObject.
        /// </summary>
        public void Hide()
        {
            // Set the GameObject this script is attached to as inactive in the scene.
            gameObject.SetActive(false);
        }
    }
}