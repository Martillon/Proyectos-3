using UnityEngine;
using Scripts.Player.Core;

namespace Scripts.Player.Movement.Detectors
{
    /// <summary>
    /// Detects if the player is touching a wall using raycasts.
    /// Updates the PlayerStateManager with this information.
    /// </summary>
    public class PlayerWallDetector : MonoBehaviour
    {
        [Header("Wall Check Configuration")]
        [Tooltip("The transform representing the origin for wall detection raycasts (usually the player's center).")]
        [SerializeField] private Transform wallCheckOrigin;
        [Tooltip("The horizontal distance to cast the ray for detecting walls.")]
        [SerializeField] private float wallCheckDistance = 0.5f;
        [Tooltip("LayerMask defining what layers are considered walls.")]
        [SerializeField] private LayerMask wallLayerMask;

        [Header("Gizmos")]
        [SerializeField] private Color gizmoColor = Color.cyan;

        private PlayerStateManager _stateManager;

        private void Awake()
        {
            _stateManager = GetComponentInParent<PlayerStateManager>();
            if (_stateManager == null)
            {
                Debug.LogError("PlayerWallDetector: PlayerStateManager not found! This component will be disabled.", this);
                enabled = false;
                return;
            }
            if (wallCheckOrigin == null)
            {
                Debug.LogError("PlayerWallDetector: 'Wall Check Origin' is not assigned.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (wallCheckOrigin == null) return;
            DetectWall();
        }

        private void DetectWall()
        {
            // It's often better to check against the direction the player is *facing* rather than both sides,
            // unless you have mechanics like wall-sliding on your back. This version checks in the facing direction.
            float facingDirection = _stateManager.FacingDirection;
            
            RaycastHit2D hit = Physics2D.Raycast(wallCheckOrigin.position, Vector2.right * facingDirection, wallCheckDistance, wallLayerMask);

            _stateManager.SetWallState(hit.collider != null);

            // For debugging in scene view
            #if UNITY_EDITOR
            Debug.DrawRay(wallCheckOrigin.position, Vector2.right * facingDirection * wallCheckDistance, hit.collider != null ? Color.red : gizmoColor);
            #endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (wallCheckOrigin != null)
            {
                Gizmos.color = gizmoColor;
                Vector2 origin = wallCheckOrigin.position;
                // Draw lines for both directions in the editor for easy setup
                Gizmos.DrawLine(origin, origin + Vector2.left * wallCheckDistance);
                Gizmos.DrawLine(origin, origin + Vector2.right * wallCheckDistance);
            }
        }
#endif
    }
}