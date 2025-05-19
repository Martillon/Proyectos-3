using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core; // For GameConstants and PlayerStateManager

namespace Scripts.Player.Movement.Detectors
{
    /// <summary>
    /// Detects if the player is currently grounded and if the surface beneath is a one-way platform.
    /// This component is responsible for updating the PlayerStateManager with the ground status.
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
        [Tooltip("Tag used to identify one-way platforms that the player can drop through.")]
        [SerializeField] private string oneWayPlatformTag = GameConstants.PlatformTag;

        [Header("Debug")]
        [SerializeField] private Color gizmoColor = Color.green;

        private PlayerStateManager _playerStateManager; // Referencia al StateManager

        // Internal state for this frame's detection
        private bool _isCurrentlyGrounded;
        private bool _isOnPlatformThisFrame;
        private Collider2D _detectedPlatformColliderThisFrame;

        void Awake()
        {
            // Obtener la referencia al PlayerStateManager, que debería estar en este GameObject o en un padre.
            _playerStateManager = GetComponentInParent<PlayerStateManager>();
            if (_playerStateManager == null)
            {
                Debug.LogError("PlayerGroundDetector: PlayerStateManager not found on this GameObject or its parents! Ground detection state cannot be updated.", this);
                enabled = false; // Deshabilitar este script si falta la dependencia crítica
                return;
            }

            if (groundCheckOrigin == null)
            {
                Debug.LogError("PlayerGroundDetector: 'Ground Check Origin' transform is not assigned. Ground detection will not work.", this);
                enabled = false;
            }
        }

        // Es común hacer las detecciones físicas o que dependen de otros estados (como IsDropping)
        // en Update o LateUpdate para asegurar que los estados base estén actualizados para el frame actual.
        // Si PlayerPlatformHandler actualiza IsDropping en PlayerStateManager en su Update,
        // este Update debería ejecutarse después o leer el estado del frame anterior.
        // Para simplificar, asumimos que el orden de Update de los scripts es manejable o
        // que el PlayerStateManager.IsDroppingFromPlatform se actualiza antes de este Update.
        // O, si el orden es un problema, se podría hacer en LateUpdate.
        void Update() 
        {
            if (_playerStateManager == null || groundCheckOrigin == null) return;

            DetectGroundAndPlatform();
            
            // Actualizar el PlayerStateManager con los resultados de la detección
            _playerStateManager.UpdateGroundedState(_isCurrentlyGrounded, _isOnPlatformThisFrame, _detectedPlatformColliderThisFrame);
            //Debug.Log($"GROUND_DETECTOR: Updating StateManager.IsGrounded to {_isCurrentlyGrounded}");
        }

        private void DetectGroundAndPlatform()
        {
            // Si el jugador está en el proceso de 'dropping' a través de una plataforma,
            // no se considera 'grounded' para propósitos de lógica de salto o aterrizaje.
            if (_playerStateManager.IsDroppingFromPlatform)
            {
                _isCurrentlyGrounded = false;
                _isOnPlatformThisFrame = false;
                return;
            }

            //bool previousGrounded = _isCurrentlyGrounded; // For debugging state changes

            _isCurrentlyGrounded = Physics2D.OverlapCircle(groundCheckOrigin.position, groundCheckRadius, groundDetectionLayerMask);
            _detectedPlatformColliderThisFrame = null; // Reset

            if (_isCurrentlyGrounded)
            {
                Collider2D[] collidersUnderPlayer = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundDetectionLayerMask);
                foreach (Collider2D hitCollider in collidersUnderPlayer)
                {
                    if (hitCollider.CompareTag(oneWayPlatformTag) && hitCollider.GetComponent<PlatformEffector2D>() != null)
                    {
                        _isOnPlatformThisFrame = true;
                        _detectedPlatformColliderThisFrame = hitCollider; // Guardar el collider
                        break; 
                    }
                }
            }

            //if (_isCurrentlyGrounded != previousGrounded)
            //{
            //    Debug.Log($"PlayerGroundDetector: Grounded state changed to {_isCurrentlyGrounded}. OnPlatform: {_isOnPlatformThisFrame}. PosY: {transform.position.y}");
            //}
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
// --- END OF FILE PlayerGroundDetector.cs ---
