// --- START OF FILE EnemyAttackMelee.cs ---

using System.Collections;
using UnityEngine;
using Scripts.Core.Interfaces; // For IDamageable

namespace Scripts.Enemies
{
    /// <summary>
    /// Handles melee attack logic for an enemy.
    /// Checks proximity to the target, manages attack cooldown, and applies damage.
    /// Can pause the enemy's movement during an attack animation via EnemyAIController.
    /// </summary>
    [RequireComponent(typeof(EnemyAIController))] // Relies on AIController for context
    public class EnemyAttackMelee : MonoBehaviour
    {
        [Header("Melee Attack Settings")]
        [Tooltip("Range within which the enemy can execute a melee attack.")]
        [SerializeField] private float attackRange = 1.5f;
        [Tooltip("Cooldown time (in seconds) between melee attacks.")]
        [SerializeField] private float attackCooldown = 1.2f;
        [Tooltip("Damage inflicted by a single melee attack.")]
        [SerializeField] private int damageAmount = 1;
        [Tooltip("Duration (in seconds) the enemy pauses movement to perform the attack animation.")]
        [SerializeField] private float attackAnimationDuration = 0.5f; // Time enemy stops moving

        // [Header("Optional Feedback")]
        // [SerializeField] private Animator enemyAnimator; // If attack animation is triggered here
        // [SerializeField] private string attackAnimationTrigger = "MeleeAttack";
        // [SerializeField] private Sounds attackSFX;
        // [SerializeField] private AudioSource audioSourceForSFX;

        private float lastAttackTimestamp;
        private EnemyAIController aiController;
        private bool isCurrentlyAttacking = false;

        private void Awake()
        {
            aiController = GetComponent<EnemyAIController>();
            // if (enemyAnimator == null) enemyAnimator = GetComponent<Animator>(); // Or GetComponentInChildren
            // if (audioSourceForSFX == null) audioSourceForSFX = GetComponent<AudioSource>();
            
            if (aiController == null)
            {
                Debug.LogError($"EnemyAttackMelee on '{gameObject.name}' requires an EnemyAIController component.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Attempts to perform a melee attack on the specified target.
        /// Checks for range, cooldown, and if an attack is already in progress.
        /// </summary>
        /// <param name="target">The Transform of the target (typically the player).</param>
        public void TryAttack(Transform target)
        {
            if (target == null || isCurrentlyAttacking || Time.time < lastAttackTimestamp + attackCooldown)
            {
                return;
            }

            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (distanceToTarget <= attackRange)
            {
                StartCoroutine(PerformMeleeAttackSequence(target));
            }
        }

        private IEnumerator PerformMeleeAttackSequence(Transform target)
        {
            isCurrentlyAttacking = true;
            aiController.SetCanMove(false); // Pause AI movement

            // Debug.Log($"Enemy '{gameObject.name}' performing melee attack.", this); // Uncomment for debugging

            // TODO: Trigger attack animation
            // enemyAnimator?.SetTrigger(attackAnimationTrigger);
            // attackSFX?.Play(audioSourceForSFX);

            // Wait for a moment (e.g., halfway through animation) before applying damage
            // This makes the hit feel more impactful and timed with the visual.
            yield return new WaitForSeconds(attackAnimationDuration / 2f); 

            // Double-check range before applying damage, in case target moved out during wind-up
            if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange)
            {
                if (target.TryGetComponent<IDamageable>(out var damageableTarget))
                {
                    damageableTarget.TakeDamage(damageAmount);
                    // Debug.Log($"Enemy '{gameObject.name}' dealt {damageAmount} melee damage to {target.name}.", this); // Uncomment for debugging
                }
            }
            
            // Wait for the remainder of the attack animation
            yield return new WaitForSeconds(attackAnimationDuration / 2f);

            aiController.SetCanMove(true); // Resume AI movement
            lastAttackTimestamp = Time.time; // Set cooldown after the attack sequence finishes
            isCurrentlyAttacking = false;
        }

        /// <summary>
        /// Checks if the target is within the defined melee attack range.
        /// </summary>
        /// <param name="target">The target's Transform.</param>
        /// <returns>True if the target is in range, false otherwise.</returns>
        public bool IsTargetInAttackRange(Transform target)
        {
            if (target == null) return false;
            return Vector2.Distance(transform.position, target.position) <= attackRange;
        }

        /// <summary>
        /// Indicates if the enemy is currently executing its attack sequence.
        /// </summary>
        public bool IsAttacking()
        {
            return isCurrentlyAttacking;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif
    }
}
// --- END OF FILE EnemyAttackMelee.cs ---