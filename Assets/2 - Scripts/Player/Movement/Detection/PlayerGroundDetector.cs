using UnityEngine;
using System.Collections.Generic; // Required for List
using Scripts.Player.Core; // For GameConstants and PlayerStateManager
// using Scripts.Environment.Interfaces; // Ya no es estrictamente necesario aquí si SM lo maneja

namespace Scripts.Player.Movement.Detectors
{
    /// <summary>
    /// Detects if the player is currently grounded and identifies all ground surfaces beneath.
    /// This component is responsible for updating the PlayerStateManager with the ground status
    /// and the list of colliders the player is currently standing on.
    /// </summary>
    public class PlayerGroundDetector : MonoBehaviour
    {
        [Header("Ground Check Configuration")]
        [Tooltip("Transform representing the origin point for ground detection (usually at player's feet).")]
        [SerializeField] private Transform groundCheckOrigin;
        [Tooltip("Radius of the circle used for the OverlapCircle ground check.")]
        [SerializeField] private float groundCheckRadius = 0.15f;
        [Tooltip("LayerMask defining what layers are considered 'Ground'.")]
        [SerializeField] private LayerMask groundDetectionLayerMask;

        [Header("Debug")]
        [SerializeField] private Color gizmoColor = Color.green;

        private PlayerStateManager _playerStateManager;

        // Internal state for this frame's detection
        private bool _isCurrentlyGrounded;
        private List<Collider2D> _detectedGroundCollidersThisFrame = new List<Collider2D>();
        // ELIMINADO: private bool _isOnOneWayPlatformThisFrame;

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();
            if (_playerStateManager == null)
            {
                Debug.LogError("PlayerGroundDetector: PlayerStateManager not found on this GameObject or its parents! Ground detection state cannot be updated.", this);
                enabled = false;
                return;
            }

            if (groundCheckOrigin == null)
            {
                Debug.LogError("PlayerGroundDetector: 'Ground Check Origin' transform is not assigned. Ground detection will not work.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (_playerStateManager == null || groundCheckOrigin == null) return;

            DetectGroundAndPlatforms();

            // Actualizar el PlayerStateManager con los resultados de la detección
            _playerStateManager.UpdateGroundedState(_isCurrentlyGrounded, _detectedGroundCollidersThisFrame);
            //Debug.Log($"GROUND_DETECTOR: Updating StateManager.IsGrounded to {_isCurrentlyGrounded}, Colliders found: {_detectedGroundCollidersThisFrame.Count}");
        }

        private void DetectGroundAndPlatforms()
        {
            _detectedGroundCollidersThisFrame.Clear(); // Limpiar la lista de la detección anterior
            // ELIMINADO: _isOnOneWayPlatformThisFrame = false; // Reset

            if (_playerStateManager.IsDroppingFromPlatform || groundCheckOrigin == null)
            {
                _isCurrentlyGrounded = false;
                return;
            }

            Collider2D[] collidersUnderPlayer = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundDetectionLayerMask);
            _isCurrentlyGrounded = collidersUnderPlayer.Length > 0;

            if (_isCurrentlyGrounded)
            {
                foreach (Collider2D hitCollider in collidersUnderPlayer)
                {
                    _detectedGroundCollidersThisFrame.Add(hitCollider);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (groundCheckOrigin != null)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(groundCheckOrigin.position, groundCheckRadius);
            }
        }
#endif
    }
}