using UnityEngine;
using System.Collections;
using System.Linq;
using Scripts.Core.Interfaces;
using Scripts.Enemies.Movement;
using Scripts.Enemies.Visuals;

namespace Scripts.Enemies.Core
{
    /// <summary>
    /// Manages an enemy's health, damage reception, invulnerability, and death sequence.
    /// Implements IDamageable and IInstakillable.
    /// </summary>
    [RequireComponent(typeof(EnemyAIController))]
    public class EnemyHealth : MonoBehaviour, IDamageable, IInstakillable
    {
        [Header("Health & Death")]
        [Tooltip("The maximum health of the enemy.")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("If true, the GameObject is destroyed after the death sequence. If false, it's deactivated (for object pooling).")]
        [SerializeField] private bool destroyOnDeath = true;
        [Tooltip("Delay in seconds after death animation starts before the object is destroyed/deactivated.")]
        [SerializeField] private float deathEffectDelay = 2.0f;
        [Tooltip("For special cases (like a window enemy), keeps the object visible in a 'defeated' state instead of disappearing.")]
        [SerializeField] private bool remainVisibleAsDefeated = false;
        
        [Header("Damage Feedback")]
        [Tooltip("Duration of invulnerability after taking a hit.")]
        [SerializeField] private float hitInvulnerabilityDuration = 0.5f;
        
        [Header("Vulnerability Window (Optional)")]
        [Tooltip("If true, the enemy can only take damage when its Animator is in one of the specified states.")]
        [SerializeField] private bool vulnerableOnlyInSpecificStates = false;
        [Tooltip("Names of the Animator states where this enemy is vulnerable.")]
        [SerializeField] private string[] vulnerableAnimationStates = { "Open", "Attack" };
        
        [Header("Required Components (Auto-found)")]
        [SerializeField] private EnemyAIController aiController;
        [SerializeField] private EnemyVisualController visualController;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D mainCollider;

        private float _currentHealth;
        private bool _isInvulnerable = false;

        public bool IsDead { get; private set; } = false;

        private void Awake()
        {
            _currentHealth = maxHealth;

            // Find required components on the root object
            aiController = GetComponent<EnemyAIController>();
            visualController = GetComponentInChildren<EnemyVisualController>(true);
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<Collider2D>();

            // Validations
            if (!aiController) Debug.LogError($"EH on {name}: EnemyAIController is missing!", this);
            if (!visualController) Debug.LogWarning($"EH on {name}: EnemyVisualController is missing. Animations and feedback might fail.", this);
            if (!rb) Debug.LogWarning($"EH on {name}: Rigidbody2D is missing.", this);
            if (!mainCollider) Debug.LogWarning($"EH on {name}: Main Collider2D is missing.", this);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || _isInvulnerable) return;

            // Check for animation state vulnerability if required
            if (vulnerableOnlyInSpecificStates)
            {
                AnimatorStateInfo stateInfo = visualController.GetBodyAnimator().GetCurrentAnimatorStateInfo(0);
                bool isInVulnerableState = false;
                foreach (string stateName in vulnerableAnimationStates)
                {
                    if (stateInfo.IsName(stateName))
                    {
                        isInVulnerableState = true;
                        break;
                    }
                }
                if (!isInVulnerableState)
                {
                    // Optionally, play a "blocked" or "invulnerable" sound/VFX here
                    return;
                }
            }

            _currentHealth -= amount;
            
            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Trigger hit feedback (invulnerability, visual flash)
                StartCoroutine(HitFeedbackSequence());
            }
        }
        
        public void ApplyInstakill()
        {
            if (IsDead) return;
            _currentHealth = 0;
            Die();
        }

        private IEnumerator HitFeedbackSequence()
        {
            _isInvulnerable = true;
            // The visual controller is responsible for the actual flashing effect
            visualController?.StartHitFlash(hitInvulnerabilityDuration);
            
            // Stun the enemy briefly
            if (aiController) aiController.SetCanAct(false);

            yield return new WaitForSeconds(hitInvulnerabilityDuration);

            if (!IsDead)
            {
                if (aiController) aiController.SetCanAct(true);
                _isInvulnerable = false;
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            // Stop all other behaviors
            if (aiController) aiController.enabled = false;
            var attackComponents = GetComponentsInChildren<MonoBehaviour>().OfType<IEnemyAttack>();
            foreach(var attack in attackComponents) { ((MonoBehaviour)attack).enabled = false; }
            var moveComponent = GetComponentInChildren<EnemyMovementComponent>();
            if(moveComponent) moveComponent.enabled = false;

            // Handle physics
            if (mainCollider) mainCollider.enabled = false;
            if (rb)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic; 
            }
            
            // Trigger death animation via the visual controller
            visualController?.TriggerDeathAnimation();
            
            // Handle final disposal of the GameObject
            if (remainVisibleAsDefeated)
            {
                // The object stays, but is effectively inert.
                enabled = false; // Disable this health script.
            }
            else
            {
                StartCoroutine(DestroyAfterDelay(deathEffectDelay));
            }
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false); // For object pooling
            }
        }
    }
    
    // A simple marker interface for attack components to make them easy to find and disable on death.
    public interface IEnemyAttack {}
}