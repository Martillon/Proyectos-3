using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core; // For PlayerStateManager

namespace Scripts.Player.Movement.Motor // Nuevo sub-namespace
{
    /// <summary>
    /// Handles the physical movement of the player (horizontal, jump, gravity)
    /// based on input and state provided by the PlayerStateManager.
    /// Requires a Rigidbody2D on the same GameObject or parent.
    /// </summary>
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Movement Parameters")]
        [Tooltip("Horizontal movement speed.")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("Vertical force applied for a jump.")]
        [SerializeField] private float jumpForce = 10f;
        [Tooltip("Multiplier for gravity when the player is falling, to make falls feel snappier.")]
        [SerializeField] private float fallMultiplier = 2.5f;
        // Coyote time and jump buffering are handled by logic determining JumpInputTriggered in PlayerStateManager
        [Tooltip("Short time window (in seconds) after leaving a platform where the player can still jump.")]
        [SerializeField] private float coyoteTimeDuration = 0.1f;

        private Rigidbody2D _rb;
        private PlayerStateManager _playerStateManager;
        private float _coyoteTimeCounterMotor; 

        void Awake()
        {
            _rb = GetComponentInParent<Rigidbody2D>(); // Rigidbody is on Player_Root
            _playerStateManager = GetComponentInParent<PlayerStateManager>();

            if (_rb == null) Debug.LogError("PlayerMotor: Rigidbody2D not found on parent or self!", this);
            if (_playerStateManager == null) Debug.LogError("PlayerMotor: PlayerStateManager not found on parent or self!", this);
        }

        void FixedUpdate()
        {
            if (_rb == null || _playerStateManager == null) return;

            Vector2 currentVelocity = _rb.linearVelocity; 
            bool isGrounded = _playerStateManager.IsGrounded; // Leer una vez para consistencia en este FixedUpdate

            // Actualizar Coyote Time (si no lo has movido a Update)
            if (isGrounded) { _coyoteTimeCounterMotor = coyoteTimeDuration; }
            else { _coyoteTimeCounterMotor -= Time.fixedDeltaTime; }

            if (_playerStateManager.IsDroppingFromPlatform) // <<< ESTADO CLAVE
            {
                // Cuando está bajando de plataforma, PlayerPlatformHandler debería tener más control
                // sobre la velocidad Y para asegurar que atraviese.
                // PlayerPlatformHandler ya aplica un _rb.velocity.y negativo.
                // Aquí, principalmente evitamos que la lógica normal de salto/gravedad interfiera.
                // También nos aseguramos de que no haya movimiento horizontal.
                //Debug.Log($"MOTOR FixedUpdate: IS DROPPING. Current RB Vel Y before override: {_rb.linearVelocity.y}, Calculated currentVel Y: {currentVelocity.y}");
                currentVelocity.x = 0f;

                // Podrías añadir una velocidad de caída mínima si el nudge no es suficiente
                // o si el Rigidbody pierde velocidad Y por alguna razón.
                if (currentVelocity.y > -0.5f) // Si no está cayendo o cae muy lento
                {
                    currentVelocity.y = -0.5f; // Asegurar una pequeña velocidad de caída constante
                    //Debug.Log($"MOTOR FixedUpdate: IS DROPPING. Forcing currentVelocity.y to -0.5f");

                }
                // NO llamar a HandleJump ni ApplyFallMultiplier aquí
            }
            else // Lógica de movimiento normal
            {
                //Debug.Log("MOTOR FixedUpdate: NORMAL MOVEMENT LOGIC.");
                float horizInput = _playerStateManager.HorizontalInput;
                bool lockHorizontalMovement = _playerStateManager.PositionLockInputActive || (_playerStateManager.IsCrouchingLogic && isGrounded);

                if (!lockHorizontalMovement) { currentVelocity.x = horizInput * moveSpeed; }
                else { currentVelocity.x = 0f; }
                
                HandleJump(ref currentVelocity); // Pasa isGrounded y otros estados relevantes si es necesario
                ApplyFallMultiplier(ref currentVelocity); // Pasa isGrounded
            }

            _rb.linearVelocity = currentVelocity;
            // Debug.Log($"MOTOR FixedUpdate END: Applied Velocity = {_rb.linearVelocity}, IsDropping={_playerStateManager.IsDroppingFromPlatform}");
        }

        private void HandleJump(ref Vector2 currentVelocity /*, bool isCurrentlyGrounded, bool canUseCoyote, etc. */)
        {
            // Usa los estados pasados o los del PlayerStateManager (excepto IsDropping)
            bool canUseCoyote = _coyoteTimeCounterMotor > 0f; // _coyoteTimeCounterMotor se actualiza arriba
            bool baseJumpConditionsMet = !_playerStateManager.PositionLockInputActive &&
                                         !_playerStateManager.IsCrouchingLogic &&
                                         !_playerStateManager.IsTouchingWall;
            // No necesitamos !IsDropping aquí porque ya está manejado en FixedUpdate

            bool canPhysicallyJump = (_playerStateManager.IsGrounded || canUseCoyote) && baseJumpConditionsMet;

            if (_playerStateManager.JumpInputDown && canPhysicallyJump)
            {
                currentVelocity.y = jumpForce;
                _coyoteTimeCounterMotor = 0f; 
                _playerStateManager.ConsumeJumpInput(); 
            }
            else if (_playerStateManager.JumpInputDown && !canPhysicallyJump) 
            {
                _playerStateManager.ConsumeJumpInput(); 
            }
        }

        private void ApplyFallMultiplier(ref Vector2 currentVelocity /*, bool isCurrentlyGrounded */)
        {
            // Si está cayendo (currentVelocity.y ya es < 0 después de la gravedad normal o el salto)
            if (currentVelocity.y < 0) 
            {
                currentVelocity.y += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }
    }
}
// --- END OF FILE PlayerMotor.cs ---
