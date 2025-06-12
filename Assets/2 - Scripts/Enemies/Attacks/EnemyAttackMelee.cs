using Scripts.Core.Pooling;
using UnityEngine;
using Scripts.Enemies.Core;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Visuals;

namespace Scripts.Enemies.Attacks
{
    public class EnemyAttackMelee : MonoBehaviour, IEnemyAttack, IPooledObject, IEnemyStatReceiver
    {
        [Header("Component References")]
        [Tooltip("The EnemyMeleeHitbox component that deals the actual damage.")]
        [SerializeField] private EnemyMeleeHitbox meleeHitbox;

        // --- Injected & Cached Data ---
        private EnemyStats _stats;
        private EnemyAIController _aiController;
        private EnemyVisualController _visualController;
        
        // --- State ---
        private float _lastAttackTime;
        private bool _isCurrentlyAttacking;

        private void Awake()
        {
            // Get references from parent/root
            _aiController = GetComponentInParent<EnemyAIController>();
            _visualController = GetComponentInParent<EnemyVisualController>();
            if (meleeHitbox == null) meleeHitbox = GetComponentInChildren<EnemyMeleeHitbox>(true);
            
            // Validations
            if (_aiController == null) Debug.LogError($"EAM on {name}: Missing EnemyAIController!", this);
            if (_visualController == null) Debug.LogError($"EAM on {name}: Missing EnemyVisualController!", this);
            if (meleeHitbox == null) Debug.LogError($"EAM on {name}: Missing EnemyMeleeHitbox!", this);
        }

        // Called by the Spawner/AIController to inject the stats asset
        public void Configure(EnemyStats stats)
        {
            this._stats = stats;
        }

        // Called by the Pooler/AIController to reset state
        public void OnObjectSpawn()
        {
            _isCurrentlyAttacking = false;
            // Set last attack time to a long time ago so the enemy can attack immediately if needed.
            _lastAttackTime = -999f;
        }

        public bool CanInitiateAttack(Transform target)
        {
            if (_stats == null || _isCurrentlyAttacking || Time.time < _lastAttackTime + _stats.attackCooldown)
            {
                return false;
            }
            return true;
        }

        public void TryAttack(Transform target)
        {
            if (!CanInitiateAttack(target)) return;

            _isCurrentlyAttacking = true;
            _lastAttackTime = Time.time;
            _aiController.SetCanAct(false);

            _visualController.TriggerMeleeAttack();
        }

        public void PerformAttackAction()
        {
            if (_stats == null) return;
            meleeHitbox?.Activate(_stats.attackDamage);
        }

        public void OnAttackFinished()
        {
            if (!_isCurrentlyAttacking) return;

            _isCurrentlyAttacking = false;
            meleeHitbox?.Deactivate();
        
            if (!_aiController.IsDead)
            {
                _aiController.SetCanAct(true);
            }
        }
    }
}