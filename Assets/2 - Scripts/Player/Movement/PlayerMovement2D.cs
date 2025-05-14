using Scripts.Core;
using System.Collections;
using Scripts.Player.Core;
using Scripts.Player.Weapons;
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
        
        [Header("Animation & Visuals")]
        [Tooltip("Animator component for the player's body animations.")]
        [SerializeField] private Animator bodyAnimator;
        [Tooltip("Transform of the child GameObject that contains the player's main visuals (SpriteRenderer & Body Animator). This object will be flipped.")]
        [SerializeField] private Transform playerVisualsContainer; // e.g., a child named "SpriteAndBodyAnimator"
        [Tooltip("Optional: GameObject representing the player's aimable arm/weapon mount. Will be handled by HandleAimableArmVisibility.")]
        [SerializeField] private GameObject aimableArmObject;

        [Header("Aiming Components (Reference needed for arm visibility logic)")]
        [SerializeField] private AimDirectionResolver aimResolver;


        // --- Animator Parameter Hashes (for efficiency) ---
        private readonly int animIsMovingHash = Animator.StringToHash("isMoving");
        private readonly int animIsGroundedHash = Animator.StringToHash("isGrounded");
        private readonly int animIsCrouchingHash = Animator.StringToHash("isCrouching");
        // private readonly int animVerticalSpeedHash = Animator.StringToHash("VerticalSpeed"); // Si decides usarlo
        private readonly int animDieTriggerHash = Animator.StringToHash("Die");
        private readonly int animVictoryTriggerHash = Animator.StringToHash("Victory");

        private float facingDirection = 1f; // 1 for right, -1 for left (visuals default to right)

        [Header("Debug Gizmos")]
        [SerializeField] private Color groundCheckGizmoColor = Color.green;
        [SerializeField] private Color wallCheckGizmoColor = Color.cyan;

        // Component references
        private Rigidbody2D _rb;
        private Collider2D _playerCollider;
        private CapsuleCollider2D _capsuleCollider;

        // Internal State Variables
        private Vector2 _moveInputVector;
        private bool _isGrounded;
        private bool _jumpInputRequested;
        private bool _isCrouchingLogic;
        private bool _isCrouchVisualApplied;
        private bool _isPositionLockedInput;
        private bool _isTouchingWallState;
        private bool _isDroppingFromPlatform = false;
        private float _coyoteTimeRemaining;

        // Original values
        private Vector2 _originalCapsuleSize;
        private Vector2 _originalCapsuleOffset;
        private Vector3 _originalFirePointLocalPos;
        
        public bool IsGrounded => _isGrounded;
        public bool IsCrouching => _isCrouchingLogic; 
        public bool IsDropping => _isDroppingFromPlatform;
        public bool IsTouchingWall => _isTouchingWallState;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerCollider = GetComponent<Collider2D>();
            _capsuleCollider = GetComponent<CapsuleCollider2D>();

            if (_rb == null) Debug.LogError("P2DM: Rigidbody2D component not found!", this);
            if (_playerCollider == null) Debug.LogError("P2DM: A Collider2D component is required!", this);
            if (groundCheckOrigin == null) Debug.LogError("P2DM: 'Ground Check Origin' not assigned!", this);
            if (wallCheckOrigin == null) Debug.LogError("P2DM: 'Wall Check Origin' not assigned!", this);
            if (bodyAnimator == null) Debug.LogWarning("P2DM: 'Body Animator' not assigned. Player animations will not work.", this);
            if (playerVisualsContainer == null) Debug.LogWarning("P2DM: 'Player Visuals Container' not assigned. Player flipping might not work as intended.", this);
            if (aimResolver == null) aimResolver = GetComponentInChildren<AimDirectionResolver>(); // Try to find


            if (_capsuleCollider != null)
            {
                _originalCapsuleSize = _capsuleCollider.size;
                _originalCapsuleOffset = _capsuleCollider.offset;
            }
            else
            {
                _originalCapsuleSize = _playerCollider.bounds.size;
                _originalCapsuleOffset = _playerCollider.offset;
            }

            if (firePoint != null) _originalFirePointLocalPos = firePoint.localPosition;

            // Set initial facing direction based on scale (if visuals container exists)
            if(playerVisualsContainer != null) facingDirection = Mathf.Sign(playerVisualsContainer.localScale.x);
        }
        
        private void OnEnable()
        {
            PlayerEvents.OnPlayerDeath += HandlePlayerDeathAnimation;
            PlayerEvents.OnLevelCompleted += HandlePlayerVictoryAnimation; // Assuming you have this event
        }

        private void OnDisable()
        {
            PlayerEvents.OnPlayerDeath -= HandlePlayerDeathAnimation;
            PlayerEvents.OnLevelCompleted -= HandlePlayerVictoryAnimation;
        }

        private void Update()
        {
            if (InputManager.Instance?.Controls == null) return;

            HandleInput();
            PerformGroundCheck(); // Update grounded state based on physics
            UpdateLogicalStates(); // Update crouching, coyote time
            HandleJumpAndDropActions();
            
            ApplyCrouchVisualsAndCollider(); // Apply collider/firepoint changes
            HandleAimableArmVisibility();   // Handle visibility of the aimable arm
            UpdateBodyAnimatorParameters(); // Update animator parameters
            HandleVisualFlip();             // Flip the player visuals
        }
        
        private void HandleJumpAndDropActions()
        {
            bool jumpButtonPressedThisFrame = InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame();
            bool intendsToPressDown = _moveInputVector.y < -0.5f;


            if (jumpButtonPressedThisFrame && !_isDroppingFromPlatform)
            {
                if (_isGrounded && intendsToPressDown && IsOnTaggedPlatform(oneWayPlatformTag))
                {
                    StartCoroutine(DropThroughPlatformCoroutine());
                }
                else if (_coyoteTimeRemaining > 0f && !_isPositionLockedInput && !_isCrouchingLogic && !_isTouchingWallState)
                {
                    _jumpInputRequested = true;
                    _coyoteTimeRemaining = 0f;
                }
            }
        }
        
        private void HandleVisualFlip()
        {
            if (playerVisualsContainer == null) return;

            // Only flip if not position locked and there's horizontal movement input,
            // or if not moving but aim direction changes horizontal facing (more complex, handle with AimDirectionResolver if needed for visuals)
            // For simplicity: flip based on movement input if not locked.
            if (!_isPositionLockedInput && Mathf.Abs(_moveInputVector.x) > 0.01f)
            {
                facingDirection = _moveInputVector.x > 0 ? 1f : -1f;
            }
            // Else, maintain current facingDirection (e.g., when aiming while stationary after moving)

            playerVisualsContainer.localScale = new Vector3(facingDirection, 1f, 1f);
        }

        private void HandleInput()
        {
            Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            _moveInputVector.x = Mathf.Abs(rawInput.x) > 0.1f ? Mathf.Sign(rawInput.x) : 0f;
            _moveInputVector.y = Mathf.Abs(rawInput.y) > 0.1f ? Mathf.Sign(rawInput.y) : 0f;
            _isPositionLockedInput = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
        }

        private void UpdateLogicalStates()
        {
            bool intendsToPressDown = _moveInputVector.y < -0.5f; // Using processed input now
            _isCrouchingLogic = intendsToPressDown && _isGrounded && !_isDroppingFromPlatform;
            // Debug.Log($"P2DM.Update :: intendsToPressDown: {intendsToPressDown}, isGrounded: {_isGrounded}, isDropping: {_isDroppingFromPlatform} => isCrouchingLogic: {_isCrouchingLogic}");


            if (_isGrounded) _coyoteTimeRemaining = coyoteTime;
            else _coyoteTimeRemaining -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            PerformWallCheck(); // Wall check is physics-based
            
            Vector2 currentVelocity = _rb.linearVelocity;
            bool lockHorizontalMovement = _isPositionLockedInput || (_isCrouchingLogic && _isGrounded);
            // Debug.Log($"P2DM.FixedUpdate :: lockHorizontal: {lockHorizontalMovement} (isPosLocked: {_isPositionLockedInput}, isCrouchLogic: {_isCrouchingLogic}, isGrounded: {_isGrounded})");

            if (!lockHorizontalMovement && !_isDroppingFromPlatform)
            {
                currentVelocity.x = _moveInputVector.x * moveSpeed;
            }
            else
            {
                currentVelocity.x = 0f;
                // if (lockHorizontalMovement) Debug.Log("P2DM.FixedUpdate :: Horizontal movement LOCKED.");
            }

            if (_jumpInputRequested)
            {
                currentVelocity.y = jumpForce;
                _jumpInputRequested = false;
                _isGrounded = false;
            }

            if (_rb.linearVelocity.y < 0 && !_jumpInputRequested) 
            {
                currentVelocity.y += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
            
            _rb.linearVelocity = currentVelocity;
        }

        private void PerformGroundCheck()
        {
            if (groundCheckOrigin == null)
            {
                _isGrounded = false; return;
            }

            if (_isDroppingFromPlatform)
            {
                _isGrounded = false; return;
            } 
            
            _isGrounded = Physics2D.OverlapCircle(groundCheckOrigin.position, groundCheckRadius, groundLayer);
        }

        private void PerformWallCheck()
        {
            if (wallCheckOrigin == null)
            {
                _isTouchingWallState = false; return;
            } 
            Vector2 origin = wallCheckOrigin.position; _isTouchingWallState 
                = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer)
                  || Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
        }

        private bool IsOnTaggedPlatform(string tag)
        {
            if (groundCheckOrigin == null || !_isGrounded) return false; 
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundLayer); 
            foreach (Collider2D hitCollider in colliders) 
            {
                if (hitCollider.CompareTag(tag) && hitCollider.GetComponent<PlatformEffector2D>() != null)
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
                SetCrouchStateOnColliderAndArm(true);
            }
            else if (!_isCrouchingLogic && _isCrouchVisualApplied)
            {
                if (CanStandUpSafely())
                {
                    _isCrouchVisualApplied = false;
                    SetCrouchStateOnColliderAndArm(false);
                }
            }
        }
        
        private void SetCrouchStateOnColliderAndArm(bool crouch)
        {
            // Collider and FirePoint adjustment
            if (_capsuleCollider != null)
            {
                _capsuleCollider.size = crouch ? new Vector2(_originalCapsuleSize.x, _originalCapsuleSize.y * crouchHeightMultiplier) : _originalCapsuleSize;
                _capsuleCollider.offset = crouch ? new Vector2(_originalCapsuleOffset.x, _originalCapsuleOffset.y * crouchHeightMultiplier) : _originalCapsuleOffset;
            }
            else if (_playerCollider is BoxCollider2D boxCol)
            {
                boxCol.size = crouch ? new Vector2(_originalCapsuleSize.x, _originalCapsuleSize.y * crouchHeightMultiplier) : _originalCapsuleSize;
                boxCol.offset = crouch ? new Vector2(_originalCapsuleOffset.x, _originalCapsuleOffset.y * crouchHeightMultiplier) : _originalCapsuleOffset;
            }
            if (firePoint != null)
            {
                firePoint.localPosition = crouch ? 
                    new Vector3(_originalFirePointLocalPos.x, _originalFirePointLocalPos.y * crouchHeightMultiplier, _originalFirePointLocalPos.z) : 
                    _originalFirePointLocalPos;
            }
            // Note: Arm visibility is now handled by HandleAimableArmVisibility
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
            if (!_isCrouchVisualApplied) return true;
            if (_capsuleCollider == null) return true; 
            
            Vector2 standUpCenter = (Vector2)transform.position + _originalCapsuleOffset; 
            
            Vector2 standUpSize = _originalCapsuleSize; _playerCollider.enabled = false; 
            
            Collider2D[] obstructions = Physics2D.OverlapCapsuleAll
                (standUpCenter, standUpSize, _capsuleCollider.direction, 0f, groundLayer | wallLayer); 
            
            _playerCollider.enabled = true;

            foreach (Collider2D obstruction in obstructions)
            {
                if (obstruction != _playerCollider) return false; 
            } 
            
            return true;
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
        
        // --- Animation and Arm Visibility ---
        private void UpdateBodyAnimatorParameters()
        {
            if (bodyAnimator == null) return;

            bodyAnimator.SetBool(animIsGroundedHash, _isGrounded);
            // Usar _moveInputVector.x para "isMoving" da una respuesta más inmediata que rb.velocity.x
            // y evita que la animación de correr se quede si choca contra una pared.
            bodyAnimator.SetBool(animIsMovingHash, Mathf.Abs(_moveInputVector.x) > 0.01f && _isGrounded && !_isCrouchingLogic);
            bodyAnimator.SetBool(animIsCrouchingHash, _isCrouchingLogic && _isGrounded); // Animación de crouch si está lógicamente agachado y en suelo

            // Si tu estado "Fall / Jump" usa VerticalSpeed:
            // bodyAnimator.SetFloat(animVerticalSpeedHash, _rb.velocity.y);
        }

        private void HandleAimableArmVisibility()
        {
            if (aimableArmObject == null) return;

            bool showArm = true;
            if (_isCrouchVisualApplied) // Si el collider/visuals de agachado están aplicados
            {
                showArm = false;
            }
            else if (!_isGrounded && aimResolver != null && aimResolver.IsAimingDownwards)
            {
                showArm = false;
            }
            // Considerar añadir: else if (IsInDeathOrVictoryAnimation()) showArm = false;

            if (aimableArmObject.activeSelf != showArm)
            {
                aimableArmObject.SetActive(showArm);
            }
        }

        // --- Event Handlers for Death/Victory Animations ---
        private void HandlePlayerDeathAnimation()
        {
            if (bodyAnimator != null)
            {
                bodyAnimator.SetTrigger(animDieTriggerHash);
            }
            // Podrías también deshabilitar este script de movimiento aquí si PlayerHealthSystem no lo hace
            // this.enabled = false; 
        }

        private void HandlePlayerVictoryAnimation(string levelIdContext) // El parámetro puede no ser usado aquí
        {
            if (bodyAnimator != null)
            {
                bodyAnimator.SetTrigger(animVictoryTriggerHash);
            }
            // También podrías querer deshabilitar el movimiento
            // this.enabled = false; 
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