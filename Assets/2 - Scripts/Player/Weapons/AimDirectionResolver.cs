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
        [Header("Configuration")]
        [Tooltip("Threshold for vertical stick input to be considered a primary aim direction (e.g., straight up/down).")]
        [SerializeField] private float verticalAimThreshold = 0.7f;
        
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
            // If movement is locked, the player can aim in 8 directions freely.
            if (stateManager.PositionLockInputActive)
            {
                if (rawInput.sqrMagnitude > 0.1f * 0.1f)
                {
                    // Snap to 8 directions (horizontal, vertical, diagonal)
                    float angle = Mathf.Atan2(rawInput.y, rawInput.x) * Mathf.Rad2Deg;
                    float snappedAngle = Mathf.Round(angle / 45.0f) * 45.0f;
                    return Quaternion.Euler(0, 0, snappedAngle) * Vector2.right;
                }
                // If locked but no input, aim forward.
                return new Vector2(stateManager.FacingDirection, 0);
            }

            // --- Standard Movement Aiming Logic ---
            
            // Vertical aiming takes priority
            if (Mathf.Abs(rawInput.y) > verticalAimThreshold)
            {
                // Straight up
                if (rawInput.y > 0) return Vector2.up;
                // Straight down (only if in the air)
                if (rawInput.y < 0 && !stateManager.IsGrounded) return Vector2.down;
            }

            // Diagonal aiming (requires some horizontal input)
            if (rawInput.y > 0.1f && Mathf.Abs(rawInput.x) > 0.1f)
            {
                return new Vector2(Mathf.Sign(rawInput.x), 1).normalized;
            }
            if (rawInput.y < -0.1f && !stateManager.IsGrounded && Mathf.Abs(rawInput.x) > 0.1f)
            {
                return new Vector2(Mathf.Sign(rawInput.x), -1).normalized;
            }

            // Default to aiming horizontally in the direction the player is facing.
            return new Vector2(stateManager.FacingDirection, 0);
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

