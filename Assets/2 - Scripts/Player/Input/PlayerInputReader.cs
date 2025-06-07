using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core;
using UnityEngine.InputSystem;

namespace Scripts.Player.Input
{
    /// <summary>
    /// Reads raw input from the InputManager and updates the PlayerStateManager.
    /// This component acts as a bridge between the input hardware and the player's state.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("Threshold for analog stick movement to be considered a directional press.")]
        [SerializeField] private float inputThreshold = 0.25f;
        [Tooltip("Threshold for the vertical axis to be considered an intentional 'down' press for crouching/dropping.")]
        [SerializeField] private float crouchDownThreshold = -0.7f;
        
        private PlayerStateManager _stateManager;

        private void Awake()
        {
            // This component is critical, so we get the reference from the parent (Player_Root).
            _stateManager = GetComponentInParent<PlayerStateManager>();
            if (_stateManager == null)
            {
                Debug.LogError("PlayerInputReader: PlayerStateManager not found on parent! This component cannot function.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_stateManager == null || InputManager.Instance?.Controls == null) return;
            
            // Read values once per frame
            Vector2 rawMoveInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            bool jumpPressed = InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame();
            bool shootPressed = InputManager.Instance.Controls.Player.Shoot.WasPressedThisFrame();
            bool shootHeld = InputManager.Instance.Controls.Player.Shoot.IsPressed();
            bool lockPositionHeld = InputManager.Instance.Controls.Player.PositionLock.IsPressed();

            // Process and update state manager
            float processedHorizontal = Mathf.Abs(rawMoveInput.x) > inputThreshold ? Mathf.Sign(rawMoveInput.x) : 0f;
            _stateManager.SetMovementInput(processedHorizontal, rawMoveInput.y);
            _stateManager.SetIntendsToPressDown(rawMoveInput.y < crouchDownThreshold);
            _stateManager.SetJumpInput(jumpPressed);
            _stateManager.SetShootInput(shootPressed, shootHeld);
            _stateManager.SetPositionLockInput(lockPositionHeld);
        }
    }
}