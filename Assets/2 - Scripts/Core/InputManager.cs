using UnityEngine;
// It's good practice to check if the generated class exists to avoid compilation errors
// if the input asset is renamed or not yet generated.
#if ENABLE_INPUT_SYSTEM
using Scripts.InputSystem; // Assuming PlayerControls is in this namespace
#endif

namespace Scripts.Core
{
    /// <summary>
    /// Centralized singleton that manages the PlayerControls input asset.
    /// Provides a global point of access to input actions and allows switching
    /// between control schemes (e.g., Gameplay vs. UI).
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // The static instance for the singleton pattern.
        public static InputManager Instance { get; private set; }

        #if ENABLE_INPUT_SYSTEM
        public PlayerControls Controls { get; private set; }
        #endif

        private void Awake()
        {
            // Standard singleton implementation
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("InputManager: Another instance already exists. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // The object this component is on should be part of a persistent scene (e.g., 'Program')
            // so DontDestroyOnLoad is appropriate if it's not already handled by the scene's persistence.
            // DontDestroyOnLoad(gameObject); // Uncomment if this manager isn't in a scene that naturally persists.

            #if ENABLE_INPUT_SYSTEM
            // Initialize and enable the controls
            Controls = new PlayerControls();
            #else
            Debug.LogError("InputManager: Unity's Input System package is not enabled or PlayerControls asset not found.", this);
            #endif
        }
        
        private void OnEnable()
        {
            SceneLoader.OnSceneReady += EnablePlayerControlsOnSceneReady;
        }
    
        private void OnDisable()
        {
            SceneLoader.OnSceneReady -= EnablePlayerControlsOnSceneReady;
        }

        private void OnDestroy()
        {
            // Clean up controls when the object is destroyed to prevent memory leaks.
            #if ENABLE_INPUT_SYSTEM
            Controls?.Disable();
            #endif
        }
        
        private void EnablePlayerControlsOnSceneReady()
        {
            // A scene is ready, enable the player's controls.
            // We still call Enable() on the whole asset in case other maps are needed.
            Controls?.Enable();
            EnablePlayerControls();
        }

        #if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Enables gameplay-related input action maps (e.g., "Player") and disables UI maps.
        /// </summary>
        public void EnablePlayerControls()
        {
            if (Controls == null) return;
            Controls.UI.Disable();
            Controls.Player.Enable();
        }

        /// <summary>
        /// Enables UI-related input action maps (e.g., "UI") and disables gameplay maps.
        /// </summary>
        public void EnableUIControls()
        {
            if (Controls == null) return;
            Controls.Player.Disable();
            Controls.UI.Enable();
        }

        /// <summary>
        /// Disables all input action maps. Useful for cutscenes or when game is fully suspended.
        /// </summary>
        public void DisableAllControls()
        {
            Controls?.Disable();
        }
        #else
        // Dummy methods to prevent compile errors if the Input System is disabled.
        public void EnablePlayerControls() {}
        public void EnableUIControls() {}
        public void DisableAllControls() {}
        #endif
    }
}
