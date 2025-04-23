using UnityEngine;
using Scripts.InputSystem; // Assuming you have a PlayerControls class generated from the Input System

namespace Scripts.Core
{
    /// <summary>
    /// InputManager
    /// 
    /// Centralized singleton that holds the PlayerControls input asset.
    /// Allows switching between gameplay and UI maps.
    /// Provides a global reference to access input actions from any script.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public PlayerControls Controls { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Controls = new PlayerControls();
                Controls.Enable();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Enables gameplay controls and disables UI inputs.
        /// </summary>
        public void EnablePlayerControls()
        {
            Controls.Player.Enable();
            Controls.UI.Disable();
        }

        /// <summary>
        /// Enables UI controls and disables gameplay inputs.
        /// </summary>
        public void EnableUIControls()
        {
            Controls.UI.Enable();
            Controls.Player.Disable();
        }
        
        public void DisableAllControls()
        {
            Controls.Disable();
        }
    }
}
