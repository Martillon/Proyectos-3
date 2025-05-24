// --- START OF FILE EnemyAttackMelee.cs ---
using UnityEngine;
using System.Collections;
using Scripts.Enemies.Core;

// No longer needs Scripts.Core.Interfaces directly if hitbox handles damage

namespace Scripts.Enemies.Melee
{
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
            aiController = GetComponentInParent<EnemyAIController>();
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
            if (meleeHitbox == null) // Si no está asignado en el inspector
            {
                // Buscar en el Root y sus hijos
                Transform rootTransform = transform.parent; // Asumiendo que Enemy_Visuals es hijo de EnemyMelee_root
                if (rootTransform != null)
                {
                    meleeHitbox = rootTransform.GetComponentInChildren<EnemyMeleeHitbox>(true);
                }

                if (meleeHitbox == null)
                {
                    Debug.LogError($"EnemyAttackMelee on '{gameObject.name}': MeleeHitbox could not be found. Attack will not function.", this);
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
            aiController.SetCanMove(false); 
            enemyAnimator.SetTrigger(attackAnimationTriggerName); // Asegúrate que el nombre coincida con el Animator
            //Debug.Log($"[{Time.frameCount}] MeleeAttacker: Coroutine STARTED. Trigger '{attackAnimationTriggerName}' fired. isCurrentlyInAttackSequence = true.");

            // Esperar a que FinishAttackSequence sea llamado por el evento de animación,
            // lo que pondrá isCurrentlyInAttackSequence a false.
            // Añadimos un timeout para evitar un bucle infinito si el evento nunca se llama.
            float animationTimeout = 10f; // Tiempo máximo de espera razonable para una animación de ataque
            float timer = 0f;

            while (isCurrentlyInAttackSequence && timer < animationTimeout)
            {
                timer += Time.deltaTime;
                yield return null; // Esperar al siguiente frame
            }

            // Este bloque ahora solo se ejecuta si el while loop terminó debido al TIMEOUT,
            // lo que significa que el evento de animación OnAttackAnimationFinished probablemente no se llamó.
            if (isCurrentlyInAttackSequence) 
            {
                Debug.LogWarning($"[{Time.frameCount}] MeleeAttacker: Attack sequence for '{gameObject.name}' TIMED OUT after {animationTimeout}s. Forcing FinishAttackSequence. Check Animation Events for 'OnAttackAnimationFinished'.");
                if (meleeHitbox != null && meleeHitbox.gameObject.activeSelf)
                {
                    Debug.LogWarning($"[{Time.frameCount}] MeleeAttacker: Hitbox was still active on timeout. Deactivating as a fallback.");
                    DeactivateHitbox(); // Asegurarse de que la hitbox se desactive
                }
                FinishAttackSequence(); // Llamar a FinishAttackSequence como último recurso
            }
            else
            {
                //Debug.Log($"[{Time.frameCount}] MeleeAttacker: Coroutine ENDED because isCurrentlyInAttackSequence became false (likely via Animation Event).");
            }
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
            if (!isCurrentlyInAttackSequence) return; 

            //Debug.Log($"[{Time.frameCount}] MeleeAttacker: FinishAttackSequence CALLED. Setting isCurrentlyInAttackSequence to false, CanMove to true.");
            aiController.SetCanMove(true); 
            lastAttackEndTime = Time.time; 
            isCurrentlyInAttackSequence = false;
        }
        // --- END OF ANIMATION EVENT METHODS ---

        public bool IsTargetInAttackInitiationRange(Transform target)
        {
            if (target == null) return false;
            return Vector2.Distance(transform.position, target.position) <= attackInitiationRange;
        }

        public bool IsAttacking() => isCurrentlyInAttackSequence;
        
        public bool CanInitiateAttack(Transform target)
        {
            if (target == null || isCurrentlyInAttackSequence || Time.time < lastAttackEndTime + attackCooldown)
            {
                return false;
            }
            return IsTargetInAttackInitiationRange(target);
        }

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
                    // Matrix math needed for rotated/offset box. For simplicity, show a sphere at its position.
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