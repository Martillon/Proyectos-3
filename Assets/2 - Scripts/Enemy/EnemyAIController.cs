// --- START OF FILE EnemyAIController.cs ---
using UnityEngine;
using System.Collections;
using Scripts.Enemies.Core;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Ranged; // For Coroutines if needed for attack patterns

namespace Scripts.Enemies
{
    /// <summary>
    /// Main controller for enemy AI behavior.
    /// Manages references to movement, attack, and health components.
    /// Handles player detection and delegates actions based on enemy type and state.
    /// Requires a Rigidbody2D for physics-based movement and interactions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyHealth))] // Enemies should always have health
    public class EnemyAIController : MonoBehaviour
    {
        public enum EnemyBehaviorType { Melee, Ranged /*, Patrol, Guard, etc. */ }

        [Header("AI Core Settings")]
        [Tooltip("Defines the general behavior pattern of this enemy.")]
        [SerializeField] private EnemyBehaviorType behaviorType = EnemyBehaviorType.Melee;
        [Tooltip("Range within which the enemy detects the player.")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("Range within which the enemy will stop approaching and start attacking (if applicable).")]
        [SerializeField] private float engagementRange = 1.5f; // Could be same as attackRange for melee
        [Tooltip("Movement speed of the enemy.")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Tooltip("Transform of the player. If null, will try to find GameObject with 'Player' tag.")]
        [SerializeField] private Transform playerTarget;

        [Header("Ground & Edge Detection (Whiskers)")]
        [Tooltip("Distance downwards from whisker origins to check for ground.")]
        [SerializeField] private float groundCheckDistance = 0.6f;
        [Tooltip("Horizontal offset from center for the 'front' whisker used for edge detection.")]
        [SerializeField] private float edgeWhiskerOffset = 0.5f;
        [Tooltip("Layer mask defining what is considered ground or an edge.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Physics & Collision")]
        [Tooltip("Optional: Physics material to reduce friction or bounciness.")]
        [SerializeField] private PhysicsMaterial2D physicsMaterial;
        [Tooltip("Mass of the enemy. Higher mass makes it harder for the player to push.")]
        [SerializeField] private float enemyMass = 5f; // Increased default mass

        // Component References
        private Rigidbody2D rb;
        private EnemyHealth enemyHealth;
        private EnemyAttackMelee meleeAttacker; // Specific attack components
        private EnemyAttackRanged rangedAttacker;

        // State
        private bool isPlayerDetected = false;
        private bool isFacingRight = true;
        private bool canMove = true; // To pause movement during attacks, etc.
        private Vector2 currentMovementDirection = Vector2.zero;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            enemyHealth = GetComponent<EnemyHealth>(); // Should always exist due to RequireComponent

            meleeAttacker = GetComponent<EnemyAttackMelee>();
            rangedAttacker = GetComponent<EnemyAttackRanged>();

            if (playerTarget == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTarget = playerObject.transform;
                }
                // else Debug.LogWarning($"EnemyAIController on '{gameObject.name}': Player target not found.", this); // Uncomment for debugging
            }

            if (rb != null)
            {
                rb.freezeRotation = true; // Essential for 2D
                rb.interpolation = RigidbodyInterpolation2D.Interpolate; // For smoother movement
                if (physicsMaterial != null) rb.sharedMaterial = physicsMaterial;
                rb.mass = enemyMass;
            }
        }

        private void FixedUpdate()
        {
            if (enemyHealth.IsDead || playerTarget == null)
            {
                if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop horizontal movement if dead or no target
                return;
            }

            HandleDetection();
            
            if (isPlayerDetected)
            {
                FaceTarget();
                HandleMovement();
                HandleAttack();
            }
            else
            {
                // TODO: Implement idle or patrol behavior if player is not detected
                if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop horizontal movement
            }
        }

        private void HandleDetection()
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            isPlayerDetected = (distanceToPlayer <= detectionRange);
        }

        private void FaceTarget()
        {
            if (playerTarget.position.x > transform.position.x && !isFacingRight)
            {
                Flip();
            }
            else if (playerTarget.position.x < transform.position.x && isFacingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        private void HandleMovement()
        {
            if (!canMove)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop horizontal if can't move
                return;
            }

            bool isGrounded = IsNearGround();
            bool isNearEdge = IsNearEdge();

            currentMovementDirection = Vector2.zero;
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

            if (distanceToPlayer > engagementRange) // If outside engagement range, try to move closer
            {
                currentMovementDirection.x = (playerTarget.position.x > transform.position.x) ? 1 : -1;
            }
            // Else (within engagement range), typically stop moving horizontally to attack.
            // This might be overridden by specific attack behaviors (e.g., a charging melee enemy).

            if (isGrounded && !isNearEdge && currentMovementDirection.x != 0)
            {
                rb.linearVelocity = new Vector2(currentMovementDirection.x * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                // If not grounded, near an edge, or decided not to move, stop horizontal velocity.
                // Gravity will still apply.
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }

        private bool IsNearGround()
        {
            // A simple downward raycast from the center, or multiple for stability
            Vector2 origin = (Vector2)transform.position - new Vector2(0, GetComponent<Collider2D>().bounds.extents.y - 0.1f); // Origin slightly above feet
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
            // Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red); // Uncomment for debugging
            return hit.collider != null;
        }

        private bool IsNearEdge()
        {
            // "Whisker" pointing down in front of the enemy based on facing direction
            float whiskerDirection = isFacingRight ? 1f : -1f;
            Vector2 frontWhiskerOrigin = (Vector2)transform.position + 
                                         new Vector2(edgeWhiskerOffset * whiskerDirection, -GetComponent<Collider2D>().bounds.extents.y + 0.1f);
            
            RaycastHit2D hit = Physics2D.Raycast(frontWhiskerOrigin, Vector2.down, groundCheckDistance * 1.5f, groundLayer); // Check a bit further for edges
            // Debug.DrawRay(frontWhiskerOrigin, Vector2.down * (groundCheckDistance * 1.5f), hit.collider != null ? Color.blue : Color.yellow); // Uncomment for debugging
            return hit.collider == null; // True if NO ground is detected by the front whisker (i.e., near an edge)
        }


        private void HandleAttack()
        {
            // Stop movement while attacking, can be re-enabled by attack script if needed (e.g. lunge)
            // bool isCurrentlyAttacking = false; // This would be set by the attack components

            switch (behaviorType)
            {
                case EnemyBehaviorType.Melee:
                    if (meleeAttacker != null)
                    {
                        // Melee attacker might set 'canMove = false' during its attack animation
                        meleeAttacker.TryAttack(playerTarget); 
                        // isCurrentlyAttacking = meleeAttacker.IsAttacking(); // If attack script has such a state
                    }
                    break;
                case EnemyBehaviorType.Ranged:
                    if (rangedAttacker != null)
                    {
                        // Ranged attacker might also set 'canMove = false'
                        rangedAttacker.TryAttack(playerTarget);
                        // isCurrentlyAttacking = rangedAttacker.IsAttacking(); // If attack script has such a state
                    }
                    break;
            }
            // if (isCurrentlyAttacking) canMove = false; else canMove = true; // Example
        }
        
        /// <summary>
        /// Public method to allow attack scripts to temporarily halt movement.
        /// </summary>
        public void SetCanMove(bool canEnemyMove)
        {
            this.canMove = canEnemyMove;
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Detection Range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Engagement Range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, engagementRange);

            if (Application.isPlaying) // Only draw whiskers if playing and components are available
            {
                 // Ground Check Gizmo (approximated, as actual check is more complex)
                if(GetComponent<Collider2D>() != null)
                {
                    Vector2 groundOrigin = (Vector2)transform.position - new Vector2(0, GetComponent<Collider2D>().bounds.extents.y - 0.1f);
                    Gizmos.color = IsNearGround() ? Color.green : Color.magenta;
                    Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDistance);

                    // Edge Whisker Gizmo
                    float whiskerDir = isFacingRight ? 1f : -1f;
                    Vector2 edgeOrigin = (Vector2)transform.position + 
                                         new Vector2(edgeWhiskerOffset * whiskerDir, -GetComponent<Collider2D>().bounds.extents.y + 0.1f);
                    Gizmos.color = IsNearEdge() ? Color.yellow : Color.blue; // Yellow if edge, blue if ground
                    Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * (groundCheckDistance * 1.5f));
                }
            }
            else // Editor-time approximation for whiskers
            {
                if(GetComponent<Collider2D>() != null)
                {
                    Vector2 groundOrigin = (Vector2)transform.position - new Vector2(0, GetComponent<Collider2D>().bounds.extents.y - 0.1f);
                    Gizmos.color = Color.gray;
                    Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDistance);
                    Vector2 edgeOriginRight = (Vector2)transform.position + 
                                         new Vector2(edgeWhiskerOffset, -GetComponent<Collider2D>().bounds.extents.y + 0.1f);
                    Gizmos.DrawLine(edgeOriginRight, edgeOriginRight + Vector2.down * (groundCheckDistance * 1.5f));
                    Vector2 edgeOriginLeft = (Vector2)transform.position + 
                                         new Vector2(-edgeWhiskerOffset, -GetComponent<Collider2D>().bounds.extents.y + 0.1f);
                    Gizmos.DrawLine(edgeOriginLeft, edgeOriginLeft + Vector2.down * (groundCheckDistance * 1.5f));
                }
            }
        }
#endif
    }
}
// --- END OF FILE EnemyAIController.cs ---