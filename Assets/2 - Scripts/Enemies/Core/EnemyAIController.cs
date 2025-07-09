using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Scripts.Core;
using Scripts.Core.Pooling;
using Scripts.Enemies.Attacks;
using Scripts.Enemies.Movement;
using Scripts.Enemies.Movement.SteeringBehaviors.Implementations;
using Scripts.Enemies.Ranged;
using Scripts.Enemies.Visuals;

namespace Scripts.Enemies.Core
{
    // A marker interface for any component that needs to be configured with stats.
    public interface IEnemyStatReceiver { void Configure(EnemyStats stats); }

    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
    public class EnemyAIController : MonoBehaviour, IPooledObject, IEnemyStatReceiver
    {
        [Header("Patrol Settings (Mobile Only)")]
        [Tooltip("Time to wait at each end of a patrol route.")]
        [SerializeField] private float patrolWaitTime = 2f;
        [Tooltip("Time to move along a patrol route before turning (if no wall/edge is hit).")]
        [SerializeField] private float patrolMoveTime = 3f;

        // --- Component References (found in Awake) ---
        private EnemyMovementComponent _movementComponent;
        private IEnemyAttack _attacker;
        private EnemyVisualController _visualController;
        private List<IEnemyStatReceiver> _statReceivers;

        // --- Steering Behaviors ---
        private StayStillBehavior _stayStillBehavior;
        private PatrolBehavior _patrolBehavior;
        private ChaseBehavior _chaseBehavior;
        
        // --- Injected & Cached Data ---
        private EnemyStats _stats;
        private Transform _playerTarget;
        private Rigidbody2D _rb;

        // --- State ---
        private bool _isPlayerDetected = false;
        public bool CanAct { get; private set; } = true;
        public bool IsFacingRight { get; private set; } = true;
        public bool IsDead { get; private set; } = false;

        private void Awake()
        {
            // Find all components this controller needs to manage
            _visualController = GetComponentInChildren<EnemyVisualController>();
            _attacker = GetComponentInChildren<IEnemyAttack>();
            _movementComponent = GetComponentInChildren<EnemyMovementComponent>();
            _statReceivers = GetComponentsInChildren<IEnemyStatReceiver>(true).ToList();
            _rb = GetComponent<Rigidbody2D>();
            
            _stayStillBehavior = new StayStillBehavior();
            
            if(_movementComponent == null) Debug.LogError($"EnemyAIController on {name} requires an EnemyMovementComponent!", this);
        }

        // Called by the Spawner to inject the stats asset and configure all child components
        public void Configure(EnemyStats stats)
        {
            this._stats = stats;
            foreach (var receiver in _statReceivers)
            {
                // The 'as' cast is safe; if the receiver is not a MonoBehaviour, it will be null.
                if ((receiver as MonoBehaviour) != this)
                {
                    receiver.Configure(stats);
                }
            }
        }

        // Called by the Object Pooler to reset the enemy to a pristine state
        public void OnObjectSpawn()
        {
            Debug.Log("EnemyAIController: OnObjectSpawn called for " + name);
            
            IsDead = false;
            enabled = true;
            SetCanAct(true);

            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
            }

            if (_playerTarget == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
                if (playerGO != null) _playerTarget = playerGO.transform;
            }

            if (_stats == null)
            {
                Debug.LogError($"Enemy '{name}' spawned without stats configured!", this);
                return;
            }

            // Configure movement based on stats
            if (!_stats.isStatic && _movementComponent != null)
            {
                Debug.Log("EnemyAIController: Configuring movement component for " + name);
                _movementComponent.enabled = true;
                _patrolBehavior = new PatrolBehavior(_stats.moveSpeed, patrolWaitTime, patrolMoveTime);
                _chaseBehavior = new ChaseBehavior(_stats.moveSpeed, _playerTarget, _stats.engagementRange);
            }
            else if (_movementComponent != null)
            {
                _movementComponent.enabled = false;
            }
        }
        
        // Called by EnemyHealth when the enemy dies
        public void NotifyOfDeath()
        {
            if (IsDead) return; // Prevent this from running multiple times
            IsDead = true;

            // Stop the AI brain.
            enabled = false;
            
            // Stop all physical movement.
            if (_movementComponent) _movementComponent.enabled = false;
            
            // Tell the visual controller to play the death animation.
            _visualController?.TriggerDeathAnimation();
            
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero; // Stop any existing movement instantly.
                _rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private void Update()
        {
            if (_playerTarget == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
                if (playerGO != null) _playerTarget = playerGO.transform;
                
                // If we still can't find one, don't do anything else this frame.
                if (_playerTarget == null) return;
            }
            
            if (IsDead || !CanAct || _playerTarget == null || _stats == null)
            {
                if (_movementComponent && !_stats.isStatic) _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                return;
            }
            
            DetectPlayer();

            if (_stats.isStatic)
            {
                HandleStaticLogic();
            }
            else
            {
                HandleMobileLogic();
            }
        }

        private void DetectPlayer()
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _playerTarget.position);
            if (distanceToPlayer > _stats.detectionRange)
            {
                _isPlayerDetected = false;
                return;
            }

            // Static enemies can have special detection cones
            if (_stats.isStatic)
            {
                Vector2 dirToPlayer = (_playerTarget.position - transform.position).normalized;
                // Assuming cone direction is part of the visual controller's transform, not here.
                // This part may need adjustment based on your window enemy's specific setup.
                // For now, we'll use a simple distance check.
                _isPlayerDetected = true;
            }
            else
            {
                _isPlayerDetected = true;
            }
        }

        private void HandleMobileLogic()
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _playerTarget.position);

            if (_isPlayerDetected)
            {
                if (distanceToPlayer <= _stats.engagementRange)
                {
                    _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                    FaceTarget(_playerTarget.position);
                    TryAttacking();
                }
                else
                {
                    _movementComponent.SetSteeringBehavior(_chaseBehavior);
                }
            }
            else
            {
                if (_stats.canPatrol) _movementComponent.SetSteeringBehavior(_patrolBehavior);
                else _movementComponent.SetSteeringBehavior(_stayStillBehavior);
            }
        }

        private void HandleStaticLogic()
        {
            _visualController?.SetWindowPlayerDetected(_isPlayerDetected);
            
            if (_isPlayerDetected)
            {
                TryAttacking();
            }
        }
        
        /// <summary>
        /// A command from an external source (like a BossController) to immediately
        /// stop all actions, logic, and animations.
        /// </summary>
        public void ForceFreeze()
        {
            Debug.Log($"Freezing minion: {gameObject.name}");
            
            // 1. Stop the AI logic by disabling the 'CanAct' flag.
            SetCanAct(false);

            // 2. Stop the physical movement by disabling the movement component.
            if (_movementComponent != null)
            {
                _movementComponent.enabled = false;
            }

            // 3. Stop the visual animation by calling the new method on the visual controller.
            _visualController?.ForceIdleState();
        }

        private void TryAttacking()
        {
            if (_attacker != null && _attacker.CanInitiateAttack(_playerTarget))
            {
                _attacker.TryAttack(_playerTarget);
                if (_stats.isStatic) _visualController?.TriggerWindowAttack();
                else _visualController?.TriggerMeleeAttack(); // Assuming mobile enemies trigger this way
            }
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            if (_stats.isStatic) return;

            if (targetPosition.x > transform.position.x && !IsFacingRight) Flip();
            else if (targetPosition.x < transform.position.x && IsFacingRight) Flip();
        }

        private void Flip()
        {
            IsFacingRight = !IsFacingRight;
            _visualController?.FlipVisuals(IsFacingRight);
        }

        public void SetCanAct(bool canAct) => CanAct = canAct;
        
        // --- Animation Event Routing ---
        public void HandleAnimationAttackAction()
        {
            // The attacker component handles its own specific action (hitbox vs projectile).
             if (_attacker is EnemyAttackMelee melee) melee.PerformAttackAction();
             if (_attacker is EnemyAttackRanged ranged) ranged.PerformAttackAction();
        }

        public void HandleAnimationAttackFinished()
        {
            if (_attacker is EnemyAttackMelee melee) melee.OnAttackFinished();
            if (_attacker is EnemyAttackRanged ranged) ranged.OnAttackFinished();
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// This method is called by the Unity Editor to draw debug visuals in the Scene view.
        /// It will only draw when the GameObject this script is attached to is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // We need to get the stats to know the ranges. If the game isn't running,
            // we might not have them yet, so we add a null check.
            if (_stats == null)
            {
                // This is a common trick: if the stats are null (because the game isn't playing),
                // try to find the component that would hold the stats asset. This isn't perfect,
                // but can help visualize ranges before pressing Play.
                // For a more robust solution, you'd need a custom editor script, but this is great for now.
                return; // Let's keep it simple: only draw if stats are loaded.
            }

            // Store the current position for cleaner code.
            Vector3 currentPosition = transform.position;

            // --- Draw the Detection Range ---
            // This is the larger circle where the enemy first "notices" the player.
            Gizmos.color = Color.yellow; // Yellow is a common color for detection.
            Gizmos.DrawWireSphere(currentPosition, _stats.detectionRange);

            // --- Draw the Engagement Range ---
            // This is the smaller, inner circle where the enemy will stop chasing and start attacking.
            Gizmos.color = Color.red; // Red is a common color for attack ranges.
            Gizmos.DrawWireSphere(currentPosition, _stats.engagementRange);
        }
#endif // UNITY_EDITOR
    }
}