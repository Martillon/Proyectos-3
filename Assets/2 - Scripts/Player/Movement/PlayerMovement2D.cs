using Scripts.Core;
using UnityEngine;

namespace Scripts.Player.Movement
{
    /// <summary>
    /// PlayerMovement2D
    /// 
    /// Handles player movement, jumping, crouching and position locking.
    /// Includes faster gravity for snappier jumping, coyote time for lenient jump windows,
    /// and restrictions on jumping while position is locked or near walls.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float fallMultiplier = 2f;
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Check")]
        [SerializeField] private Transform wallCheck;
        [SerializeField] private float wallCheckRadius = 0.2f;

        [Header("Debug")]
        [SerializeField] private Color groundCheckGizmoColor = Color.red;
        [SerializeField] private Color wallCheckGizmoColor = Color.blue;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private bool isGrounded;
        private bool jumpRequested;
        private bool isCrouching;
        private bool positionLocked;
        private bool isTouchingWall;

        private float coyoteTimeCounter;

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

            // Coyote time countdown
            if (isGrounded)
                coyoteTimeCounter = coyoteTime;
            else
                coyoteTimeCounter -= Time.deltaTime;

            // Check jump input (disallowed if position is locked)
            if (InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame() && coyoteTimeCounter > 0f && !positionLocked)
            {
                jumpRequested = true;
            }

            // Check crouch
            isCrouching = moveInput.y < 0f && isGrounded;

            // Check position lock
            positionLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
        }

        private void FixedUpdate()
        {
            GroundCheck();
            WallCheck();

            // Horizontal movement (no movement if locked)
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
                coyoteTimeCounter = 0f;
            }

            // Apply increased gravity when falling
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Checks if the player is touching the ground.
        /// </summary>
        private void GroundCheck()
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        /// <summary>
        /// Checks for wall contact to prevent illegal climbing.
        /// </summary>
        private void WallCheck()
        {
            isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, groundLayer);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = groundCheckGizmoColor;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
            if (wallCheck != null)
            {
                Gizmos.color = wallCheckGizmoColor;
                Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
            }
        }
    }
}

