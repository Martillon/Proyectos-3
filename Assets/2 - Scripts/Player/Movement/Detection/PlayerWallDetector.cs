using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core; // For PlayerStateManager

namespace Scripts.Player.Movement.Detectors
{
    /// <summary>
    /// Detects if the player is currently touching a wall on either side.
    /// Updates the PlayerStateManager with this information.
    /// </summary>
    public class PlayerWallDetector : MonoBehaviour
    {
        [Header("Wall Check Configuration")]
        [Tooltip("Transform representing the origin point for wall detection raycasts (usually player's center or slightly adjusted).")]
        [SerializeField] private Transform wallCheckOrigin;
        [Tooltip("Distance of the raycasts used to detect walls.")]
        [SerializeField] private float wallCheckDistance = 0.1f;
        [Tooltip("LayerMask defining what layers are considered 'Walls'.")]
        [SerializeField] private LayerMask wallDetectionLayerMask; // Renamed for clarity

        [Header("Debug")]
        [SerializeField] private Color gizmoColor = Color.cyan;

        private PlayerStateManager _playerStateManager;
        private bool _isCurrentlyTouchingWall;

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();
            if (_playerStateManager == null)
            {
                Debug.LogError("PlayerWallDetector: PlayerStateManager not found! Wall detection state cannot be updated.", this);
                enabled = false;
                return;
            }
            if (wallCheckOrigin == null)
            {
                Debug.LogError("PlayerWallDetector: 'Wall Check Origin' not assigned. Wall detection will not work.", this);
                enabled = false;
            }
        }

        void Update() // O FixedUpdate si prefieres sincronizarlo con la física para reacciones de pared
        {
            if (_playerStateManager == null || wallCheckOrigin == null) return;

            DetectWall();
            _playerStateManager.UpdateWallState(_isCurrentlyTouchingWall);
        }

        private void DetectWall()
        {
            Vector2 origin = wallCheckOrigin.position;
            
            // Se podría usar el FacingDirection del PlayerStateManager para solo chequear la pared de enfrente,
            // pero chequear ambos lados es más general para mecánicas como el wall slide o wall jump.
            bool hitLeft = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallDetectionLayerMask);
            bool hitRight = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallDetectionLayerMask);

            _isCurrentlyTouchingWall = hitLeft || hitRight;

            // Debug.DrawRay(origin, Vector2.left * wallCheckDistance, hitLeft ? Color.red : gizmoColor);
            // Debug.DrawRay(origin, Vector2.right * wallCheckDistance, hitRight ? Color.red : gizmoColor);
            // if(_isCurrentlyTouchingWall) Debug.Log("PlayerWallDetector: Touching wall.");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (wallCheckOrigin != null)
            {
                Gizmos.color = gizmoColor;
                Vector2 origin = wallCheckOrigin.position;
                Gizmos.DrawLine(origin, origin + Vector2.left * wallCheckDistance);
                Gizmos.DrawLine(origin, origin + Vector2.right * wallCheckDistance);
            }
        }
#endif
    }
}
// --- END OF FILE PlayerWallDetector.cs ---