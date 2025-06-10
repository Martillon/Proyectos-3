using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// Calculates the player's aiming direction based on input and player state.
    /// Provides a normalized direction vector for the weapon system to use.
    /// </summary>
    public class AimDirectionResolver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStateManager stateManager;

        public Vector2 CurrentDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            if (stateManager == null)
            {
                stateManager = GetComponentInParent<PlayerStateManager>();
            }
            if (stateManager == null)
            {
                Debug.LogError("AimDirectionResolver: PlayerStateManager not found!", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (stateManager == null) return;
            
            // Get raw analog stick input for precise angle calculation.
            Vector2 rawMoveInput = InputManager.Instance?.Controls.Player.Move.ReadValue<Vector2>() ?? Vector2.zero;
            
            CurrentDirection = CalculateAimDirection(rawMoveInput);
        }

        private Vector2 CalculateAimDirection(Vector2 rawInput)
        {
            if (stateManager.PositionLockInputActive)
            {
                if (rawInput.sqrMagnitude > 0.1f * 0.1f)
                {
                    float angle = Mathf.Atan2(rawInput.y, rawInput.x) * Mathf.Rad2Deg;
                    float snappedAngle = Mathf.Round(angle / 45.0f) * 45.0f;
                    return Quaternion.Euler(0, 0, snappedAngle) * Vector2.right;
                }
                return new Vector2(stateManager.FacingDirection, 0);
            }

            // --- NEW Standard Movement Aiming Logic ---
            float horizontal = rawInput.x;
            float vertical = rawInput.y;
            Vector2 aimDirection = new Vector2(stateManager.FacingDirection, 0); // Default to forward

            // Check for significant vertical input
            if (Mathf.Abs(vertical) > 0.3f) // Use a less strict threshold
            {
                // Check for significant horizontal input to determine diagonal vs. straight
                if (Mathf.Abs(horizontal) > 0.3f)
                {
                    // Diagonal Aiming
                    aimDirection = new Vector2(Mathf.Sign(horizontal), Mathf.Sign(vertical));
                }
                else
                {
                    // Straight Vertical Aiming
                    aimDirection = new Vector2(0, Mathf.Sign(vertical));
                }
            }
            // If no significant vertical input, check for horizontal
            else if (Mathf.Abs(horizontal) > 0.1f)
            {
                // Straight Horizontal Aiming
                aimDirection = new Vector2(stateManager.FacingDirection, 0);
            }
            // If no significant input at all, keep aiming forward
            else
            {
                aimDirection = new Vector2(stateManager.FacingDirection, 0);
            }
    
            // Constraint: You cannot aim straight down while on the ground.
            if (stateManager.IsGrounded && aimDirection.x == 0 && aimDirection.y < 0)
            {
                return new Vector2(stateManager.FacingDirection, 0); // Revert to horizontal
            }

            return aimDirection.normalized;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (stateManager == null) return;
            Gizmos.color = Color.green;
            Vector3 origin = stateManager.transform.position;
            Gizmos.DrawLine(origin, origin + (Vector3)CurrentDirection * 1.5f);
        }
#endif
    }
}

