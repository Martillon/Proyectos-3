using Scripts.Core;
using UnityEngine;

namespace Scripts.Player.Movement
{
    /// <summary>
    /// PlayerMovement2D
    /// 
    /// Handles player movement, jumping, crouching and position locking.
    /// Movement is grid-like (no analog speed), jump is responsive and aerial control is supported.
    /// Integrates Unity Input System via a centralized InputManager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Debug")]
        [SerializeField] private Color groundCheckGizmoColor = Color.red;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private bool isGrounded;
        private bool jumpRequested;
        private bool isCrouching;
        private bool positionLocked;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            // Read directional input (rounded to -1, 0, or 1)
            Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            moveInput = new Vector2(Mathf.Sign(rawInput.x), Mathf.Sign(rawInput.y));

            // Clamp input to discrete values
            if (Mathf.Abs(rawInput.x) < 0.5f) moveInput.x = 0f;
            if (Mathf.Abs(rawInput.y) < 0.5f) moveInput.y = 0f;

            // Check jump input
            if (InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame() && isGrounded)
            {
                jumpRequested = true;
            }

            // Check crouch (down pressed)
            isCrouching = moveInput.y < 0f && isGrounded;

            // Check position lock
            positionLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
        }

        private void FixedUpdate()
        {
            GroundCheck();

            // Horizontal movement is disabled while position is locked
            if (!positionLocked)
            {
                rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            // Apply jump
            if (jumpRequested)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpRequested = false;
            }
        }

        /// <summary>
        /// Checks if the player is touching the ground.
        /// </summary>
        private void GroundCheck()
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = groundCheckGizmoColor;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}

