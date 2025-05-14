using UnityEngine;
using Scripts.Core;
using Scripts.Player.Movement;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// Resolves player aim direction based on input and player state (grounded, crouching, locking, dropping).
    /// Handles 8-directional aiming and provides information about the aiming state.
    /// </summary>
    public class AimDirectionResolver : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the PlayerMovement2D script to get player state.")]
        [SerializeField] private PlayerMovement2D playerMovement;

        [Header("Aiming Configuration")]
        [Tooltip("The Y-component threshold to consider the player as aiming significantly downwards (e.g., -0.7f). Should be negative.")]
        [SerializeField] private float aimingDownThreshold = -0.7f;


        [Header("Debug")]
        [SerializeField] private float gizmoLength = 1f;
        [SerializeField] private Color gizmoColor = Color.green;

        private Vector2 _currentCalculatedAimDirection = Vector2.right;
        private float _lastNonZeroHorizontalInput = 1f; // Default to right

        /// <summary>
        /// Gets the current calculated aiming direction (normalized).
        /// </summary>
        public Vector2 CurrentDirection => _currentCalculatedAimDirection;

        /// <summary>
        /// Gets a value indicating whether the player is currently aiming significantly downwards.
        /// </summary>
        public bool IsAimingDownwards { get; private set; }


        private void Awake()
        {
            if (playerMovement == null)
            {
                playerMovement = GetComponentInParent<PlayerMovement2D>() ?? GetComponent<PlayerMovement2D>();
                if (playerMovement == null)
                    Debug.LogError("AimDirectionResolver: PlayerMovement2D reference is missing! This component will not function correctly.", this);
            }
        }

        private void Update()
        {
            if (playerMovement == null || InputManager.Instance?.Controls == null) return;

            Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            float inputX = Mathf.Abs(rawInput.x) > 0.1f ? Mathf.Sign(rawInput.x) : 0f;
            float inputY = Mathf.Abs(rawInput.y) > 0.1f ? Mathf.Sign(rawInput.y) : 0f;

            if (inputX != 0)
            {
                _lastNonZeroHorizontalInput = inputX;
            }

            bool isGrounded = playerMovement.IsGrounded;
            bool isCrouching = playerMovement.IsCrouching;
            bool isLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
            bool isDropping = playerMovement.IsDropping;

            _currentCalculatedAimDirection = CalculateAimDirection(inputX, inputY, isGrounded, isCrouching, isLocked, isDropping);

            // Update the IsAimingDownwards property based on the calculated direction
            IsAimingDownwards = _currentCalculatedAimDirection.y < aimingDownThreshold;
            // Debug.Log($"AimDirectionResolver: Current Aim: {_currentCalculatedAimDirection}, IsAimingDown: {IsAimingDownwards}"); // Uncomment for debugging
        }

        private Vector2 CalculateAimDirection(float inputX, float inputY, bool isGrounded, bool isCrouching, bool isLocked, bool isDropping)
        {
            if (isDropping)
            {
                return new Vector2(_lastNonZeroHorizontalInput, 0f).normalized;
            }

            Vector2 resolvedDirection = _currentCalculatedAimDirection; 

            if (inputY > 0) 
            {
                resolvedDirection = (inputX != 0) ? new Vector2(inputX, 1f).normalized : Vector2.up;
            }
            else if (inputY < 0) 
            {
                if (isLocked) 
                {
                    resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                }
                else 
                {
                    if (isCrouching) 
                    {
                        resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : new Vector2(_lastNonZeroHorizontalInput, 0f);
                    }
                    else if (!isGrounded) 
                    {
                        resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                    }
                    // If grounded, pressing down, but NOT logically crouching (e.g., if intendsToPressDown but isGrounded became false transiently before isCrouchingLogic updated)
                    // This state is less common if isCrouchingLogic is robust.
                    // If it falls through here, it might retain previous horizontal or diagonal aim.
                    // Defaulting to horizontal forward if just pressing down on ground (without full crouch criteria)
                    else if (isGrounded && inputX == 0) // Just pressing down on ground, no horizontal
                    {
                        resolvedDirection = new Vector2(_lastNonZeroHorizontalInput, 0f); // Aim forward
                    }
                }
            }
            else if (inputX != 0)
            {
                resolvedDirection = new Vector2(inputX, 0f);
            }
            else
            {
                if (isGrounded && !isLocked && !isCrouching) 
                {
                    resolvedDirection = new Vector2(_lastNonZeroHorizontalInput, 0f);
                }
            }
            
            return resolvedDirection.sqrMagnitude > 0.001f ? resolvedDirection.normalized : new Vector2(_lastNonZeroHorizontalInput, 0f).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (playerMovement == null && !Application.isPlaying) { 
                 playerMovement = GetComponentInParent<PlayerMovement2D>() ?? GetComponent<PlayerMovement2D>();
            }
            Gizmos.color = gizmoColor;
            Vector3 pos = Application.isPlaying ? transform.position : (playerMovement != null ? playerMovement.transform.position : transform.position);
            // Use the internal field for gizmo if not playing, as property might not be updated if Update() hasn't run.
            Vector2 dir = Application.isPlaying ? _currentCalculatedAimDirection : new Vector2(_lastNonZeroHorizontalInput, 0);
            Gizmos.DrawLine(pos, pos + (Vector3)(dir * gizmoLength));
        }

        private void OnValidate()
        {
            if (playerMovement == null)
            {
                playerMovement = GetComponentInParent<PlayerMovement2D>() ?? GetComponent<PlayerMovement2D>();
            }
        }
    }
}
// --- END OF FILE AimDirectionResolver.cs ---

