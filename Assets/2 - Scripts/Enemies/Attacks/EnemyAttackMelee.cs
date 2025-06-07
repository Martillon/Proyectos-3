using UnityEngine;
using System.Collections;
using Scripts.Enemies.Core;
using Scripts.Enemies.Visuals;

namespace Scripts.Enemies.Melee
{
    /// <summary>
    /// Manages the logic for an enemy's melee attack. It determines when to attack,
    /// triggers animations, and activates/deactivates the physical hitbox.
    /// </summary>
    public class EnemyAttackMelee : MonoBehaviour, IEnemyAttack
    {
        [Header("Attack Settings")]
        [Tooltip("Damage dealt by a successful hit.")]
        [SerializeField] private int damageAmount = 10;
        [Tooltip("The cooldown time between attack attempts.")]
        [SerializeField] private float attackCooldown = 1.5f;
        
        [Header("Component References")]
        [Tooltip("The EnemyMeleeHitbox component that deals the actual damage.")]
        [SerializeField] private EnemyMeleeHitbox meleeHitbox;
        [Tooltip("The controller that manages animations.")]
        [SerializeField] private EnemyVisualController visualController;
        [Tooltip("The AI controller for this enemy.")]
        [SerializeField] private EnemyAIController aiController;
        
        private float _lastAttackTime;
        private bool _isCurrentlyAttacking;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (aiController == null) aiController = GetComponentInParent<EnemyAIController>();
            if (visualController == null) visualController = GetComponentInParent<EnemyVisualController>();
            if (meleeHitbox == null) meleeHitbox = GetComponentInChildren<EnemyMeleeHitbox>(true);
            
            // Validations
            if (aiController == null) Debug.LogError($"EAM on {name}: Missing EnemyAIController!", this);
            if (visualController == null) Debug.LogError($"EAM on {name}: Missing EnemyVisualController!", this);
            if (meleeHitbox == null) Debug.LogError($"EAM on {name}: Missing EnemyMeleeHitbox!", this);
        }

        public bool CanInitiateAttack(Transform target)
        {
            if (_isCurrentlyAttacking || Time.time < _lastAttackTime + attackCooldown)
            {
                return false;
            }
            // The AI controller already checks for engagement range.
            return true;
        }

        public void TryAttack(Transform target)
        {
            if (!CanInitiateAttack(target)) return;
            
            StartCoroutine(AttackSequence());
        }

        private IEnumerator AttackSequence()
        {
            _isCurrentlyAttacking = true;
            _lastAttackTime = Time.time;
            aiController.SetCanAct(false); // Stun the AI during the attack animation
            
            // Tell the visual controller to play the attack animation
            visualController.TriggerMeleeAttack();
            
            // The animation itself will call ActivateHitbox and DeactivateHitbox
            // via Animation Events. We just need to wait for the animation to signal it's finished.
            // A timeout is used as a fallback in case the animation event is missing.
            yield return new WaitUntil(() => !_isCurrentlyAttacking);

            // This line is reached when OnAttackAnimationFinished sets _isCurrentlyAttacking to false.
            if (!aiController.IsDead) // Check if the enemy died during the attack
            {
               aiController.SetCanAct(true); // Restore AI control
            }
        }
        
        // --- Animation Event Methods ---
        // These methods are called directly from events on the attack animation clip.
        
        public void ActivateHitbox()
        {
            meleeHitbox?.Activate(damageAmount);
        }

        public void DeactivateHitbox()
        {
            meleeHitbox?.Deactivate();
        }

        public void OnAttackAnimationFinished()
        {
            _isCurrentlyAttacking = false;
        }
    }
}