using UnityEngine;
using Scripts.Core;
using Scripts.Player.Movement;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// AimDirectionResolver
    /// 
    /// Interprets player input into directional aiming based on movement, jump state and lock state.
    /// Applies game-specific logic for crouch, jump + down, and diagonal aiming.
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
        
        private void Awake()
        {
            if (movement == null)
            {
                movement = GetComponent<PlayerMovement2D>();
            }
        }

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

            // Sides only
            if (Mathf.Abs(input.x) > 0.5f && Mathf.Abs(input.y) < 0.5f)
            {
                direction = new Vector2(Mathf.Sign(input.x), 0f);
            }

            // Jumping + down = shoot down
            else if (isJumping && input.y < -0.5f)
            {
                direction = Vector2.down;
            }

            // On ground + down + locked = shoot down
            else if (isGrounded && input.y < -0.5f && isLocked)
            {
                direction = Vector2.down;
            }

            // On ground + down = crouch, aim defaults
            else if (isGrounded && input.y < -0.5f)
            {
                // Retain current lateral direction, default to right
                if (currentDirection.x != 0)
                    direction = new Vector2(Mathf.Sign(currentDirection.x), 0f);
                else
                    direction = Vector2.right;
            }

            // Up only
            else if (input.y > 0.5f && Mathf.Abs(input.x) < 0.5f)
            {
                direction = Vector2.up;
            }

            // Diagonal
            else if (Mathf.Abs(input.x) > 0.5f && Mathf.Abs(input.y) > 0.5f)
            {
                direction = new Vector2(Mathf.Sign(input.x), Mathf.Sign(input.y));
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

