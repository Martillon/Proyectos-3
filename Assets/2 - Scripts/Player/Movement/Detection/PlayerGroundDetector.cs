using UnityEngine;
using System.Collections.Generic;
using Scripts.Player.Core;

namespace Scripts.Player.Movement.Detectors
{
    /// <summary>
    /// Detects if the player is on the ground by performing an OverlapCircle check.
    /// It updates the PlayerStateManager with the ground status and a list of all
    /// ground colliders the player is currently standing on.
    /// </summary>
    public class PlayerGroundDetector : MonoBehaviour
    {
        [Header("Ground Check Configuration")]
        [Tooltip("The transform representing the origin point for the ground check (usually at the player's feet).")]
        [SerializeField] private Transform groundCheckOrigin;
        [Tooltip("Radius of the circle used for the OverlapCircle ground check.")]
        [SerializeField] private float groundCheckRadius = 0.2f;
        [Tooltip("LayerMask defining what layers are considered 'Ground' or 'Platform'.")]
        [SerializeField] private LayerMask groundLayerMask;

        [Header("Gizmos")]
        [SerializeField] private Color gizmoColor = Color.green;

        private PlayerStateManager _stateManager;
        // Re-using this list every frame avoids allocating new memory, which is a small but good optimization.
        private readonly List<Collider2D> _detectedCollidersThisFrame = new List<Collider2D>();

        private void Awake()
        {
            _stateManager = GetComponentInParent<PlayerStateManager>();
            if (_stateManager == null)
            {
                Debug.LogError("PlayerGroundDetector: PlayerStateManager not found! Ground detection will not work.", this);
                enabled = false;
                return;
            }
            if (groundCheckOrigin == null)
            {
                Debug.LogError("PlayerGroundDetector: 'Ground Check Origin' is not assigned.", this);
                enabled = false;
            }
        }

        // Using FixedUpdate for physics-related checks can be more reliable.
        private void FixedUpdate()
        {
            if (groundCheckOrigin == null) return;

            DetectGround();
        }

        private void DetectGround()
        {
            // Clear the list from the previous frame.
            _detectedCollidersThisFrame.Clear();

            // If we are intentionally dropping, we are not grounded, regardless of what the overlap check says.
            if (_stateManager.IsDroppingFromPlatform)
            {
                _stateManager.SetGroundedState(false, _detectedCollidersThisFrame);
                return;
            }

            int hitCount = Physics2D.OverlapCircle(groundCheckOrigin.position, groundCheckRadius, new ContactFilter2D { useLayerMask = true, layerMask = groundLayerMask }, _detectedCollidersThisFrame);

            bool isGrounded = hitCount > 0;
            
            //Debug.Log($"Ground Check: {isGrounded}, Detected Colliders: {hitCount}", this);
            
            // Pass the results to the state manager.
            _stateManager.SetGroundedState(isGrounded, _detectedCollidersThisFrame);
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