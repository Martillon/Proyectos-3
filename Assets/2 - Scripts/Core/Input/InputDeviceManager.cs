using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Scripts.Core.Input
{
    /// <summary>
    /// A persistent singleton that detects the player's last used input device (Gamepad vs. Keyboard/Mouse)
    /// and provides a global event to notify other systems of changes.
    /// It relies on a PlayerInput component being present in the scene.
    /// </summary>
    public class InputDeviceManager : MonoBehaviour
    {
        public static InputDeviceManager Instance { get; private set; }

        public static event Action<bool> OnDeviceChanged;
        public static bool IsUsingGamepad { get; private set; }

        // We still need a reference to the PlayerInput component.
        private PlayerInput _playerInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Find the PlayerInput component in the scene.
            _playerInput = FindAnyObjectByType<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("InputDeviceManager: A PlayerInput component is required in the scene but was not found.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_playerInput != null)
            {
                // onControlsChanged is the correct event. It fires when the PlayerInput
                // component switches which control scheme it is using.
                _playerInput.onControlsChanged += OnControlsChanged;
            }
        }

        private void OnDisable()
        {
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= OnControlsChanged;
            }
        }

        private void Start()
        {
            // On startup, check the initial control scheme.
            if (_playerInput != null)
            {
                OnControlsChanged(_playerInput);
            }
        }

        /// <summary>
        /// This method is called by the PlayerInput component's event whenever
        /// the active control device changes.
        /// </summary>
        private void OnControlsChanged(PlayerInput input)
        {
            // --- THE SIMPLIFIED LOGIC ---
            // The PlayerInput component has a property that tells us the name of the
            // control scheme it is currently using (e.g., "Gamepad" or "Keyboard&Mouse").
            if (input == null || string.IsNullOrEmpty(input.currentControlScheme)) return;

            string controlSchemeName = input.currentControlScheme;
            
            // We check if the name of the scheme indicates it's a gamepad.
            bool isDeviceGamepad = controlSchemeName.Equals("Gamepad", StringComparison.OrdinalIgnoreCase);

            // If the state has changed from what we last knew...
            if (isDeviceGamepad != IsUsingGamepad)
            {
                // ...update our state and notify everyone.
                Debug.Log($"Input Device Switched To: {(isDeviceGamepad ? "Gamepad" : "Keyboard&Mouse")}");
                IsUsingGamepad = isDeviceGamepad;
                OnDeviceChanged?.Invoke(isDeviceGamepad);
            }
        }
    }
}