using UnityEngine;
using Scripts.Core;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// AimDirectionResolver
    /// 
    /// Resolves raw input into one of 8 cardinal or diagonal aim directions.
    /// Automatically reads from InputManager and exposes current direction.
    /// Includes optional Gizmos drawing for debugging.
    /// Implements special handling: when down + lateral is pressed, aim only lateral.
    /// </summary>
    public class AimDirectionResolver : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private float gizmoLength = 1f;
        [SerializeField] private Color gizmoColor = Color.green;

        private Vector2 currentDirection = Vector2.right; // Default facing right

        /// <summary>
        /// Public getter for the current aim direction.
        /// </summary>
        public Vector2 CurrentDirection => currentDirection;

        private void Update()
        {
            Vector2 input = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>();
            currentDirection = ResolveDirection(input);
        }

        /// <summary>
        /// Converts a raw input vector into a normalized direction with discrete 8-way resolution.
        /// If down + left/right are pressed together, returns only horizontal aiming.
        /// </summary>
        /// <param name="input">The raw input from stick, D-Pad or keys</param>
        /// <returns>A normalized Vector2 representing direction, or previous direction if input is zero</returns>
        private Vector2 ResolveDirection(Vector2 input)
        {
            Vector2 dir = Vector2.zero;

            if (input.y < -0.5f && Mathf.Abs(input.x) > 0.5f)
            {
                // Down + side = crouch and shoot forward
                dir.x = Mathf.Sign(input.x);
                dir.y = 0;
            }
            else
            {
                if (input.x > 0.5f) dir.x = 1;
                else if (input.x < -0.5f) dir.x = -1;

                if (input.y > 0.5f) dir.y = 1;
                else if (input.y < -0.5f) dir.y = -1;
            }

            return dir != Vector2.zero ? dir.normalized : currentDirection;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(currentDirection * gizmoLength));
        }
    }
}

