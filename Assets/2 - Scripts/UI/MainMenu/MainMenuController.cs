// --- File: MainMenuController.cs (ULTRA-SIMPLE VERSION) ---

using UnityEngine;
using Scripts.Core;

namespace Scripts.UI.MainMenu
{
    /// <summary>
    /// Acts as the main "Operating System" for the virtual desktop menu.
    /// Manages which "app window" is currently open by activating and deactivating them.
    /// This version uses a simple, instant GameObject switching method.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }
        
        [Header("UI Panels")]
        [Tooltip("A reference to the main menu panel.")]
        [SerializeField] private UIPanelAnimator mainMenuPanel;
        [Tooltip("A reference to the bounties panel.")]
        [SerializeField] private UIPanelAnimator bountiesPanel;
        // Add other panels here as needed, e.g.:
        // [SerializeField] private UIPanelAnimator optionsPanel;

        private UIPanelAnimator _currentlyOpenPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InputManager.Instance?.EnableUIControls();
            
            // --- Initial State Setup ---
            // Deactivate all panels except the main one at the start.
            bountiesPanel?.Hide();
            // optionsPanel?.Hide(); // etc.

            // Show the main menu panel.
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Show();
                _currentlyOpenPanel = mainMenuPanel;
            }
        }
        
        /// <summary>
        /// The new, extremely simple window management method.
        /// Instantly hides the old panel and instantly shows the new one.
        /// </summary>
        /// <param name="panelToOpen">The panel to show.</param>
        public void OpenPanel(UIPanelAnimator panelToOpen)
        {
            // Don't do anything if we're asked to open a null panel or the one that's already open.
            if (panelToOpen == null || _currentlyOpenPanel == panelToOpen) return;
            
            // 1. If a panel is currently open, instantly hide it.
            if (_currentlyOpenPanel != null)
            {
                _currentlyOpenPanel.Hide();
            }

            // 2. Instantly show the new panel.
            panelToOpen.Show();

            // 3. Update the reference to our new "current" panel.
            _currentlyOpenPanel = panelToOpen;
        }

        public void ReturnToMainMenu()
        {
            OpenPanel(mainMenuPanel);
        }
        
        public void QuitApplication()
        {
            Debug.Log("QUIT signal received. Shutting down application.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}

