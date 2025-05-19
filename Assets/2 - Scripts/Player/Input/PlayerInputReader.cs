// --- En PlayerInputReader.cs ---
using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core; // For InputManager, PlayerStateManager

namespace Scripts.Player.Input 
{
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerStateManager _playerStateManager;
        private const float INPUT_THRESHOLD = 0.1f; 
        private const float CROUCH_DOWN_THRESHOLD = -0.5f;

        // Buffer para el input de salto
        private bool _jumpPressedThisFrameBuffer;

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();
            if (_playerStateManager == null)
            {
                Debug.LogError("PlayerInputReader: PlayerStateManager not found!", this);
                enabled = false; 
            }
        }

        void Update()
        {
            if (_playerStateManager == null || InputManager.Instance?.Controls == null) return;

            Vector2 rawMoveInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            // Guardar el estado de WasPressedThisFrame en el buffer
            _jumpPressedThisFrameBuffer = InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame(); 
            bool positionLockHeld = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
            bool shootPressedThisFrame = InputManager.Instance.Controls.Player.Shoot.WasPressedThisFrame();
            bool shootHeld = InputManager.Instance.Controls.Player.Shoot.IsPressed();

            bool jumpPressedThisFrame = InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame();
            
            float processedHorizontalInput = Mathf.Abs(rawMoveInput.x) > INPUT_THRESHOLD ? Mathf.Sign(rawMoveInput.x) : 0f;
            float processedVerticalInput = Mathf.Abs(rawMoveInput.y) > INPUT_THRESHOLD ? Mathf.Sign(rawMoveInput.y) : 0f;
            bool intendsToPressDownForCrouch = rawMoveInput.y < CROUCH_DOWN_THRESHOLD;

            _playerStateManager.UpdateMovementInput(processedHorizontalInput, processedVerticalInput);
            _playerStateManager.UpdateIntendsToPressDownState(intendsToPressDownForCrouch);
            // Actualizar StateManager con el valor del buffer
            _playerStateManager.UpdateJumpInputState(_jumpPressedThisFrameBuffer); 
            _playerStateManager.UpdatePositionLockState(positionLockHeld);
            _playerStateManager.UpdateShootInputState(shootPressedThisFrame, shootHeld);

            if (_jumpPressedThisFrameBuffer)
            {
                // Debug.Log($"INPUT_READER Update: JumpPressedThisFrame = TRUE. Updating StateManager."); // Uncomment for debugging
            }
            
            if (jumpPressedThisFrame) // Solo actualiza a true si realmente se presionó
            {
                _playerStateManager.UpdateJumpInputState(true); 
                // Debug.Log($"INPUT_READER Update: JumpPressedThisFrame = TRUE. Updating StateManager.");
            }
        }
        
    }
}
// --- END OF FILE PlayerInputReader.cs ---
