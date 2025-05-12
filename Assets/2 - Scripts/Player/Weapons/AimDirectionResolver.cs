using UnityEngine;
using Scripts.Core;
using Scripts.Player.Movement;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// Resolves player aim direction based on input and player state (grounded, crouching, locking, dropping).
    /// Handles 8-directional aiming.
    /// </summary>
    public class AimDirectionResolver : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the PlayerMovement2D script to get player state.")]
        [SerializeField] private PlayerMovement2D playerMovement;

        [Header("Debug")]
        [SerializeField] private float gizmoLength = 1f;
        [SerializeField] private Color gizmoColor = Color.green;

        private Vector2 _currentCalculatedAimDirection = Vector2.right;
        private float _lastNonZeroHorizontalInput = 1f; // Default to right

        public Vector2 CurrentDirection => _currentCalculatedAimDirection;

        private void Awake()
        {
            if (playerMovement == null)
            {
                playerMovement = GetComponentInParent<PlayerMovement2D>() ?? GetComponent<PlayerMovement2D>();
                if (playerMovement == null)
                    Debug.LogError("AimDirectionResolver: PlayerMovement2D reference is missing!", this);
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
            bool isCrouching = playerMovement.IsCrouching; // Logical crouch state from PlayerMovement2D
            bool isLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
            bool isDropping = playerMovement.IsDropping;

            _currentCalculatedAimDirection = CalculateAimDirection(inputX, inputY, isGrounded, isCrouching, isLocked, isDropping);
        }

        private Vector2 CalculateAimDirection(float inputX, float inputY, bool isGrounded, bool isCrouching, bool isLocked, bool isDropping)
        {
            // Priority 1: Dropping through platform - always aim horizontally forward
            if (isDropping)
            {
                return new Vector2(_lastNonZeroHorizontalInput, 0f).normalized;
            }

            Vector2 resolvedDirection = _currentCalculatedAimDirection; // Default to last known direction

            // Priority 2: Vertical input (Up or Down takes precedence if present)
            if (inputY > 0) // Pressing Up
            {
                resolvedDirection = (inputX != 0) ? new Vector2(inputX, 1f).normalized : Vector2.up;
            }
            else if (inputY < 0) // Pressing Down
            {
                if (isLocked) // Position Lock + Down
                {
                    resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                }
                else // Not Locked + Pressing Down
                {
                    if (isCrouching) // isCrouching implies grounded and pressing down
                    {
                        // If also pressing side: aim diagonal down. Else (only down): aim forward horizontally.
                        resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : new Vector2(_lastNonZeroHorizontalInput, 0f);
                    }
                    else if (!isGrounded) // In Air + Pressing Down (Jumping + Down)
                    {
                        resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                    }
                    // If grounded, pressing down, but NOT isCrouching (e.g. about to drop through platform but jump not pressed yet)
                    // then usually defaults to horizontal aim unless overridden by jump-drop logic.
                    // For now, if it falls through other conditions, it might keep last horizontal or current.
                    // This specific edge case is minor as drop/jump logic takes over.
                }
            }
            // Priority 3: Horizontal input only (No Vertical Input active for aiming)
            else if (inputX != 0)
            {
                resolvedDirection = new Vector2(inputX, 0f);
            }
            // Priority 4: No directional input from player for aiming
            else
            {
                // If idle on ground, not locked, not logically crouching: aim horizontally forward
                if (isGrounded && !isLocked && !isCrouching) 
                {
                    resolvedDirection = new Vector2(_lastNonZeroHorizontalInput, 0f);
                }
                // Otherwise (e.g., in air with no input, or locked with no input, or crouching with no new input),
                // maintain the previously calculated aim direction (resolvedDirection initialized to currentAimDirection).
            }
            
            // Ensure a valid, normalized direction. Default to horizontal facing if somehow zero.
            return resolvedDirection.sqrMagnitude > 0.001f ? resolvedDirection.normalized : new Vector2(_lastNonZeroHorizontalInput, 0f).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            // ... (Gizmo drawing logic - no functional changes needed here based on recent discussion) ...
            if (playerMovement == null && !Application.isPlaying) { /* ... find ... */ }
            Gizmos.color = gizmoColor;
            Vector3 pos = Application.isPlaying ? transform.position : (playerMovement != null ? playerMovement.transform.position : transform.position);
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

