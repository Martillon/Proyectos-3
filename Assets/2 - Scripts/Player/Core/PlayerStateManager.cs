// --- START OF FILE PlayerStateManager.cs ---
using UnityEngine;

namespace Scripts.Player.Core // O un namespace Player.State
{
    /// <summary>
    /// Central repository for the player's current state.
    /// Other player components update these states, and others can read from them
    /// to make decisions.
    /// </summary>
    public class PlayerStateManager : MonoBehaviour
    {
        // --- Input States (Updated by PlayerInputReader) ---
        public float HorizontalInput { get; private set; }
        public float VerticalInput { get; private set; } // General vertical input (-1, 0, 1)
        public bool IntendsToPressDown { get; private set; } // Specific for crouch/drop intent (e.g., Y < -0.5)
        public bool JumpInputDown { get; private set; } // True for one frame on jump press
        public bool PositionLockInputActive { get; private set; }
        public bool ShootInputDown { get; private set; } // True for one frame on shoot press
        public bool ShootInputHeld { get; private set; } // True while shoot is held

        // --- Physics/World States (Updated by Detectors/Handlers) ---
        public bool IsGrounded { get; private set; }
        public bool IsOnOneWayPlatform { get; private set; } // Updated by PlayerGroundDetector
        public bool IsTouchingWall { get; private set; }   // Updated by PlayerWallDetector
        public bool IsCrouchingLogic { get; private set; } // True if logical conditions for crouch are met
        public bool IsCrouchVisualApplied { get; private set; } // True if crouch collider/visuals are active
        public bool IsDroppingFromPlatform { get; private set; }
        public float CurrentFacingDirection { get; private set; } = 1f; // 1 for right, -1 for left

        // --- Movement Lock State (Derived/Composite State) ---
        public bool IsMovementLockedByInput => PositionLockInputActive;
        public bool IsMovementLockedByCrouch => IsCrouchingLogic && IsGrounded;
        public bool CanMoveHorizontally => !IsMovementLockedByInput && !IsMovementLockedByCrouch && !IsDroppingFromPlatform;
        
        public Collider2D ActivePlayerCollider { get; private set; } // Actualizado por PlayerCrouchHandler
        public Collider2D CurrentGroundPlatformCollider { get; private set; } // Actualizado por PlayerGroundDetector

        // --- Update Methods (Called by specialized components) ---

        public void UpdateMovementInput(float horizontal, float vertical)
        {
            HorizontalInput = horizontal;
            VerticalInput = vertical;

            // Actualizar FacingDirection si hay input horizontal.
            // PositionLockInputActive NO debe impedir que cambie la dirección a la que MIRA el personaje.
            // PositionLockInputActive impide el MOVIMIENTO físico, no necesariamente el cambio de orientación visual.
            if (Mathf.Abs(horizontal) > 0.01f) 
            {
                CurrentFacingDirection = Mathf.Sign(horizontal);
                // Debug.Log($"STATEMANAGER: Updated FacingDirection to {CurrentFacingDirection}");
            }
            // Si no hay input horizontal, CurrentFacingDirection mantiene su último valor.
        }
        
        public void UpdateIntendsToPressDownState(bool intendsDown)
        {
            IntendsToPressDown = intendsDown;
        }

        public void UpdateJumpInputState(bool jumpPressedDown)
        {
            // Si ya estaba presionado y el nuevo estado es true, no hagas nada (evita múltiples "true" si Update corre antes que el consumo)
            // Si el nuevo estado es true, ponlo a true.
            // Si el nuevo estado es false (desde PlayerInputReader.LateUpdate), permite que se ponga a false SOLO SI YA ERA FALSE.
            // Esto es un poco complicado. Simplifiquemos: PlayerInputReader lo pone a true. PlayerMotor lo pone a false.
            if (jumpPressedDown) // Solo permite ponerlo a true
            {
                JumpInputDown = true;
                // Debug.Log($"STATEMANAGER: JumpInputDown SET TO TRUE by InputReader");
            }
        }

        public void ConsumeJumpInput() // Nuevo método
        {
            JumpInputDown = false;
            // Debug.Log($"STATEMANAGER: JumpInputDown CONSUMED (set to false) by Motor");
        }

        public void UpdatePositionLockState(bool isActive)
        {
            PositionLockInputActive = isActive;
        }
        
        public void UpdateShootInputState(bool shootPressedDown, bool shootHeld)
        {
            ShootInputDown = shootPressedDown;
            ShootInputHeld = shootHeld;
        }

        public void UpdateGroundedState(bool isGrounded, bool isOnPlatform, Collider2D platformColliderIfAny)
        {
            IsGrounded = isGrounded;
            IsOnOneWayPlatform = isOnPlatform;
            CurrentGroundPlatformCollider = isOnPlatform ? platformColliderIfAny : null;
        }

        public void UpdateWallState(bool isTouchingWall)
        {
            IsTouchingWall = isTouchingWall;
        }

        public void UpdateCrouchLogicState(bool isLogicallyCrouching)
        {
            IsCrouchingLogic = isLogicallyCrouching;
        }

        public void UpdateCrouchVisualState(bool visualsApplied)
        {
            IsCrouchVisualApplied = visualsApplied;
        }
        
        public void UpdateDroppingState(bool isDropping)
        {
            IsDroppingFromPlatform = isDropping;
        }

        public void UpdateFacingDirection(float direction) // direction should be 1 or -1
        {
            if (Mathf.Abs(direction) == 1f) // Ensure valid input
            {
                CurrentFacingDirection = direction;
            }
        }
        
        public void UpdateActiveCollider(Collider2D activeCollider)
        {
            ActivePlayerCollider = activeCollider;
        }
        
        

        // Example of how a composite state could be useful for the animator
        public bool IsConsideredMovingOnGround => IsGrounded && Mathf.Abs(HorizontalInput) > 0.01f && !IsCrouchingLogic;


        // TODO: Consider a method to reset all relevant states (e.g., on player death/respawn if needed)
        // public void ResetVolatileStates() { ... JumpInputDown = false; ... }
    }
}
// --- END OF FILE PlayerStateManager.cs ---
