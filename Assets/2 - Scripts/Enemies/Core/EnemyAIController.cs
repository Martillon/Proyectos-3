using Scripts.Core;
using UnityEngine;
using Scripts.Enemies.Movement;
using Scripts.Enemies.Movement.SteeringBehaviors;
using Scripts.Enemies.Movement.SteeringBehaviors.Implementations;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Ranged;
using Scripts.Enemies.Visuals;

namespace Scripts.Enemies.Core
{
    public enum EnemyBehaviorType { Melee, RangedMobile, RangedStaticWindow }

    /// <summary>
    /// The central "brain" of an enemy. It determines the enemy's state (patrolling, chasing,
    /// attacking) based on player proximity and its behavior type. It then commands other
    /// components (Movement, Attack, Visuals) to execute the desired actions.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
    public class EnemyAIController : MonoBehaviour
    {
        [Header("AI Core Settings")]
        [SerializeField] public EnemyBehaviorType behaviorType = EnemyBehaviorType.Melee;
        [Tooltip("The range at which the enemy will detect the player and react.")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("The range at which the enemy will stop moving towards the player and attempt to attack.")]
        [SerializeField] private float engagementRange = 1.5f;
        [Tooltip("The player's transform. If null, it will be found by tag at runtime.")]
        [SerializeField] private Transform playerTarget;

        [Header("Patrol Behavior (for Mobile Enemies)")]
        [SerializeField] private bool canPatrol = true;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private float patrolMoveTime = 3f;

        [Header("Cone Detection (for Window Enemy)")]
        [Tooltip("For Static Window enemies, enables cone-based detection instead of circular.")]
        [SerializeField] private bool useConeDetection = true;
        [SerializeField] private float coneDetectionAngle = 120f;
        [Tooltip("The forward direction of the detection cone in local space (e.g., (0, -1) for down).")]
        [SerializeField] private Vector2 coneForwardDirection = Vector2.down;

        // --- Component References ---
        private EnemyMovementComponent _movementComponent;
        private EnemyAttackMelee _meleeAttacker;
        private EnemyAttackRanged _rangedAttacker;
        private EnemyVisualController _visualController;
        private EnemyHealth _enemyHealth; // The source of truth for the IsDead state

        // --- Steering Behaviors ---
        private StayStillBehavior _stayStillBehavior;
        private PatrolBehavior _patrolBehavior;
        private ChaseBehavior _chaseBehavior;

        // --- State ---
        private bool _isPlayerDetected = false;
        public bool CanAct { get; private set; } = true;
        public bool IsFacingRight { get; private set; } = true;
        
        // ** CORRECTED PROPERTY: Reads directly from the Health component **
        public bool IsDead => _enemyHealth != null && _enemyHealth.IsDead;

        private void Awake()
        {
            // --- Get all required components ---
            _enemyHealth = GetComponent<EnemyHealth>();
            _movementComponent = GetComponentInChildren<EnemyMovementComponent>();
            _visualController = GetComponentInChildren<EnemyVisualController>();
            _meleeAttacker = GetComponentInChildren<EnemyAttackMelee>();
            _rangedAttacker = GetComponentInChildren<EnemyAttackRanged>();

            // --- Validations ---
            if (_enemyHealth == null) { Debug.LogError($"AI on {name}: EnemyHealth component is missing! This is required.", this); enabled = false; return; }

            if (playerTarget == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
                if (playerGO != null) playerTarget = playerGO.transform;
            }

            // --- Initialize Steering Behaviors for mobile enemies ---
            if (behaviorType != EnemyBehaviorType.RangedStaticWindow)
            {
                if (_movementComponent == null) { Debug.LogError($"AI on {name}: Mobile enemy is missing EnemyMovementComponent!", this); enabled = false; return; }

                _stayStillBehavior = new StayStillBehavior();
                _patrolBehavior = new PatrolBehavior(moveSpeed, patrolWaitTime, patrolMoveTime);
                _chaseBehavior = new ChaseBehavior(moveSpeed, playerTarget, engagementRange);
            }
        }

        private void Update()
        {
            // ** CRITICAL CHECK **: If dead, do nothing. This stops all AI logic immediately.
            if (IsDead)
            {
                // Ensure movement stops if it hasn't already.
                if (behaviorType != EnemyBehaviorType.RangedStaticWindow && _movementComponent != null && _movementComponent.enabled)
                {
                    _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                }
                return;
            }
            
            // If stunned or no player, also halt actions.
            if (!CanAct || playerTarget == null)
            {
                if (behaviorType != EnemyBehaviorType.RangedStaticWindow && _movementComponent != null)
                {
                    _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                }
                // For window enemy, being unable to act should probably close it.
                if(behaviorType == EnemyBehaviorType.RangedStaticWindow)
                {
                     _visualController?.SetWindowPlayerDetected(false);
                }
                return;
            }

            DetectPlayer();

            switch (behaviorType)
            {
                case EnemyBehaviorType.Melee:
                case EnemyBehaviorType.RangedMobile:
                    HandleMobileLogic();
                    break;
                case EnemyBehaviorType.RangedStaticWindow:
                    HandleWindowLogic();
                    break;
            }
        }

        private void HandleMobileLogic()
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

            if (_isPlayerDetected)
            {
                // Within engagement range, try to attack.
                if (distanceToPlayer <= engagementRange)
                {
                    _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                    FaceTarget(playerTarget.position);
                    TryAttacking();
                }
                else // Detected but not in range, chase.
                {
                    _movementComponent.SetSteeringBehavior(_chaseBehavior);
                }
            }
            else // Not detected, patrol or stand still.
            {
                if (canPatrol)
                {
                    _movementComponent.SetSteeringBehavior(_patrolBehavior);
                }
                else
                {
                    _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                }
            }
        }

        private void HandleWindowLogic()
        {
            if (_visualController == null || _rangedAttacker == null) return;
            
            // Tell the visual controller if the player is detected. The Animator will handle opening/closing.
            _visualController.SetWindowPlayerDetected(_isPlayerDetected);
            
            // If the window is in a state where it can attack, try to attack.
            if (_isPlayerDetected && _rangedAttacker.CanInitiateAttack(playerTarget))
            {
                // The AI decides to attack, but the visual controller triggers the animation
                // which then calls the attack script via an animation event.
                _visualController.TriggerWindowAttack();
                // We still call TryAttack here, as it may handle cooldowns or other logic.
                _rangedAttacker.TryAttack(playerTarget);
            }
        }
        
        private void TryAttacking()
        {
            if (_meleeAttacker != null && _meleeAttacker.CanInitiateAttack(playerTarget))
            {
                _meleeAttacker.TryAttack(playerTarget);
            }
            else if (_rangedAttacker != null && _rangedAttacker.CanInitiateAttack(playerTarget))
            {
                _rangedAttacker.TryAttack(playerTarget);
            }
        }

        private void DetectPlayer()
        {
            if (playerTarget == null)
            {
                _isPlayerDetected = false;
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer > detectionRange)
            {
                _isPlayerDetected = false;
                return;
            }

            // If in range, perform cone check if applicable
            if (behaviorType == EnemyBehaviorType.RangedStaticWindow && useConeDetection)
            {
                Vector2 directionToPlayer = (playerTarget.position - transform.position).normalized;
                Vector2 worldConeForward = transform.TransformDirection(coneForwardDirection);
                float angleToPlayer = Vector2.Angle(worldConeForward, directionToPlayer);
                _isPlayerDetected = angleToPlayer <= coneDetectionAngle / 2f;
            }
            else
            {
                // For circular detection, being within detectionRange is enough.
                _isPlayerDetected = true;
            }
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            if (behaviorType == EnemyBehaviorType.RangedStaticWindow) return; // Static enemies don't flip

            if (targetPosition.x > transform.position.x && !IsFacingRight)
            {
                Flip();
            }
            else if (targetPosition.x < transform.position.x && IsFacingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            IsFacingRight = !IsFacingRight;
            _visualController?.FlipVisuals(IsFacingRight);
        }
        
        public void SetCanAct(bool canAct)
        {
            CanAct = canAct;
        }
        
        public void HandleAnimationAttackAction()
        {
            // Route the "action" event (e.g., activate hitbox, spawn projectile)
            // to the appropriate attack script.
            if (behaviorType == EnemyBehaviorType.Melee && _meleeAttacker != null)
            {
                _meleeAttacker.PerformAttackAction();
            }
            else if (behaviorType != EnemyBehaviorType.Melee && _rangedAttacker != null) // RangedMobile or RangedStaticWindow
            {
                _rangedAttacker.PerformAttackAction();
            }
        }

        public void HandleAnimationAttackFinished()
        {
            // Route the "finished" event to the appropriate attack script.
            if (behaviorType == EnemyBehaviorType.Melee && _meleeAttacker != null)
            {
                _meleeAttacker.OnAttackFinished();
            }
            else if (behaviorType != EnemyBehaviorType.Melee && _rangedAttacker != null)
            {
                _rangedAttacker.OnAttackFinished();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, engagementRange);

            if (behaviorType == EnemyBehaviorType.RangedStaticWindow && useConeDetection)
            {
                Gizmos.color = Color.blue;
                Vector3 forward = transform.TransformDirection(coneForwardDirection.normalized);
                if (forward == Vector3.zero) forward = transform.up * -1; // Fallback
                
                Vector3 upRay = Quaternion.AngleAxis(-coneDetectionAngle / 2, Vector3.forward) * forward;
                Vector3 downRay = Quaternion.AngleAxis(coneDetectionAngle / 2, Vector3.forward) * forward;

                Gizmos.DrawRay(transform.position, upRay * detectionRange);
                Gizmos.DrawRay(transform.position, downRay * detectionRange);
                Gizmos.DrawLine(transform.position + upRay * detectionRange, transform.position + downRay * detectionRange);
            }
        }
#endif
    }
}