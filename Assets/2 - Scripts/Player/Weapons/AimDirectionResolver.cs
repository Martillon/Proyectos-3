using UnityEngine;
using Scripts.Core;
using Scripts.Player.Movement;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// Resolves player aim direction based on input and player state.
    /// Handles 8-directional aiming including special rules for crouching, jumping and locking.
    /// </summary>
    public class AimDirectionResolver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement2D movement;

        [Header("Debug")]
        [SerializeField] private float gizmoLength = 1f;
        [SerializeField] private Color gizmoColor = Color.green;

        private Vector2 currentDirection = Vector2.right;

        public Vector2 CurrentDirection => currentDirection;

        private void Update()
        {
            Vector2 input = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            bool isGrounded = movement.IsGrounded;
            bool isLocked = InputManager.Instance.Controls.Player.PositionLock.IsPressed();
            bool isJumping = !isGrounded;

            currentDirection = ResolveDirection(input, isGrounded, isLocked, isJumping);
        }

        private Vector2 ResolveDirection(Vector2 input, bool isGrounded, bool isLocked, bool isJumping)
        {
            Vector2 direction = Vector2.zero;

            bool hasHorizontal = Mathf.Abs(input.x) > 0.5f;
            bool hasVertical = Mathf.Abs(input.y) > 0.5f;

            // ↑ Only up
            if (input.y > 0.5f && !hasHorizontal)
            {
                direction = Vector2.up;
            }
            // ↓ Jumping + down
            else if (isJumping && input.y < -0.5f)
            {
                direction = Vector2.down;
            }
            // ↓ Grounded + down + locked
            else if (isGrounded && input.y < -0.5f && isLocked)
            {
                direction = Vector2.down;
            }
            // ↘↙ Diagonal down allowed only if jumping or locked
            else if (input.y < -0.5f && hasHorizontal && (isJumping || isLocked))
            {
                direction = new Vector2(Mathf.Sign(input.x), -1f).normalized;
            }
            // ↓ Grounded + down = crouch, aim forward
            else if (isGrounded && input.y < -0.5f)
            {
                if (currentDirection.x != 0)
                    direction = new Vector2(Mathf.Sign(currentDirection.x), 0f);
                else
                    direction = Vector2.right;
            }
            // ↗↖ Diagonals upward
            else if (input.y > 0.5f && hasHorizontal)
            {
                direction = new Vector2(Mathf.Sign(input.x), 1f).normalized;
            }
            // →← Lateral only
            else if (hasHorizontal)
            {
                direction = new Vector2(Mathf.Sign(input.x), 0f);
            }

            return direction != Vector2.zero ? direction.normalized : currentDirection;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(currentDirection * gizmoLength));
        }
    }
}

