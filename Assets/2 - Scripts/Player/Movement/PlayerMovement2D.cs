using Scripts.Core;
using UnityEngine;

namespace Scripts.Player.Movement
{
    /// <summary>
    /// PlayerMovement2D
    /// 
    /// Handles 2D player movement, jumping, crouching, gravity control, and wall detection.
    /// Includes coyote time, position lock, and directional input handling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal movement speed.")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Vertical jump impulse.")]
        [SerializeField] private float jumpForce = 10f;

        [Tooltip("Multiplier applied when falling to speed up descent.")]
        [SerializeField] private float fallMultiplier = 2f;

        [Tooltip("Time window after leaving the ground where jump is still allowed.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("Ground Check")]
        [Tooltip("Transform used to check if the player is grounded.")]
        [SerializeField] private Transform groundCheck;

        [Tooltip("Radius of the overlap circle for ground detection.")]
        [SerializeField] private float groundCheckRadius = 0.2f;

        [Tooltip("Layer mask used to detect the ground.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Check")]
        [Tooltip("Transform used to start the wall check raycasts.")]
        [SerializeField] private Transform wallCheckOrigin;

        [Tooltip("Distance for raycasts to detect walls on either side.")]
        [SerializeField] private float wallCheckDistance = 0.1f;

        [Tooltip("Layer mask used to detect walls.")]
        [SerializeField] private LayerMask wallLayer;

        [Header("Debug")]
        [Tooltip("Color of the gizmo drawn for ground check radius.")]
        [SerializeField] private Color groundCheckGizmoColor = Color.red;

        [Tooltip("Color of the gizmo lines for wall check rays.")]
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
            // Read and normalize directional input
            Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            moveInput = new Vector2(Mathf.Sign(rawInput.x), Mathf.Sign(rawInput.y));

            if (Mathf.Abs(rawInput.x) < 0.5f) moveInput.x = 0f;
            if (Mathf.Abs(rawInput.y) < 0.5f) moveInput.y = 0f;

            // Handle coyote time tracking
            if (isGrounded)
                coyoteTimeCounter = coyoteTime;
            else
                coyoteTimeCounter -= Time.deltaTime;

            // Request jump if conditions are met
            if (InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame() && coyoteTimeCounter > 0f && !positionLocked && !isTouchingWall)
            {
                jumpRequested = true;
            }

            // Handle crouching state
            isCrouching = moveInput.y < 0f && isGrounded;

            // Check for position lock input
            positionLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
        }

        private void FixedUpdate()
        {
            GroundCheck();
            WallCheck();

            Vector2 velocity = rb.linearVelocity;

            // Horizontal movement
            if (!positionLocked)
            {
                velocity.x = moveInput.x * moveSpeed;
            }
            else
            {
                velocity.x = 0f;
            }

            // Handle jumping
            if (jumpRequested)
            {
                velocity.y = jumpForce;
                jumpRequested = false;
                coyoteTimeCounter = 0f;
            }

            // Apply additional gravity when falling
            if (velocity.y < 0)
            {
                velocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime);
            }

            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Checks if the player is touching the ground.
        /// </summary>
        private void GroundCheck()
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        /// <summary>
        /// Casts rays to the left and right to detect wall contact.
        /// </summary>
        private void WallCheck()
        {
            Vector2 origin = wallCheckOrigin.position;

            bool leftWall = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer);
            bool rightWall = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);

            isTouchingWall = leftWall || rightWall;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = groundCheckGizmoColor;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            if (wallCheckOrigin != null)
            {
                Gizmos.color = wallCheckGizmoColor;
                Gizmos.DrawLine(wallCheckOrigin.position, wallCheckOrigin.position + Vector3.left * wallCheckDistance);
                Gizmos.DrawLine(wallCheckOrigin.position, wallCheckOrigin.position + Vector3.right * wallCheckDistance);
            }
        }
    }
}