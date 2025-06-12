using UnityEngine;
using System.Collections;
using Scripts.Core.Pooling;
using Scripts.Enemies.Core;

namespace Scripts.Enemies.Ranged
{
    // Define the enum here so EnemyStats can access it.
    public enum AimingStyle { Horizontal, TowardsTarget, FixedDirections }

    public class EnemyAttackRanged : MonoBehaviour, IEnemyAttack, IPooledObject, IEnemyStatReceiver
    {
        [Header("Ranged Attack Setup")]
        [Tooltip("Prefab of the projectile to be fired. Must have an EnemyProjectile component.")]
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("Transform from which the projectile is spawned.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Number of projectiles to fire in a single burst.")]
        [SerializeField] private int projectilesPerBurst = 1;
        [Tooltip("Time delay between each shot within a burst.")]
        [SerializeField] private float delayBetweenBurstShots = 0.1f;
        
        [Header("Line of Sight")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask lineOfSightBlockers;

        // --- Injected & Cached Data ---
        private EnemyStats _stats;
        private EnemyAIController _aiController;
        
        // --- State ---
        private float _lastAttackTime;
        private bool _isCurrentlyAttacking;
        private Transform _currentTarget;

        private void Awake()
        {
            _aiController = GetComponentInParent<EnemyAIController>();
            if (projectilePrefab == null) Debug.LogError($"EAR on {name}: Projectile Prefab is missing!", this);
            if (firePoint == null) Debug.LogError($"EAR on {name}: Fire Point is missing!", this);
        }

        public void Configure(EnemyStats stats)
        {
            this._stats = stats;
        }
        
        public void OnObjectSpawn()
        {
            _isCurrentlyAttacking = false;
            _lastAttackTime = -999f;
            _currentTarget = null;
        }

        public bool CanInitiateAttack(Transform target)
        {
            if (_stats == null || _isCurrentlyAttacking || Time.time < _lastAttackTime + _stats.attackCooldown)
            {
                return false;
            }
            
            if (requireLineOfSight && target != null)
            {
                Vector2 directionToTarget = target.position - firePoint.position;
                if (Physics2D.Raycast(firePoint.position, directionToTarget.normalized, directionToTarget.magnitude, lineOfSightBlockers))
                {
                    return false; // Blocked
                }
            }
            return true;
        }

        public void TryAttack(Transform target)
        {
            if (!CanInitiateAttack(target)) return;
        
            _isCurrentlyAttacking = true;
            _lastAttackTime = Time.time;
            _currentTarget = target;
            _aiController.SetCanAct(false);
        }
        
        // Called by an animation event via the AI Controller
        public void PerformAttackAction()
        {
            if (_currentTarget == null || _aiController.IsDead) return;
            StartCoroutine(FireBurstRoutine(_currentTarget));
        }

        // Called by an animation event via the AI Controller
        public void OnAttackFinished()
        {
            if (!_isCurrentlyAttacking) return;
            
            _isCurrentlyAttacking = false;
            _currentTarget = null;
            if (!_aiController.IsDead)
            {
                _aiController.SetCanAct(true);
            }
        }

        private IEnumerator FireBurstRoutine(Transform target)
        {
            for (int i = 0; i < projectilesPerBurst; i++)
            {
                if (_aiController.IsDead) yield break;
                
                Vector2 fireDirection = GetFireDirection(target);
                
                GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                if (projectileGO.TryGetComponent<EnemyProjectile>(out var projectile))
                {
                    // --- THE FIX ---
                    // Pass the damage from our stats asset to the projectile.
                    projectile.Initialize(fireDirection, _stats.attackDamage);
                }

                if (i < projectilesPerBurst - 1)
                {
                    yield return new WaitForSeconds(delayBetweenBurstShots);
                }
            }
        }
        
        private Vector2 GetFireDirection(Transform target)
        {
            if (_stats == null) return _aiController.IsFacingRight ? Vector2.right : Vector2.left;
            
            switch(_stats.aimingStyle)
            {
                case AimingStyle.TowardsTarget:
                    if (target == null) return _aiController.IsFacingRight ? Vector2.right : Vector2.left;
                    return (target.position - firePoint.position).normalized;
                
                // Note: The FixedDirections logic might need to be more advanced depending on your needs.
                // This version just fires down.
                case AimingStyle.FixedDirections:
                    return transform.root.TransformDirection(Vector2.down); 
                    
                case AimingStyle.Horizontal:
                default:
                    return _aiController.IsFacingRight ? Vector2.right : Vector2.left;
            }
        }
    }
}