// --- START OF FILE EnemyAttackRanged.cs ---
using UnityEngine;
using System.Collections; // For Coroutines

namespace Scripts.Enemies
{
    /// <summary>
    /// Handles ranged attack logic for an enemy.
    /// Spawns a projectile towards the target if within range and line of sight (optional).
    /// Manages attack cooldown and can pause movement during firing.
    /// </summary>
    [RequireComponent(typeof(EnemyAIController))]
    public class EnemyAttackRanged : MonoBehaviour
    {
        [Header("Ranged Attack Settings")]
        [Tooltip("Prefab of the projectile to be fired. Must have an EnemyProjectile component.")]
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("Transform from which the projectile is spawned. Its Y position should be fixed for 'agacharse-esquivar' mechanic.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Range within which the enemy will attempt to fire.")]
        [SerializeField] private float attackRange = 8f;
        [Tooltip("Cooldown time (in seconds) between ranged attacks.")]
        [SerializeField] private float attackCooldown = 2f;
        [Tooltip("Duration (in seconds) the enemy might pause movement to fire (e.g., for an attack animation). Set to 0 if enemy can fire while moving.")]
        [SerializeField] private float fireAnimationDuration = 0.3f;

        [Header("Line of Sight (Optional)")]
        [Tooltip("If true, enemy will only fire if there's a clear line of sight to the player (ignoring other enemies, projectiles).")]
        [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("Layers that can block line of sight (e.g., 'Ground', 'Obstacles'). Player layer should NOT be in this mask.")]
        [SerializeField] private LayerMask lineOfSightBlockers; 
        
        // [Header("Optional Feedback")]
        // [SerializeField] private Animator enemyAnimator;
        // [SerializeField] private string attackAnimationTrigger = "RangedAttack";
        // [SerializeField] private Sounds attackSFX;
        // [SerializeField] private AudioSource audioSourceForSFX;

        private float lastAttackTimestamp;
        private EnemyAIController aiController;
        private bool isCurrentlyFiring = false;

        private void Awake()
        {
            aiController = GetComponent<EnemyAIController>();
            // if (enemyAnimator == null) enemyAnimator = GetComponent<Animator>();
            // if (audioSourceForSFX == null) audioSourceForSFX = GetComponent<AudioSource>();

            if (aiController == null)
            {
                Debug.LogError($"EnemyAttackRanged on '{gameObject.name}' requires an EnemyAIController component.", this);
                enabled = false;
            }
            if (projectilePrefab == null)
            {
                Debug.LogError($"EnemyAttackRanged on '{gameObject.name}': Projectile Prefab is not assigned.", this);
                enabled = false;
            }
            if (projectilePrefab != null && projectilePrefab.GetComponent<EnemyProjectile>() == null)
            {
                Debug.LogError($"EnemyAttackRanged on '{gameObject.name}': Assigned Projectile Prefab '{projectilePrefab.name}' is missing an EnemyProjectile component.", this);
                enabled = false;
            }
            if (firePoint == null)
            {
                Debug.LogError($"EnemyAttackRanged on '{gameObject.name}': Fire Point transform is not assigned.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Attempts to perform a ranged attack on the specified target.
        /// Checks for range, cooldown, line of sight (if enabled), and if an attack is already in progress.
        /// </summary>
        /// <param name="target">The Transform of the target (typically the player).</param>
        public void TryAttack(Transform target)
        {
            if (target == null || isCurrentlyFiring || Time.time < lastAttackTimestamp + attackCooldown || firePoint == null)
            {
                return;
            }

            float distanceToTarget = Vector2.Distance(firePoint.position, target.position); // Use firePoint for distance to target
            if (distanceToTarget <= attackRange)
            {
                if (requireLineOfSight && !HasLineOfSight(target))
                {
                    // Debug.Log($"Enemy '{gameObject.name}': Ranged attack aborted, no line of sight to {target.name}.", this); // Uncomment for debugging
                    return;
                }
                StartCoroutine(PerformRangedAttackSequence(target));
            }
        }

        private bool HasLineOfSight(Transform target)
        {
            if (target == null || firePoint == null) return false;

            Vector2 directionToTarget = (target.position - firePoint.position).normalized;
            float distanceToTarget = Vector2.Distance(firePoint.position, target.position);

            // Raycast from firePoint to target. If it hits something in lineOfSightBlockers first, then LoS is blocked.
            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToTarget, distanceToTarget, lineOfSightBlockers);
            
            // If hit is null, it means nothing in 'lineOfSightBlockers' was hit, so LoS is clear.
            // If hit is not null, LoS is blocked.
            return hit.collider == null;
        }

        private IEnumerator PerformRangedAttackSequence(Transform target)
        {
            isCurrentlyFiring = true;
            if (fireAnimationDuration > 0)
            {
                aiController.SetCanMove(false); // Pause AI movement during firing animation
            }

            // Debug.Log($"Enemy '{gameObject.name}' performing ranged attack.", this); // Uncomment for debugging

            // TODO: Trigger attack animation (e.g., aiming or firing animation)
            // enemyAnimator?.SetTrigger(attackAnimationTrigger);
            // attackSFX?.Play(audioSourceForSFX);

            // Wait for a portion of the animation if there's a "wind-up"
            if (fireAnimationDuration > 0)
            {
                yield return new WaitForSeconds(fireAnimationDuration * 0.5f); // Example: fire halfway through animation
            }

            if (projectilePrefab != null && firePoint != null && target != null) // Double check refs
            {
                Vector2 directionToTarget = (target.position - firePoint.position).normalized;
                
                // Ensure firePoint is oriented towards the target if enemy sprite itself doesn't rotate fully
                // (EnemyAIController handles general facing, this is for fine-tuning firePoint if needed)
                // float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                // firePoint.rotation = Quaternion.Euler(0, 0, angle); // This might conflict with EnemyAIController's Flip if firePoint is child

                GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation); // Use firePoint's rotation
                EnemyProjectile projectileScript = projGO.GetComponent<EnemyProjectile>();
                if (projectileScript != null)
                {
                    projectileScript.Initialize(directionToTarget);
                    // Damage is usually set on the EnemyProjectile prefab or can be passed if variable
                    // projectileScript.SetDamage(projectileDamage); 
                }
            }

            // Wait for the remainder of the firing animation (if any)
            if (fireAnimationDuration > 0)
            {
                yield return new WaitForSeconds(fireAnimationDuration * 0.5f);
                aiController.SetCanMove(true); // Resume AI movement
            }
            
            lastAttackTimestamp = Time.time;
            isCurrentlyFiring = false;
        }

        public bool IsTargetInAttackRange(Transform target)
        {
            if (target == null || firePoint == null) return false;
            return Vector2.Distance(firePoint.position, target.position) <= attackRange;
        }

        public bool IsFiring()
        {
            return isCurrentlyFiring;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(firePoint.position, attackRange);
            }
        }
#endif
    }
}
// --- END OF FILE EnemyAttackRanged.cs ---