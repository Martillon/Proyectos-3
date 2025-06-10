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

            _isCurrentlyAttacking = true;
            _lastAttackTime = Time.time;
            aiController.SetCanAct(false); // Tell AI to stop moving/thinking.

            // Tell the VisualController to start the animation.
            visualController.TriggerMeleeAttack();
        
            // The coroutine is no longer needed. We now wait for the OnAttackFinished event.
        }
        
        /// <summary>
        /// Called by the AIController when the animation hits its action frame.
        /// </summary>
        public void PerformAttackAction()
        {
            // This is where the old "ActivateHitbox" logic goes.
            // We can just keep it simple and activate it directly.
            meleeHitbox?.Activate(damageAmount);

            // Deactivation should also be driven by an animation event if possible,
            // or by a short timer/coroutine started here if the swing is simple.
            // For now, let's assume another event or the end of the attack handles it.
        }

        /// <summary>
        /// Called by the AIController when the animation signals it has completed.
        /// </summary>
        public void OnAttackFinished()
        {
            if (!_isCurrentlyAttacking) return; // Prevent extra calls

            _isCurrentlyAttacking = false;
            meleeHitbox?.Deactivate(); // Ensure hitbox is always off at the end.
        
            // Check if the enemy is still alive before re-enabling its actions.
            if (!aiController.IsDead)
            {
                aiController.SetCanAct(true);
            }
        }
    }
}