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
            
            // Leer estados actualizados del PlayerStateManager
            bool isPosLocked = _playerStateManager.PositionLockInputActive;
            bool isCrouchLogic = _playerStateManager.IsCrouchingLogic;
            bool isGrounded = _playerStateManager.IsGrounded;
            bool isDropping = _playerStateManager.IsDroppingFromPlatform;
            float horizInput = _playerStateManager.HorizontalInput;

            // Determinar si el movimiento horizontal debe bloquearse
            bool lockHorizontalMovement = isPosLocked || (isCrouchLogic && isGrounded);
            // Debug.Log($"MOTOR FixedUpdate: lockHorizontal={lockHorizontalMovement} (isPosLocked={isPosLocked}, isCrouchLogic={isCrouchLogic}, isGrounded={isGrounded}) HorizInput={horizInput}");

            // --- INICIO DE CÁLCULO DE VELOCIDAD ---
            Vector2 currentVelocity = _rb.linearVelocity; // LEER la velocidad actual del Rigidbody

            // 1. Aplicar Movimiento Horizontal
            if (!lockHorizontalMovement && !isDropping)
            {
                currentVelocity.x = horizInput * moveSpeed;
            }
            else
            {
                currentVelocity.x = 0f;
                // if(lockHorizontalMovement) Debug.Log("MOTOR FixedUpdate: Horizontal Movement LOCKED TO 0");
            }
            
            // 2. Actualizar Coyote Time (debe hacerse antes de HandleJump si HandleJump lo usa)
            if (isGrounded) // Usa el 'isGrounded' leído del StateManager para este frame de FixedUpdate
            {
                _coyoteTimeCounterMotor = coyoteTimeDuration;
            }
            else
            {
                _coyoteTimeCounterMotor -= Time.fixedDeltaTime;
            }

            // 3. Procesar Salto (esto modificará currentVelocity.y si se salta)
            //    HandleJump AHORA modificará 'currentVelocity' directamente en lugar de _rb.linearVelocity
            HandleJump(ref currentVelocity); // <<< CAMBIO: Pasar currentVelocity por referencia

            // 4. Aplicar Multiplicador de Caída (esto modifica currentVelocity.y si se está cayendo)
            ApplyFallMultiplier(ref currentVelocity); // <<< CAMBIO: Pasar currentVelocity por referencia
            
            // --- FIN DE CÁLCULO DE VELOCIDAD ---

            // Aplicar la velocidad calculada FINAL al Rigidbody UNA SOLA VEZ
            _rb.linearVelocity = currentVelocity;
            // Debug.Log($"MOTOR FixedUpdate END: Applied Velocity = {_rb.linearVelocity}, currentVelocityCalculated = {currentVelocity}");
        }

        private void HandleHorizontalMovement()
        {
            float targetHorizontalVelocity = 0f;

            if (_playerStateManager.CanMoveHorizontally) // Usa el estado compuesto del StateManager
            {
                targetHorizontalVelocity = _playerStateManager.HorizontalInput * moveSpeed;
            }
            // else: targetHorizontalVelocity remains 0, so player stops.

            // Aplicar la velocidad horizontal
            _rb.linearVelocity = new Vector2(targetHorizontalVelocity, _rb.linearVelocity.y);
        }

        private void HandleJump(ref Vector2 currentVelocity) // <<< CAMBIO: Acepta y modifica currentVelocity
        {
            if (_playerStateManager == null) return;

            bool canUseCoyote = _coyoteTimeCounterMotor > 0f;
            bool baseJumpConditionsMet = !_playerStateManager.PositionLockInputActive &&
                                         !_playerStateManager.IsCrouchingLogic &&
                                         !_playerStateManager.IsTouchingWall &&
                                         !_playerStateManager.IsDroppingFromPlatform;
            bool canPhysicallyJump = (_playerStateManager.IsGrounded || canUseCoyote) && baseJumpConditionsMet;
            
            // Debug.Log($"MOTOR HandleJump: JumpInputDown(SM)={_playerStateManager.JumpInputDown}, CanPhysicallyJump={canPhysicallyJump} ...");

            if (_playerStateManager.JumpInputDown && canPhysicallyJump)
            {
                currentVelocity.y = jumpForce; // <<< CAMBIO: Modifica currentVelocity
                _coyoteTimeCounterMotor = 0f; 
                _playerStateManager.ConsumeJumpInput(); 
                // Debug.Log("MOTOR: JUMP APPLIED! JumpInput Consumed.", this);
            }
            else if (_playerStateManager.JumpInputDown && !canPhysicallyJump) 
            {
                // Debug.LogWarning($"MOTOR: Jump input IGNORED. Conditions not met. Consuming input anyway.", this);
                _playerStateManager.ConsumeJumpInput(); 
            }
        }

        private void ApplyFallMultiplier(ref Vector2 currentVelocity) // <<< CAMBIO: Acepta y modifica currentVelocity
        {
            // Solo aplicar si la velocidad Y (después de un posible salto) es negativa
            if (currentVelocity.y < 0) // NO uses _rb.linearVelocity.y aquí, porque podría no reflejar el salto de este frame
            {
                // No necesitamos la condición !_playerStateManager.JumpInputDown si estamos mirando currentVelocity.y
                // porque si acabamos de saltar, currentVelocity.y será positivo.
                currentVelocity.y += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }
    }
}
// --- END OF FILE PlayerMotor.cs ---
