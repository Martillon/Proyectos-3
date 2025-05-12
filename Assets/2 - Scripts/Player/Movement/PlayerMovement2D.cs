using Scripts.Core;
using System.Collections;
using UnityEngine;

namespace Scripts.Player.Movement
{
    /// <summary>
    /// PlayerMovement2D
    /// Handles 2D player movement, jumping, crouching (hold-to-crouch), gravity control,
    /// wall detection, coyote time, position lock, directional input handling, and platform drop-through.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal movement speed.")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("Vertical jump impulse force.")]
        [SerializeField] private float jumpForce = 10f;
        [Tooltip("Multiplier applied to gravity when the player is falling to make falls feel snappier.")]
        [SerializeField] private float fallMultiplier = 2.5f; // Slightly increased for better feel
        [Tooltip("Short time window (in seconds) after leaving a platform where the player can still jump.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("Ground Check")]
        [Tooltip("Transform representing the origin point for ground detection (usually at player's feet).")]
        [SerializeField] private Transform groundCheckOrigin;
        [Tooltip("Radius of the circle used for ground detection.")]
        [SerializeField] private float groundCheckRadius = 0.15f; // Adjusted for potentially more precise check
        [Tooltip("LayerMask defining what layers are considered 'Ground'.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Check")]
        [Tooltip("Transform representing the origin point for wall detection raycasts (usually player's center).")]
        [SerializeField] private Transform wallCheckOrigin;
        [Tooltip("Distance of the raycasts used to detect walls on either side.")]
        [SerializeField] private float wallCheckDistance = 0.1f;
        [Tooltip("LayerMask defining what layers are considered 'Walls'.")]
        [SerializeField] private LayerMask wallLayer;

        [Header("Platform Drop Settings")]
        [Tooltip("Duration (in seconds) for which collision with the dropped platform is ignored.")]
        [SerializeField] private float dropThroughTime = 0.25f; // Slightly increased for more reliable drop
        [Tooltip("Tag used to identify platforms that the player can drop through.")]
        [SerializeField] private string oneWayPlatformTag = "Platform"; // Ensure your platforms have this tag

        [Header("Crouch Settings")]
        [Tooltip("Multiplier applied to the player's collider height when crouching (e.g., 0.5 for half height).")]
        [SerializeField] private float crouchHeightMultiplier = 0.5f;
        [Tooltip("Optional: Reference to the player's fire point transform, to be adjusted during crouch.")]
        [SerializeField] private Transform firePoint;

        [Header("Debug Gizmos")]
        [SerializeField] private Color groundCheckGizmoColor = Color.green;
        [SerializeField] private Color wallCheckGizmoColor = Color.cyan;

        // Component References
        private Rigidbody2D _rb;
        private Collider2D _playerCollider;
        private CapsuleCollider2D _capsuleCollider; // Preferred collider type for smooth movement

        // Internal State Variables
        private Vector2 _moveInputVector;      // Stores processed horizontal/vertical input for movement
        private bool _isGrounded;
        private bool _jumpInputRequested;
        private bool _isCrouchingLogic;        // True if conditions for crouching are met (input + grounded)
        private bool _isCrouchVisualApplied;   // True if collider/visual changes for crouch are active
        private bool _isPositionLockedInput;   // True if player is holding the position lock input
        private bool _isTouchingWallState;
        private bool _isDroppingFromPlatform = false; // True if currently in the drop-through coroutine

        private float _coyoteTimeRemaining;

        // Original values for restoring collider/firepoint after crouching
        private Vector2 _originalCapsuleSize;
        private Vector2 _originalCapsuleOffset;
        private Vector3 _originalFirePointLocalPos;

        // Public Properties for other systems (e.g., AimDirectionResolver)
        public bool IsGrounded => _isGrounded;
        public bool IsCrouching => _isCrouchingLogic; // Expose the logical crouch state
        public bool IsDropping => _isDroppingFromPlatform;
        public bool IsTouchingWall => _isTouchingWallState;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerCollider = GetComponent<Collider2D>(); // General collider reference
            _capsuleCollider = GetComponent<CapsuleCollider2D>(); // Specific reference

            // Validate essential components and references
            if (_rb == null) Debug.LogError("PlayerMovement2D: Rigidbody2D component not found!", this);
            if (_playerCollider == null) Debug.LogError("PlayerMovement2D: A Collider2D component is required!", this);
            if (groundCheckOrigin == null) Debug.LogError("PlayerMovement2D: 'Ground Check Origin' transform is not assigned!", this);
            if (wallCheckOrigin == null) Debug.LogError("PlayerMovement2D: 'Wall Check Origin' transform is not assigned!", this);

            if (_capsuleCollider != null)
            {
                _originalCapsuleSize = _capsuleCollider.size;
                _originalCapsuleOffset = _capsuleCollider.offset;
            }
            else
            {
                Debug.LogWarning("PlayerMovement2D: CapsuleCollider2D not found. Crouching will use generic collider bounds, which might be less accurate.", this);
                // Fallback for non-capsule colliders (less ideal for precise crouch)
                _originalCapsuleSize = _playerCollider.bounds.size;
                _originalCapsuleOffset = _playerCollider.offset;
            }

            if (firePoint != null)
            {
                _originalFirePointLocalPos = firePoint.localPosition;
            }
        }

        private void Update()
        {
            if (InputManager.Instance?.Controls == null) return; // Guard clause

            // --- Read Input ---
            Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            _moveInputVector.x = Mathf.Abs(rawInput.x) > 0.1f ? Mathf.Sign(rawInput.x) : 0f;
            _moveInputVector.y = Mathf.Abs(rawInput.y) > 0.1f ? Mathf.Sign(rawInput.y) : 0f; // For up/down intentions

            bool jumpButtonPressedThisFrame = InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame();
            _isPositionLockedInput = InputManager.Instance.Controls.Player.PositionLock.IsPressed();

            // --- Update Grounded State (critical to do this early in Update) ---
            PerformGroundCheck(); // This updates the 'isGrounded' field

            // --- Update Crouching State (Hold-to-crouch) ---
            bool intendsToPressDown = rawInput.y < -0.5f; // Threshold for "down" input
            _isCrouchingLogic = intendsToPressDown && _isGrounded && !_isDroppingFromPlatform;

            // --- Coyote Time ---
            if (_isGrounded) _coyoteTimeRemaining = coyoteTime;
            else _coyoteTimeRemaining -= Time.deltaTime;

            // --- Handle Jump and Drop-Through Actions ---
            if (jumpButtonPressedThisFrame && !_isDroppingFromPlatform)
            {
                if (_isGrounded && intendsToPressDown && IsOnTaggedPlatform(oneWayPlatformTag))
                {
                    StartCoroutine(DropThroughPlatformCoroutine());
                }
                // Can jump if coyote time is active, not position locked, NOT CROUCHING, and not touching a wall
                else if (_coyoteTimeRemaining > 0f && !_isPositionLockedInput && !_isCrouchingLogic && !_isTouchingWallState)
                {
                    _jumpInputRequested = true;
                    _coyoteTimeRemaining = 0f; // Consume coyote time
                }
            }

            // Apply visual/collider changes for crouching
            ApplyCrouchVisualsAndCollider();
        }

        private void FixedUpdate()
        {
            // PerformWallCheck(); // Moved to Update if not strictly physics-dependent, or keep here if preferred
            
            Vector2 currentVelocity = _rb.linearVelocity;

            // --- Horizontal Movement ---
            // Lock horizontal movement if position lock input is active, OR if logically crouching on the ground
            bool lockHorizontalMovement = _isPositionLockedInput || (_isCrouchingLogic && _isGrounded);
            if (!lockHorizontalMovement && !_isDroppingFromPlatform)
            {
                currentVelocity.x = _moveInputVector.x * moveSpeed;
            }
            else
            {
                currentVelocity.x = 0f;
            }

            // --- Apply Jump ---
            if (_jumpInputRequested)
            {
                currentVelocity.y = jumpForce; // Apply jump force
                _jumpInputRequested = false;    // Consume request
                _isGrounded = false;            // Assume not grounded immediately after jump impulse
            }

            // --- Enhanced Gravity (Fall Multiplier) ---
            // Apply if velocity.y is negative (falling) AND a jump wasn't just requested this physics frame
            if (_rb.linearVelocity.y < 0 && !_jumpInputRequested) 
            {
                currentVelocity.y += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
            
            _rb.linearVelocity = currentVelocity;
        }

        private void PerformGroundCheck()
        {
            if (groundCheckOrigin == null) { _isGrounded = false; return; }
            // If currently in the process of dropping, not considered grounded for platform interactions
            if (_isDroppingFromPlatform) { _isGrounded = false; return; } 
            
            _isGrounded = Physics2D.OverlapCircle(groundCheckOrigin.position, groundCheckRadius, groundLayer);
        }

        private void PerformWallCheck() // Can be called in Update or FixedUpdate
        {
            if (wallCheckOrigin == null) { _isTouchingWallState = false; return; }
            Vector2 origin = wallCheckOrigin.position;
            _isTouchingWallState = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer) ||
                                  Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
        }

        private bool IsOnTaggedPlatform(string tag)
        {
            if (groundCheckOrigin == null || !_isGrounded) return false; // Must be grounded to be on a platform
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundLayer);
            foreach (Collider2D hitCollider in colliders)
            {
                if (hitCollider.CompareTag(tag) && hitCollider.GetComponent<PlatformEffector2D>() != null) // Check for effector too
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyCrouchVisualsAndCollider()
        {
            if (_isCrouchingLogic && !_isCrouchVisualApplied)
            {
                _isCrouchVisualApplied = true;
                SetCrouchStateOnCollider(true);
            }
            else if (!_isCrouchingLogic && _isCrouchVisualApplied)
            {
                if (CanStandUpSafely())
                {
                    _isCrouchVisualApplied = false;
                    SetCrouchStateOnCollider(false);
                }
            }
        }
        
        private void SetCrouchStateOnCollider(bool crouch)
        {
            if (_capsuleCollider != null)
            {
                _capsuleCollider.size = crouch ? new Vector2(_originalCapsuleSize.x, _originalCapsuleSize.y * crouchHeightMultiplier) : _originalCapsuleSize;
                _capsuleCollider.offset = crouch ? new Vector2(_originalCapsuleOffset.x, _originalCapsuleOffset.y * crouchHeightMultiplier) : _originalCapsuleOffset;
            }
            // Add similar logic for BoxCollider2D if capsuleCollider is null and playerCollider is BoxCollider2D
            else if (_playerCollider is BoxCollider2D boxCol)
            {
                 boxCol.size = crouch ? new Vector2(_originalCapsuleSize.x, _originalCapsuleSize.y * crouchHeightMultiplier) : _originalCapsuleSize; // Assuming originalCapsuleSize was set from box if no capsule
                 boxCol.offset = crouch ? new Vector2(_originalCapsuleOffset.x, _originalCapsuleOffset.y * crouchHeightMultiplier) : _originalCapsuleOffset;
            }


            if (firePoint != null)
            {
                firePoint.localPosition = crouch ? 
                    new Vector3(_originalFirePointLocalPos.x, _originalFirePointLocalPos.y * crouchHeightMultiplier, _originalFirePointLocalPos.z) : 
                    _originalFirePointLocalPos;
            }
        }

        private bool CanStandUpSafely()
        {
            if (!_isCrouchVisualApplied) return true; // Already standing or check not applicable
            if (_capsuleCollider == null) return true; // Cannot perform check without capsule reference

            // Calculate the "standing" collider's properties
            Vector2 standUpCenter = (Vector2)transform.position + _originalCapsuleOffset;
            Vector2 standUpSize = _originalCapsuleSize;
            
            // Temporarily disable player's own collider to avoid self-collision in the check
            _playerCollider.enabled = false;
            Collider2D[] obstructions = Physics2D.OverlapCapsuleAll(standUpCenter, standUpSize, _capsuleCollider.direction, 0f, groundLayer | wallLayer);
            _playerCollider.enabled = true; // Re-enable immediately

            foreach (Collider2D obstruction in obstructions)
            {
                // If any obstruction is found that isn't self (though self should be ignored due to temp disable)
                if (obstruction != _playerCollider) 
                {
                    // Debug.Log($"PlayerMovement2D: Cannot stand up, obstructed by {obstruction.name}"); // Uncomment for debugging
                    return false; 
                }
            }
            return true; // No obstructions found
        }

        private IEnumerator DropThroughPlatformCoroutine()
        {
            _isDroppingFromPlatform = true;
            _isCrouchingLogic = false; // Ensure not logically crouching during drop
            if (_isCrouchVisualApplied) // If collider was shrunk, restore it
            {
                SetCrouchStateOnCollider(false);
                _isCrouchVisualApplied = false;
            }

            Collider2D platformColliderToIgnore = null;
            Collider2D[] collidersUnderPlayer = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundLayer);
            foreach(Collider2D col in collidersUnderPlayer)
            {
                if (col.CompareTag(oneWayPlatformTag) && col.GetComponent<PlatformEffector2D>() != null)
                {
                    platformColliderToIgnore = col;
                    break; 
                }
            }
            
            if (platformColliderToIgnore != null)
            {
                Physics2D.IgnoreCollision(_playerCollider, platformColliderToIgnore, true);
                // A very slight downward nudge can help ensure separation if player is perfectly on edge
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -0.5f); // Or rb.AddForce(Vector2.down * tinyForce);

                yield return new WaitForSeconds(dropThroughTime);

                Physics2D.IgnoreCollision(_playerCollider, platformColliderToIgnore, false);
            }
            else
            {
                // If no specific platform found (e.g., jumped from edge), just wait out the drop time.
                // This might mean the player was already falling.
                yield return new WaitForSeconds(dropThroughTime * 0.5f); // Shorter wait if no platform to ignore
            }
            
            _isDroppingFromPlatform = false;
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