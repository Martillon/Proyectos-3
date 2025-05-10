// --- START OF FILE EnemyAttackMelee.cs ---
using UnityEngine;
using System.Collections;

// No longer needs Scripts.Core.Interfaces directly if hitbox handles damage

namespace Scripts.Enemies.Melee
{
    [RequireComponent(typeof(EnemyAIController))]
    [RequireComponent(typeof(Animator))] // Now requires an Animator
    public class EnemyAttackMelee : MonoBehaviour
    {
        [Header("Melee Attack Settings")]
        [Tooltip("Range within which the enemy will initiate its melee attack sequence.")]
        [SerializeField] private float attackInitiationRange = 1.7f; // Slightly larger than hitbox maybe
        [Tooltip("Cooldown time (in seconds) between the end of one attack sequence and the start of another.")]
        [SerializeField] private float attackCooldown = 1.2f;
        [Tooltip("Damage inflicted by the melee attack (passed to the hitbox).")]
        [SerializeField] private int damageAmount = 1;
        
        [Header("Animation & Hitbox")]
        [Tooltip("Reference to the GameObject containing the EnemyMeleeHitbox script and its trigger collider.")]
        [SerializeField] private EnemyMeleeHitbox meleeHitbox;
        [Tooltip("Name of the attack animation trigger parameter in the Animator.")]
        [SerializeField] private string attackAnimationTriggerName = "MeleeAttackTrigger"; // e.g., "Attack", "Swing"

        // [Header("Optional Feedback")]
        // [SerializeField] private Sounds attackWindupSFX; // Sound for starting the attack
        // [SerializeField] private AudioSource audioSourceForSFX;

        private float lastAttackEndTime;
        private EnemyAIController aiController;
        private Animator enemyAnimator;
        private bool isCurrentlyInAttackSequence = false;

        private void Awake()
        {
            aiController = GetComponent<EnemyAIController>();
            enemyAnimator = GetComponent<Animator>();
            // if (audioSourceForSFX == null) audioSourceForSFX = GetComponent<AudioSource>();

            if (aiController == null)
            {
                Debug.LogError($"EnemyAttackMelee on '{gameObject.name}' requires an EnemyAIController.", this);
                enabled = false; return;
            }
            if (enemyAnimator == null)
            {
                Debug.LogError($"EnemyAttackMelee on '{gameObject.name}' requires an Animator component.", this);
                enabled = false; return;
            }
            if (meleeHitbox == null)
            {
                Debug.LogError($"EnemyAttackMelee on '{gameObject.name}': MeleeHitbox reference is not assigned.", this);
                // Try to find it as a child if not assigned
                meleeHitbox = GetComponentInChildren<EnemyMeleeHitbox>(true); // true to include inactive
                if (meleeHitbox == null)
                {
                    Debug.LogError($"EnemyAttackMelee on '{gameObject.name}': MeleeHitbox could not be found as a child. Attack will not function.", this);
                    enabled = false; return;
                }
            }
            // Ensure hitbox is initially inactive (though its Awake should handle this)
            meleeHitbox.gameObject.SetActive(false);
        }

        /// <summary>
        /// Attempts to initiate a melee attack if the target is in range and cooldown allows.
        /// </summary>
        public void TryAttack(Transform target)
        {
            if (target == null || isCurrentlyInAttackSequence || Time.time < lastAttackEndTime + attackCooldown)
            {
                return;
            }

            if (IsTargetInAttackInitiationRange(target))
            {
                StartCoroutine(MeleeAttackSequenceCoroutine());
            }
        }

        private IEnumerator MeleeAttackSequenceCoroutine()
        {
            isCurrentlyInAttackSequence = true;
            aiController.SetCanMove(false); // Stop AI movement

            // Debug.Log($"Enemy '{gameObject.name}' starting melee attack sequence.", this); // Uncomment for debugging
            // attackWindupSFX?.Play(audioSourceForSFX);
            enemyAnimator.SetTrigger(attackAnimationTriggerName); // Trigger the attack animation

            // The animation itself will call ActivateHitbox() and DeactivateHitbox() via Animation Events.
            // We need to wait for the animation to logically complete or for a signal.
            // For simplicity, we can wait for the animator to exit the attack state,
            // or use a fixed duration if the animation state is hard to track.
            // A robust way is to have an Animation Event at the END of the attack animation
            // that calls a method like "AttackSequenceFinished()".

            // Simple wait based on an assumption or a new field for total attack duration:
            // yield return new WaitForSeconds(enemyAnimator.GetCurrentAnimatorStateInfo(0).length); // This can be tricky
            // For now, let's assume the animation events handle hitbox, and we just manage cooldown timing.
            // The duration of AI pause can be tied to animation length.
            // We'll set 'isCurrentlyInAttackSequence = false' and 'lastAttackEndTime'
            // via an animation event or after a determined duration.
            // Let's use a placeholder for total animation cycle time before cooldown starts.
            // A better way: Animation event calls "OnAttackAnimationFinished".
            float animationLength = GetAnimationClipLength(attackAnimationTriggerName);
            if(animationLength > 0) {
                yield return new WaitForSeconds(animationLength);
            } else {
                Debug.LogWarning($"EnemyAttackMelee on {gameObject.name}: Could not get length for animation triggered by '{attackAnimationTriggerName}'. Using default wait.", this);
                yield return new WaitForSeconds(1f); // Default wait if animation length not found
            }
            
            // Ensure hitbox is off if animation event somehow missed
            if (meleeHitbox.gameObject.activeSelf)
            {
                // Debug.LogWarning($"EnemyAttackMelee '{gameObject.name}': Hitbox was still active after attack sequence. Deactivating.", this); // Uncomment for debugging
                DeactivateHitbox(); // Called by animation, but as a fallback
            }

            FinishAttackSequence();
        }
        
        // Helper to estimate animation clip length (can be unreliable if state names don't match trigger names)
        private float GetAnimationClipLength(string triggerName)
        {
            RuntimeAnimatorController ac = enemyAnimator.runtimeAnimatorController;    
            for (int i = 0; i<ac.animationClips.Length; i++)
            {
                // This is a simplification. Trigger names don't always map directly to clip names or state names.
                // A more robust way is to check the current state after triggering or have specific state names.
                if (ac.animationClips[i].name.Contains(triggerName) || ac.animationClips[i].name.Contains("Attack")) // Be flexible with naming
                {
                    return ac.animationClips[i].length;
                }
            }
            return 0; // Not found
        }


        // --- PUBLIC METHODS TO BE CALLED BY ANIMATION EVENTS ---
        /// <summary>
        /// Activates the melee hitbox. Intended to be called by an Animation Event.
        /// </summary>
        public void ActivateHitbox()
        {
            if (meleeHitbox != null)
            {
                meleeHitbox.Activate(damageAmount);
            }
        }

        /// <summary>
        /// Deactivates the melee hitbox. Intended to be called by an Animation Event.
        /// </summary>
        public void DeactivateHitbox()
        {
            if (meleeHitbox != null)
            {
                meleeHitbox.Deactivate();
            }
        }
        
        /// <summary>
        /// Marks the end of the attack sequence. Can be called by an Animation Event at the end of the attack animation.
        /// </summary>
        public void OnAttackAnimationFinished()
        {
            FinishAttackSequence();
        }

        private void FinishAttackSequence()
        {
            if (!isCurrentlyInAttackSequence) return; // Avoid multiple calls

            // Debug.Log($"Enemy '{gameObject.name}' finished melee attack sequence.", this); // Uncomment for debugging
            aiController.SetCanMove(true); // Resume AI movement
            lastAttackEndTime = Time.time; // Start cooldown now
            isCurrentlyInAttackSequence = false;
        }
        // --- END OF ANIMATION EVENT METHODS ---

        public bool IsTargetInAttackInitiationRange(Transform target)
        {
            if (target == null) return false;
            return Vector2.Distance(transform.position, target.position) <= attackInitiationRange;
        }

        public bool IsAttacking() => isCurrentlyInAttackSequence;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange for initiation range
            Gizmos.DrawWireSphere(transform.position, attackInitiationRange);

            // Draw the hitbox area if assigned and in editor
            if (meleeHitbox != null && meleeHitbox.GetComponent<Collider2D>() != null)
            {
                Collider2D hitboxCollider = meleeHitbox.GetComponent<Collider2D>();
                Gizmos.color = Color.magenta;
                // This is a simplified draw. For accurate BoxCollider2D, you'd use Gizmos.matrix.
                if(hitboxCollider is BoxCollider2D box)
                {
                    // Matrix math needed for rotated/offset box. For simplicity, just show a sphere at its position.
                    Gizmos.DrawWireSphere(meleeHitbox.transform.position, box.size.x / 2f); 
                } else if (hitboxCollider is CapsuleCollider2D capsule) {
                    Gizmos.DrawWireSphere(meleeHitbox.transform.position, capsule.size.x / 2f);
                } else if (hitboxCollider is CircleCollider2D circle) {
                     Gizmos.DrawWireSphere(meleeHitbox.transform.position, circle.radius);
                }
            }
        }
#endif
    }
}
// --- END OF FILE EnemyAttackMelee.cs ---