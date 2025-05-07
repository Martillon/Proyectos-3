using UnityEngine;
using Scripts.Core;
using Scripts.Player.Movement;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// Resolves player aim direction based on input and player state.
    /// Handles 8-directional aiming including special rules for crouching, jumping, locking, and dropping through platforms.
    /// </summary>
    public class AimDirectionResolver : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the PlayerMovement2D script to get player state (grounded, crouching, etc.).")]
        [SerializeField] private PlayerMovement2D playerMovement;

        [Header("Debug")]
        [SerializeField] private float gizmoLength = 1f;
        [SerializeField] private Color gizmoColor = Color.green;

        private Vector2 currentAimDirection = Vector2.right; // Default aiming right
        private float lastHorizontalInputX = 1f; // Default to right for initial state or no horizontal input

        /// <summary>
        /// The current calculated aiming direction.
        /// </summary>
        public Vector2 CurrentDirection => currentAimDirection;

        private void Awake()
        {
            if (playerMovement == null)
            {
                playerMovement = GetComponentInParent<PlayerMovement2D>();
                if (playerMovement == null)
                {
                    playerMovement = GetComponent<PlayerMovement2D>();
                }
                if (playerMovement == null)
                {
                    Debug.LogError("AimDirectionResolver: PlayerMovement2D is not assigned and could not be found. Aiming will be compromised.");
                }
            }
        }

        private void Update()
        {
            if (playerMovement == null)
            {
                // Debug.LogErrorOnce("AimDirectionResolver: Cannot update aim due to missing PlayerMovement2D reference."); // Uncomment for debugging
                return;
            }

            Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            float inputX = Mathf.Approximately(rawInput.x, 0f) ? 0f : Mathf.Sign(rawInput.x);
            float inputY = Mathf.Approximately(rawInput.y, 0f) ? 0f : Mathf.Sign(rawInput.y);

            bool isGrounded = playerMovement.IsGrounded;
            bool isCrouching = playerMovement.IsCrouching;
            bool isLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
            bool isDropping = playerMovement.IsDropping;

            if (inputX != 0) // Store last non-zero horizontal input to remember facing direction
            {
                lastHorizontalInputX = inputX;
            }

            currentAimDirection = ResolveDirection(inputX, inputY, isGrounded, isCrouching, isLocked, isDropping);
        }

        private Vector2 ResolveDirection(float inputX, float inputY, bool isGrounded, bool isCrouching, bool isLocked, bool isDropping)
        {
            // Priority 1: Dropping through platform - always aim horizontally forward
            if (isDropping)
            {
                return new Vector2(lastHorizontalInputX, 0f).normalized;
            }

            Vector2 newDirection = currentAimDirection; // Start with the current direction as a fallback

            // Priority 2: Vertical input (Up or Down)
            if (inputY > 0) // Pressing Up
            {
                newDirection = (inputX != 0) ? new Vector2(inputX, 1f).normalized : Vector2.up;
            }
            else if (inputY < 0) // Pressing Down
            {
                if (isLocked) // Position Lock + Down
                {
                    newDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                }
                else // Not Locked + Pressing Down
                {
                    if (isGrounded) // Grounded + Down (implies crouching or attempting to)
                    {
                        // If isCrouching is true, PlayerMovement2D has confirmed crouch state
                        newDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : new Vector2(lastHorizontalInputX, 0f);
                    }
                    else // In Air + Pressing Down (Jumping + Down)
                    {
                        newDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                    }
                }
            }
            // Priority 3: Horizontal input only (No Vertical Input)
            else if (inputX != 0)
            {
                newDirection = new Vector2(inputX, 0f);
            }
            // Priority 4: No directional input
            else
            {
                if (isGrounded && !isLocked && !isCrouching) // Idle on ground, not locked, not crouching
                {
                    newDirection = new Vector2(lastHorizontalInputX, 0f); // Aim horizontally in the last faced direction
                }
                // else, newDirection remains currentAimDirection (maintains aim during jump, lock, while crouching with no new input, etc.)
            }
            
            // Ensure the resolved direction is normalized. If it becomes zero (e.g. from contradictory logic), default to horizontal.
            return newDirection.sqrMagnitude > 0.01f ? newDirection.normalized : new Vector2(lastHorizontalInputX, 0f).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying && playerMovement == null) // Attempt to find PlayerMovement for editor preview
            {
                if (transform.parent != null) playerMovement = GetComponentInParent<PlayerMovement2D>();
                if (playerMovement == null) playerMovement = GetComponent<PlayerMovement2D>();
            }
            
            Gizmos.color = gizmoColor;
            Vector3 position = transform.position; 
            
            // Simulate states for editor gizmo drawing (very basic)
            Vector2 simulatedAim = Application.isPlaying ? currentAimDirection : new Vector2(lastHorizontalInputX,0);

            Gizmos.DrawLine(position, position + (Vector3)(simulatedAim * gizmoLength));
        }

        // Called when the script is loaded or a value is changed in the inspector (Editor only).
        private void OnValidate()
        {
            if (playerMovement == null)
            {
                // Try to auto-assign if on the same GameObject or parent.
                playerMovement = GetComponent<PlayerMovement2D>();
                if (playerMovement == null && transform.parent != null)
                {
                    playerMovement = GetComponentInParent<PlayerMovement2D>();
                }
            }
        }
    }
}

