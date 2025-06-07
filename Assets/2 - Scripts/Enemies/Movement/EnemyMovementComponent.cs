using UnityEngine;
using Scripts.Enemies.Core;
using Scripts.Enemies.Movement.SteeringBehaviors;

namespace Scripts.Enemies.Movement
{
    /// <summary>
    /// The "legs" of the enemy. This component handles the physical movement of the Rigidbody2D
    /// based on a provided steering behavior. It is also responsible for detecting its immediate
    /// environment (ground, walls, edges) to inform the steering behaviors.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovementComponent : MonoBehaviour
    {
        //[Header("Movement Settings")]
        //[Tooltip("How sharply the enemy can turn. Higher values mean faster turning.")]
        //[SerializeField] private float turnSpeed = 10f;

        [Header("Environment Detection")]
        [Tooltip("LayerMask for what is considered solid ground for checks.")]
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("Offset from the pivot to check for the ground below.")]
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -0.5f);
        [Tooltip("How far down to check for ground.")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [Tooltip("Offset from the pivot to check for an edge in front.")]
        [SerializeField] private Vector2 edgeCheckOffset = new Vector2(0.5f, -0.5f);
        [Tooltip("How far down to check for ground from the edge point.")]
        [SerializeField] private float edgeCheckDistance = 1f;
        [Tooltip("Offset from the pivot to check for a wall in front.")]
        [SerializeField] private Vector2 wallCheckOffset = new Vector2(0.5f, 0);
        [Tooltip("How far forward to check for a wall.")]
        [SerializeField] private float wallCheckDistance = 0.1f;
        
        // --- Public State Properties ---
        public bool IsGrounded { get; private set; }
        public bool IsNearEdge { get; private set; }
        public bool IsNearWall { get; private set; }

        private Rigidbody2D _rb;
        private EnemyAIController _aiController;
        private ISteeringBehavior _activeSteeringBehavior;

        private void Awake()
        {
            _rb = GetComponentInParent<Rigidbody2D>();
            _aiController = GetComponentInParent<EnemyAIController>();
            if (!_rb) { Debug.LogError($"EMC on {name}: Rigidbody2D not found on parent!", this); enabled = false; }
            if (!_aiController) { Debug.LogError($"EMC on {name}: EnemyAIController not found on parent!", this); enabled = false; }
        }

        public void SetSteeringBehavior(ISteeringBehavior newBehavior)
        {
            _activeSteeringBehavior = newBehavior;
        }

        private void FixedUpdate()
        {
            if (!_aiController || _aiController.IsDead || !_aiController.CanAct)
            {
                // If dead or stunned, ensure no residual velocity.
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
                return;
            }

            // Update environment detection based on facing direction.
            UpdateEnvironmentDetection();

            // Get steering output from the active behavior.
            SteeringOutput steering = _activeSteeringBehavior?.GetSteering(this) ?? SteeringOutput.Zero;

            // Apply steering to rigidbody.
            _rb.linearVelocity = new Vector2(steering.DesiredVelocity.x, _rb.linearVelocity.y);

            // Orient the enemy if the behavior requests it.
            if (steering is { ShouldOrient: true, DesiredVelocity: { sqrMagnitude: > 0.01f } })
            {
                _aiController.FaceTarget(transform.position + (Vector3)steering.DesiredVelocity);
            }
        }

        private void UpdateEnvironmentDetection()
        {
            float facingSign = _aiController.IsFacingRight ? 1f : -1f;
            Vector2 position = transform.position;

            IsGrounded = PerformRaycastCheck(position + groundCheckOffset, Vector2.down, groundCheckDistance, groundLayer);
            
            Vector2 wallCheckPos = position + new Vector2(wallCheckOffset.x * facingSign, wallCheckOffset.y);
            IsNearWall = PerformRaycastCheck(wallCheckPos, Vector2.right * facingSign, wallCheckDistance, groundLayer);
            
            Vector2 edgeCheckPos = position + new Vector2(edgeCheckOffset.x * facingSign, edgeCheckOffset.y);
            IsNearEdge = !PerformRaycastCheck(edgeCheckPos, Vector2.down, edgeCheckDistance, groundLayer);
        }
        
        private bool PerformRaycastCheck(Vector2 origin, Vector2 direction, float distance, LayerMask mask)
        {
            return Physics2D.Raycast(origin, direction, distance, mask).collider;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_aiController) _aiController = GetComponentInParent<EnemyAIController>();
            float facingSign = (Application.isPlaying && _aiController) ? (_aiController.IsFacingRight ? 1f : -1f) : 1f;
            Vector2 pos = transform.position;
            
            // Ground Check
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos + groundCheckOffset, pos + groundCheckOffset + Vector2.down * groundCheckDistance);
            
            // Wall Check
            Gizmos.color = Color.blue;
            Vector2 wallOrigin = pos + new Vector2(wallCheckOffset.x * facingSign, wallCheckOffset.y);
            Gizmos.DrawLine(wallOrigin, wallOrigin + (Vector2.right * facingSign) * wallCheckDistance);
            
            // Edge Check
            Gizmos.color = Color.yellow;
            Vector2 edgeOrigin = pos + new Vector2(edgeCheckOffset.x * facingSign, edgeCheckOffset.y);
            Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * edgeCheckDistance);
        }
#endif
    }
}
