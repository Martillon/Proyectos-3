using UnityEngine;
using System.Collections;
using Scripts.Enemies.Core;
using Scripts.Enemies.Visuals;

namespace Scripts.Enemies.Ranged
{
    public enum AimingStyle { Horizontal, TowardsTarget, FixedDirections }

    /// <summary>
    /// Manages logic for an enemy's ranged attack. Handles aiming, cooldowns, and projectile spawning.
    /// </summary>
    public class EnemyAttackRanged : MonoBehaviour, IEnemyAttack
    {
        [Header("Attack Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int projectilesPerBurst = 1;
        [SerializeField] private float delayBetweenBurstShots = 0.1f;

        [Header("Aiming")]
        [SerializeField] private AimingStyle aimingStyle = AimingStyle.Horizontal;
        [Tooltip("For FixedDirections style, define the directions to shoot in (local space).")]
        [SerializeField] private Vector2[] fixedAimDirections = { Vector2.down };
        
        [Header("Line of Sight")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask lineOfSightBlockers;
        
        private EnemyAIController _aiController;
        private float _lastAttackTime;
        private bool _isCurrentlyAttacking;
        private Transform _currentTarget;

        private void Awake()
        {
            _aiController = GetComponentInParent<EnemyAIController>();
            if (!projectilePrefab) Debug.LogError($"EAR on {name}: Projectile Prefab is missing!", this);
            if (!firePoint) Debug.LogError($"EAR on {name}: Fire Point is missing!", this);
        }

        public bool CanInitiateAttack(Transform target)
        {
            if (_isCurrentlyAttacking || Time.time < _lastAttackTime + attackCooldown) return false;
            
            // Check Line of Sight if required
            if (requireLineOfSight)
            {
                Vector2 directionToTarget = target.position - firePoint.position;
                float distance = directionToTarget.magnitude;
                if (Physics2D.Raycast(firePoint.position, directionToTarget, distance, lineOfSightBlockers))
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
        
        public void PerformAttackAction()
        {
            // Spawn the projectile(s).
            StartCoroutine(FireBurst(_currentTarget));
        }

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

        private IEnumerator FireBurst(Transform target)
        {
            for (int i = 0; i < projectilesPerBurst; i++)
            {
                if (_aiController.IsDead) yield break;
                
                Vector2 fireDirection = GetFireDirection(target);
                var projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity).GetComponent<EnemyProjectile>();
                projectile?.Initialize(fireDirection);

                if (i < projectilesPerBurst - 1)
                {
                    yield return new WaitForSeconds(delayBetweenBurstShots);
                }
            }
            _isCurrentlyAttacking = false; // Signal that the burst is complete
        }
        
        private Vector2 GetFireDirection(Transform target)
        {
            switch(aimingStyle)
            {
                case AimingStyle.TowardsTarget:
                    return (target.position - firePoint.position).normalized;
                
                case AimingStyle.FixedDirections:
                    return GetBestFixedDirection(target.position);
                    
                case AimingStyle.Horizontal:
                default:
                    return _aiController.IsFacingRight ? Vector2.right : Vector2.left;
            }
        }
        
        private Vector2 GetBestFixedDirection(Vector3 targetPosition)
        {
            if (fixedAimDirections == null || fixedAimDirections.Length == 0) return Vector2.down;

            Vector2 bestDir = fixedAimDirections[0];
            float smallestAngleDiff = float.MaxValue;
            Vector2 dirToTarget = ((Vector2)targetPosition - (Vector2)firePoint.position).normalized;
            
            foreach (var fixedDir in fixedAimDirections)
            {
                Vector2 worldFixedDir = transform.TransformDirection(fixedDir);
                float angleDiff = Vector2.Angle(worldFixedDir, dirToTarget);
                if (angleDiff < smallestAngleDiff)
                {
                    smallestAngleDiff = angleDiff;
                    bestDir = worldFixedDir;
                }
            }
            return bestDir.normalized;
        }
    }
}