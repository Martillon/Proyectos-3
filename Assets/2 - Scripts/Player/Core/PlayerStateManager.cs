// --- START OF FILE PlayerStateManager.cs ---
using UnityEngine;
using System.Collections.Generic; // Required for List
using Scripts.Environment.Interfaces; // Para ITraversablePlatform
using Scripts.Core; // Para GameConstants, si lo usas para el tag

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
        public bool IsOnOneWayPlatform { get; private set; } // True if ANY ground collider is a one-way platform
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
        
        // MODIFICADO: De Collider2D singular a List<Collider2D>
        public List<Collider2D> CurrentGroundPlatformColliders { get; private set; } = new List<Collider2D>();

        // --- Update Methods (Called by specialized components) ---

        public void UpdateMovementInput(float horizontal, float vertical)
        {
            HorizontalInput = horizontal;
            VerticalInput = vertical;

            if (Mathf.Abs(horizontal) > 0.01f) 
            {
                CurrentFacingDirection = Mathf.Sign(horizontal);
            }
        }
        
        public void UpdateIntendsToPressDownState(bool intendsDown)
        {
            IntendsToPressDown = intendsDown;
        }

        public void UpdateJumpInputState(bool jumpPressedDown)
        {
            if (jumpPressedDown)
            {
                JumpInputDown = true;
            }
        }

        public void ConsumeJumpInput()
        {
            JumpInputDown = false;
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

        // MODIFICADO: Firma y lógica para manejar múltiples colliders
        public void UpdateGroundedState(bool isGrounded, List<Collider2D> detectedColliders)
        {
            IsGrounded = isGrounded;
            CurrentGroundPlatformColliders.Clear(); // Limpiar la lista de la detección anterior
            IsOnOneWayPlatform = false; // Resetear antes de verificar

            if (IsGrounded && detectedColliders != null && detectedColliders.Count > 0)
            {
                CurrentGroundPlatformColliders.AddRange(detectedColliders);
                foreach (Collider2D col in CurrentGroundPlatformColliders)
                {
                    
                    // La forma más directa de saber si es "one-way" para el drop es si implementa ITraversablePlatform.
                    // También puedes combinarlo con el tag o PlatformEffector2D si tienes otros tipos de one-way platforms
                    // que no son ITraversablePlatform (por ejemplo, solo permiten saltar a través desde abajo).
                    if (col.GetComponent<ITraversablePlatform>() != null)
                    // Alternativamente, si quieres mantener la lógica anterior con tag y PlatformEffector2D:
                    // if (col.CompareTag(GameConstants.PlatformTag) && col.GetComponent<PlatformEffector2D>() != null)
                    {
                        IsOnOneWayPlatform = true;
                        // No necesitamos 'break' aquí, ya que IsOnOneWayPlatform solo necesita ser true
                        // si *alguna* plataforma lo es, y queremos que CurrentGroundPlatformColliders
                        // contenga todas las plataformas bajo el jugador.
                    }
                }
            }
            else // Si no está grounded o no hay colliders detectados
            {
                // IsGrounded ya está en false, CurrentGroundPlatformColliders ya está limpio,
                // e IsOnOneWayPlatform ya está en false. No se necesita más aquí.
            }
            
            // Debug.Log($"STATEMANAGER: GroundedState Updated. IsGrounded={IsGrounded}, IsOnPlatform={IsOnOneWayPlatform}, CollidersCount={CurrentGroundPlatformColliders.Count}");
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
        
        public bool IsConsideredMovingOnGround => IsGrounded && Mathf.Abs(HorizontalInput) > 0.01f && !IsCrouchingLogic;
    }
}
// --- END OF MODIFIED FILE PlayerStateManager.cs ---