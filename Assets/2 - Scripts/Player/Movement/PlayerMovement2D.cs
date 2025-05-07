using Scripts.Core;
using System.Collections;
using UnityEngine;

namespace Scripts.Player.Movement
{
    /// <summary>
    /// PlayerMovement2D
    /// Handles 2D player movement, jumping, crouching, gravity control, wall detection,
    /// coyote time, position lock, directional input handling, and platform drop-through.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))] // Player should always have a collider
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal movement speed.")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("Vertical jump impulse.")]
        [SerializeField] private float jumpForce = 10f;
        [Tooltip("Multiplier applied when falling to speed up descent.")]
        [SerializeField] private float fallMultiplier = 2f;
        [Tooltip("Time window after leaving the ground where jump is still allowed (Coyote Time).")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("Ground Check")]
        [Tooltip("Transform used as the origin for the ground check.")]
        [SerializeField] private Transform groundCheckOrigin;
        [Tooltip("Radius of the overlap circle for ground detection.")]
        [SerializeField] private float groundCheckRadius = 0.2f;
        [Tooltip("Layer mask used to detect what is considered ground.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Check")]
        [Tooltip("Transform used as the origin for wall check raycasts.")]
        [SerializeField] private Transform wallCheckOrigin;
        [Tooltip("Distance for raycasts to detect walls on either side.")]
        [SerializeField] private float wallCheckDistance = 0.1f;
        [Tooltip("Layer mask used to detect what is considered a wall.")]
        [SerializeField] private LayerMask wallLayer;

        [Header("Platform Drop Settings")]
        [Tooltip("Time (in seconds) the player's collider is disabled when dropping through a platform.")]
        [SerializeField] private float dropThroughTime = 0.2f;
        [Tooltip("Tag used to identify one-way platforms.")]
        [SerializeField] private string oneWayPlatformTag = "Platform";


        [Header("Crouch Settings")]
        [Tooltip("Multiplier for the collider's height when crouching (e.g., 0.5 for half height).")]
        [SerializeField] private float crouchHeightMultiplier = 0.5f;
        [Tooltip("Reference to the fire point transform, which might need to be adjusted when crouching.")]
        [SerializeField] private Transform firePoint; // Optional, for adjusting weapon position

        [Header("Debug Gizmos")]
        [SerializeField] private Color groundCheckGizmoColor = Color.red;
        [SerializeField] private Color wallCheckGizmoColor = Color.blue;

        // Component references
        private Rigidbody2D rb;
        private Collider2D playerCollider; // Main collider of the player

        // State variables
        private Vector2 moveInput;          // Processed movement input for physics
        private bool isGrounded;
        private bool jumpRequested;
        private bool isCrouching;           // True if the player is currently in a crouch state
        private bool isCrouchStateApplied;  // True if crouch collider/visual changes are active
        private bool positionLocked;        // True if player is holding the position lock input
        private bool isTouchingWall;
        private bool isDropping = false;    // True if currently dropping through a platform

        private float coyoteTimeCounter;

        // Original collider and fire point values for crouching
        private Vector2 originalColliderSize;
        private Vector2 originalColliderOffset;
        private Vector3 originalFirePointLocalPosition;
        private CapsuleCollider2D capsuleCollider; // Specific reference if player uses a CapsuleCollider2D

        // Public properties for other systems (e.g., AimDirectionResolver)
        public bool IsGrounded => isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsDropping => isDropping;
        public bool IsTouchingWall => isTouchingWall; // Expose if needed by other systems

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerCollider = GetComponent<Collider2D>();
            capsuleCollider = GetComponent<CapsuleCollider2D>(); // Attempt to get CapsuleCollider2D

            if (rb == null) Debug.LogError("PlayerMovement2D: Rigidbody2D not found!");
            if (playerCollider == null) Debug.LogError("PlayerMovement2D: Collider2D not found!");
            if (groundCheckOrigin == null) Debug.LogError("PlayerMovement2D: GroundCheckOrigin not assigned!");
            if (wallCheckOrigin == null) Debug.LogError("PlayerMovement2D: WallCheckOrigin not assigned!");


            if (capsuleCollider != null)
            {
                originalColliderSize = capsuleCollider.size;
                originalColliderOffset = capsuleCollider.offset;
            }
            else if (playerCollider is BoxCollider2D boxCollider) // Fallback if it's a BoxCollider2D
            {
                originalColliderSize = boxCollider.size;
                originalColliderOffset = boxCollider.offset;
            }
            else
            {
                // For other collider types, bounds might be less direct for crouch adjustments
                // Debug.LogWarning("PlayerMovement2D: Player collider is not a CapsuleCollider2D or BoxCollider2D. Crouching might not adjust collider size correctly.");
                originalColliderSize = playerCollider.bounds.size; // Less precise for some colliders
                originalColliderOffset = playerCollider.offset;
            }


            if (firePoint != null)
            {
                originalFirePointLocalPosition = firePoint.localPosition;
            }
        }

        private void Update()
        {
            if (InputManager.Instance == null || InputManager.Instance.Controls == null)
            {
                // Debug.LogErrorOnce("PlayerMovement2D: InputManager not available."); // Uncomment for debugging
                return;
            }

            // --- Read Input ---
            Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            // Process raw input for movement (GetAxis-like behavior from Vector2)
            moveInput.x = Mathf.Abs(rawInput.x) > 0.1f ? Mathf.Sign(rawInput.x) : 0f;
            moveInput.y = Mathf.Abs(rawInput.y) > 0.1f ? Mathf.Sign(rawInput.y) : 0f; // For up/down intentions

            bool jumpInputPressed = InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame();
            positionLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();

            // --- Update States ---
            bool intendsToInteractVertically = rawInput.y < -0.5f; // Strong downward intention (for crouch/drop)

            // Update crouching state
            // Player is crouching if: pressing down intención, is grounded, and not currently dropping through a platform.
            isCrouching = intendsToInteractVertically && isGrounded && !isDropping;

            // Coyote Time
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            // --- Handle Actions ---
            if (jumpInputPressed && !isDropping) // Prevent jumping while dropping
            {
                if (isGrounded && intendsToInteractVertically && IsOnTaggedPlatform(oneWayPlatformTag))
                {
                    // Debug.Log("PlayerMovement2D: Attempting to drop through platform."); // Uncomment for debugging
                    StartCoroutine(DropThroughPlatform());
                }
                else if (coyoteTimeCounter > 0f && !positionLocked && !isTouchingWall)
                {
                    // Debug.Log("PlayerMovement2D: Jump requested."); // Uncomment for debugging
                    jumpRequested = true;
                    // Immediately consume coyote time upon jump request
                    coyoteTimeCounter = 0f; 
                }
            }

            // Apply visual/collider changes for crouching
            HandleCrouchState();
        }

        private void FixedUpdate()
        {
            // Perform physics-based checks
            PerformGroundCheck();
            PerformWallCheck();

            Vector2 currentVelocity = rb.linearVelocity;

            // Horizontal Movement
            if (!positionLocked && !isDropping) // No horizontal movement if locked or dropping
            {
                currentVelocity.x = moveInput.x * moveSpeed;
            }
            else
            {
                currentVelocity.x = 0f;
            }

            // Apply Jump
            if (jumpRequested)
            {
                currentVelocity.y = jumpForce;
                jumpRequested = false; // Consume jump request
                isGrounded = false; // Assume no longer grounded after jump impulse
                // Debug.Log("PlayerMovement2D: Jump executed."); // Uncomment for debugging
            }

            // Enhanced Gravity (Fall Multiplier)
            // Apply if falling (velocity.y < 0) and not currently in the upward phase of a jump
            if (rb.linearVelocity.y < 0 && !jumpRequested) // Check rb.velocity for current falling state
            {
                currentVelocity.y += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
            
            rb.linearVelocity = currentVelocity;
        }

        private void PerformGroundCheck()
        {
            if (groundCheckOrigin == null) return;
            // Check if currently dropping, if so, player is not considered grounded by this check
            // as the collider might be off or we want to ensure they fall through.
            if (isDropping) 
            {
                isGrounded = false;
                return;
            }
            isGrounded = Physics2D.OverlapCircle(groundCheckOrigin.position, groundCheckRadius, groundLayer);
        }

        private void PerformWallCheck()
        {
            if (wallCheckOrigin == null) return;
            Vector2 origin = wallCheckOrigin.position;
            bool hitLeft = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer);
            bool hitRight = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
            isTouchingWall = hitLeft || hitRight;
        }

        private bool IsOnTaggedPlatform(string tag)
        {
            if (groundCheckOrigin == null) return false;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundLayer);
            foreach (Collider2D hitCollider in colliders)
            {
                if (hitCollider.CompareTag(tag))
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleCrouchState()
        {
            if (isCrouching && !isCrouchStateApplied)
            {
                isCrouchStateApplied = true;
                ApplyCrouchChanges(true);
                // Debug.Log("PlayerMovement2D: Crouching - State Applied."); // Uncomment for debugging
            }
            else if (!isCrouching && isCrouchStateApplied)
            {
                // Before standing up, check if there's space above
                if (CanStandUp())
                {
                    isCrouchStateApplied = false;
                    ApplyCrouchChanges(false);
                    // Debug.Log("PlayerMovement2D: Standing Up - State Applied."); // Uncomment for debugging
                }
            }
        }
        
        private void ApplyCrouchChanges(bool crouch)
        {
            if (capsuleCollider != null)
            {
                capsuleCollider.size = crouch ? new Vector2(originalColliderSize.x, originalColliderSize.y * crouchHeightMultiplier) : originalColliderSize;
                capsuleCollider.offset = crouch ? new Vector2(originalColliderOffset.x, originalColliderOffset.y * crouchHeightMultiplier) : originalColliderOffset;
            }
            else if (playerCollider is BoxCollider2D boxCollider) // Example for BoxCollider2D
            {
                boxCollider.size = crouch ? new Vector2(originalColliderSize.x, originalColliderSize.y * crouchHeightMultiplier) : originalColliderSize;
                boxCollider.offset = crouch ? new Vector2(originalColliderOffset.x, originalColliderOffset.y * crouchHeightMultiplier) : originalColliderOffset;
            }
            // Add more else if blocks for other collider types if needed

            if (firePoint != null)
            {
                firePoint.localPosition = crouch ? 
                    new Vector3(originalFirePointLocalPosition.x, originalFirePointLocalPosition.y * crouchHeightMultiplier, originalFirePointLocalPosition.z) : 
                    originalFirePointLocalPosition;
            }
        }

        private bool CanStandUp()
        {
            if (capsuleCollider == null || !isCrouchStateApplied) return true; // If not using capsule or not crouched, can stand.

            // Simulate standing up collider to check for obstructions
            float originalHeight = originalColliderSize.y;
            float crouchHeight = originalColliderSize.y * crouchHeightMultiplier;
            float heightDifference = originalHeight - crouchHeight;
            
            Vector2 castOrigin = (Vector2)transform.position + originalColliderOffset + new Vector2(0, heightDifference / 2f);
            // Use originalColliderSize for the stand-up check, but only need to check the upper part.
            // A simple box or circle cast upwards from the crouched collider's top might be sufficient.
            
            // More robust: calculate the would-be standing collider's center and size
            Vector2 standUpColliderCenter = (Vector2)transform.position + originalColliderOffset;
            Vector2 standUpColliderSize = originalColliderSize;

            // Perform an OverlapCapsule (if capsule) or OverlapBox (if box) with the "standing" dimensions
            // This needs to ignore the player's own collider. One way is to temporarily disable it.
            bool initiallyEnabled = playerCollider.enabled;
            playerCollider.enabled = false; // Temporarily disable to avoid self-collision in check
            
            Collider2D[] hits = Physics2D.OverlapCapsuleAll(standUpColliderCenter, standUpColliderSize, capsuleCollider.direction, 0f, groundLayer | wallLayer); // Check against ground and walls
            
            playerCollider.enabled = initiallyEnabled; // Re-enable collider

            foreach (Collider2D hit in hits)
            {
                if (hit != playerCollider) // Should be redundant due to temp disable, but good practice
                {
                    // Debug.Log($"PlayerMovement2D: Cannot stand up, obstructed by {hit.name}"); // Uncomment for debugging
                    return false; // Obstructed
                }
            }
            return true; // No obstructions
        }


        private IEnumerator DropThroughPlatform()
        {
            // Debug.Log("PlayerMovement2D: Initiating DropThroughPlatform."); // Uncomment for debugging
            isDropping = true;
            
            // Ensure player is not in crouch state (collider-wise) when dropping
            if (isCrouchStateApplied) 
            {
                isCrouching = false; // This will trigger HandleCrouchState in Update to revert collider if needed
                                     // or call ApplyCrouchChanges(false) directly if Update loop is too slow.
                ApplyCrouchChanges(false); // Force stand up collider for consistent drop
            }

            // Temporarily ignore collisions with one-way platforms
            // A more robust way is to use Physics2D.IgnoreCollision with found platform colliders
            // For simplicity here, just disabling main collider works if there are no other critical interactions during drop.
            
            Collider2D platformCollider = null;
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundLayer);
            foreach(Collider2D hit in hits)
            {
                if (hit.CompareTag(oneWayPlatformTag))
                {
                    platformCollider = hit;
                    Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
                    // Debug.Log($"PlayerMovement2D: Ignoring collision with platform {platformCollider.name}."); // Uncomment for debugging
                    break; 
                }
            }
            
            // playerCollider.enabled = false; // Old simpler way

            yield return new WaitForSeconds(dropThroughTime);

            // playerCollider.enabled = true; // Old simpler way
            if (platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
                // Debug.Log($"PlayerMovement2D: Restored collision with platform {platformCollider.name}."); // Uncomment for debugging
            }
            
            isDropping = false;
            // Debug.Log("PlayerMovement2D: Finished DropThroughPlatform."); // Uncomment for debugging
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckOrigin != null)
            {
                Gizmos.color = groundCheckGizmoColor;
                Gizmos.DrawWireSphere(groundCheckOrigin.position, groundCheckRadius);
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