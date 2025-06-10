using UnityEngine;
using Scripts.Player.Core;

namespace Scripts.Player.Movement.Motor
{
    /// <summary>
    /// Manages the physical movement of the player's Rigidbody2D.
    /// It applies forces for horizontal movement and jumping based on states
    /// from the PlayerStateManager, and enhances fall gravity.
    /// </summary>
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Movement Parameters")]
        [Tooltip("Maximum horizontal movement speed.")]
        [SerializeField] private float moveSpeed = 7f;
        [Tooltip("Force applied vertically for a jump.")]
        [SerializeField] private float jumpForce = 12f;

        [Header("Jump Feel & Control")]
        [Tooltip("Multiplier applied to gravity when falling, for a snappier feel.")]
        [SerializeField] private float fallMultiplier = 2.5f;
        [Tooltip("Time in seconds after leaving a ledge where a jump is still possible.")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("Time in seconds that a jump input is remembered, allowing a jump if the player becomes grounded shortly after pressing the button.")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        private Rigidbody2D _rb;
        private PlayerStateManager _stateManager;

        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;

        private void Awake()
        {
            // The motor requires these components on the root player object.
            _rb = GetComponentInParent<Rigidbody2D>();
            _stateManager = GetComponentInParent<PlayerStateManager>();
            
            if (!_rb) { Debug.LogError("PlayerMotor: Rigidbody2D not found on parent!", this); enabled = false; }
            if (!_stateManager) { Debug.LogError("PlayerMotor: PlayerStateManager not found on parent!", this); enabled = false; }
        }
        
        private void Update()
        {
            // Timers should be handled in Update, which runs on a consistent time step,
            // not FixedUpdate which can have variable time steps.
            
            // Coyote Time: Reset if grounded, otherwise count down.
            if (_stateManager.IsGrounded)
            {
                _coyoteTimeCounter = coyoteTime;
            }
            else
            {
                _coyoteTimeCounter -= Time.deltaTime;
            }
            
            // Jump Buffer: Reset if jump input is pressed, otherwise count down.
            if (_stateManager.JumpInputDown)
            {
                _jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                _jumpBufferCounter -= Time.deltaTime;
            }
        }
        
        private void FixedUpdate()
        {
            if (_rb == null || _stateManager == null) return;

            HandleHorizontalMovement();
            HandleJump();
            ApplyGravityMultiplier();
        }

        private void HandleHorizontalMovement()
        {
            // If movement is locked (crouching, aiming fixed), stop horizontal velocity.
            if (_stateManager.IsMovementLocked)
            {
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
                return;
            }

            // Apply horizontal movement
            float targetVelocityX = _stateManager.HorizontalInput * moveSpeed;
            _rb.linearVelocity = new Vector2(targetVelocityX, _rb.linearVelocity.y);
        }

        private void HandleJump()
        {
            // A jump occurs if the buffer has time AND coyote time is available.
            if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
            {
                // Apply jump force. Use VelocityChange for an instant, predictable jump height.
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0); // Reset vertical velocity for consistent jump height
                _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                
                // Consume the jump buffer and coyote time to prevent multi-jumps.
                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
                
                // Also consume the one-frame input state in the manager.
                _stateManager.ConsumeJumpInput();
            }

            // If the player lets go of the jump button early, cut the jump short.
            // This requires the Jump action to be a "Button" with a "Press and Release" interaction.
            // if (InputManager.Instance.Controls.Player.Jump.wasReleasedThisFrame && _rb.velocity.y > 0)
            // {
            //     _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * 0.5f);
            // }
        }

        private void ApplyGravityMultiplier()
        {
            // Apply extra gravity when falling to make the jump feel less "floaty".
            if (_rb.linearVelocity.y < 0)
            {
                _rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
            }
        }
    }
}