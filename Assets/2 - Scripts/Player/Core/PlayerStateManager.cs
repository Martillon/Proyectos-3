using UnityEngine;
using System.Collections.Generic;
using Scripts.Environment.Interfaces;

namespace Scripts.Player.Core
{
    /// <summary>
    /// Central hub for the player's current state. Other player components update
    /// these states, and others can read them to make decisions. Avoids tight coupling
    /// between player systems.
    /// </summary>
    public class PlayerStateManager : MonoBehaviour
    {
        // --- Input States (updated by PlayerInputReader) ---
        public float HorizontalInput { get; private set; }
        public float VerticalInput { get; private set; }
        public bool JumpInputDown { get; private set; }
        public bool ShootInputDown { get; private set; }
        public bool ShootInputHeld { get; private set; }
        public bool PositionLockInputActive { get; private set; }
        public bool IntendsToPressDown { get; private set; } // Specific intent for crouch/drop

        // --- World Interaction States (updated by Detectors/Handlers) ---
        public bool IsGrounded { get; private set; }
        public bool IsTouchingWall { get; private set; }
        public bool IsOnOneWayPlatform { get; private set; }
        public bool IsDroppingFromPlatform { get; private set; }

        // --- Ability/Logic States (updated by Handlers) ---
        public bool IsCrouching { get; private set; }
        public bool CanStandUp { get; private set; } = true; // NEW: State for being unable to stand

        // --- Derived States & Properties ---
        public float FacingDirection { get; private set; } = 1f; // 1 for right, -1 for left
        public bool IsMovementLocked => PositionLockInputActive || (IsCrouching && IsGrounded);
        public bool IsConsideredMovingOnGround => IsGrounded && Mathf.Abs(HorizontalInput) > 0.01f && !IsCrouching && !PositionLockInputActive;
        public Collider2D ActivePlayerCollider { get; private set; }
        public List<Collider2D> CurrentGroundColliders { get; } = new List<Collider2D>();


        // --- Update Methods (called by other player components) ---

        public void SetMovementInput(float horizontal, float vertical)
        {
            HorizontalInput = horizontal;
            VerticalInput = vertical;

            // Update facing direction based on non-zero horizontal input
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                FacingDirection = Mathf.Sign(horizontal);
            }
        }
        
        public void SetIntendsToPressDown(bool intendsDown) => IntendsToPressDown = intendsDown;
        public void SetJumpInput(bool isPressed) { if (isPressed) JumpInputDown = true; }
        public void ConsumeJumpInput() => JumpInputDown = false;
        public void SetPositionLockInput(bool isActive) => PositionLockInputActive = isActive;
        public void SetShootInput(bool isPressed, bool isHeld) { ShootInputDown = isPressed; ShootInputHeld = isHeld; }
        public void SetWallState(bool isTouching) => IsTouchingWall = isTouching;
        public void SetCrouchState(bool isCrouching, bool canStand) { IsCrouching = isCrouching; CanStandUp = canStand; }
        public void SetDroppingState(bool isDropping) => IsDroppingFromPlatform = isDropping;
        public void SetActiveCollider(Collider2D activeCollider) => ActivePlayerCollider = activeCollider;
        
        public void SetGroundedState(bool isGrounded, List<Collider2D> detectedColliders)
        {
            IsGrounded = isGrounded;
            CurrentGroundColliders.Clear();
            IsOnOneWayPlatform = false;

            if (!IsGrounded || detectedColliders == null) return;

            CurrentGroundColliders.AddRange(detectedColliders);
            foreach (var col in CurrentGroundColliders)
            {
                // A platform is considered "one-way" for dropping if it's traversable.
                // You could also check for PlatformEffector2D or tags if you have other types.
                if (col.GetComponent<ITraversablePlatform>() != null)
                {
                    IsOnOneWayPlatform = true;
                    break; // We only need to find one to set the state.
                }
            }
        }
    }
}