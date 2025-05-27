// En Scripts/Enemies/Core (o similar)
using UnityEngine;
using Scripts.Enemies.Movement; // Para EnemyMovementComponent
using Scripts.Enemies.Movement.SteeringBehaviors; // Para ISteeringBehavior2D
using Scripts.Enemies.Movement.SteeringBehaviors.Implementations; // Para las clases concretas
using Scripts.Enemies.Core;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Ranged;
using Scripts.Enemies.Visuals; // Para EnemyHealth (si no está en el mismo namespace)
// Añade using para tus scripts de ataque Melee/Ranged

namespace Scripts.Enemies.Core // Ajusta el namespace
{
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAIController : MonoBehaviour
    {
        public enum EnemyBehaviorType { Melee, RangedMobile, RangedStaticWindow }

        [Header("AI Core Settings")]
        [SerializeField] public EnemyBehaviorType behaviorType = EnemyBehaviorType.Melee; // Público para que otros lo lean si es necesario
        [Tooltip("Range within which the enemy detects the player.")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("Range within which the enemy will stop approaching and start attacking (if applicable).")]
        [SerializeField] private float engagementRange = 1.5f;
        [Tooltip("Transform of the player. If null, will try to find GameObject with 'Player' tag.")]
        [SerializeField] public Transform playerTarget;  // Público para que behaviors puedan accederlo si es necesario (vía aiController.playerTarget)

        [Header("Mobile Enemy Settings")]
        [Tooltip("Aplicable si el behaviorType NO es RangedStaticWindow.")]
        [SerializeField] private bool canPatrol = true;
        [Tooltip("Velocidad base para movimiento (patrulla, persecución).")]
        [SerializeField] private float moveSpeed = 2.5f; 

        [Header("Patrol Behavior Config (For Mobile)")]
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private float patrolMoveTime = 3f;

        [Header("Window Enemy Animator (Required if RangedStaticWindow)")]
        [Tooltip("El Animator que controla las animaciones de la ventana (abrir, cerrar, apuntar, atacar).")]
        [SerializeField] private Animator windowEnemyAnimator; 
        [Tooltip("Nombre del parámetro Bool en el Animator de ventana para 'isOpen'.")]
        [SerializeField]
        public string animParamIsOpen = "isOpen";
        [Tooltip("Nombre del parámetro Float para 'aimDirectionX'.")]
        [SerializeField]
        public string animParamAimDirectionX = "aimDirectionX";
        [Tooltip("Nombre del parámetro Trigger para iniciar el ataque desde el estado de apuntar.")]
        [SerializeField]
        public string animTriggerActualAttack = "Attack"; 

        [Header("Cone Detection (Primarily for Window Enemy)")]
        [Tooltip("Usar detección en cono si isStaticWindowEnemy es true.")]
        [SerializeField] private bool useConeDetectionForWindow = true; 
        [SerializeField] private float coneDetectionAngle = 120f;
        [Tooltip("Dirección central del cono en espacio LOCAL del enemigo. (0, -1) es abajo.")]
        [SerializeField] private Vector2 coneForwardDirectionLocal = Vector2.down;

        [Header("Visuals & Animation Control")]
        [Tooltip("El Transform del GameObject 'Visuals' que será escalado para flipear (móviles) y que DEBE tener EnemyVisualController.")]
        [SerializeField] public Transform visualsContainerTransform; // Ya lo tenías
        private EnemyVisualController _visualController;

        // Component References
        private EnemyMovementComponent _movementComponent;
        private EnemyHealth _enemyHealth;
        private EnemyAttackMelee _meleeAttacker;
        private EnemyAttackRanged _rangedAttacker;

        // Steering Behaviors
        private StayStillBehavior2D _stayStillBehavior;
        private PatrolBehavior2D _patrolBehavior;
        private ChaseBehavior2D _chaseBehavior;

        // State
        private bool _isPlayerDetected = false;
        public bool IsFacingRight { get; private set; } = true;
        public bool CanMove { get; private set; } = true;
        private bool _currentWindowOpenState = false;

        // Hashes
        private int _animHashIsOpen;
        private int _animHashAimDirectionX;
        private int _animHashTriggerActualAttack;

        public bool IsDead => _enemyHealth != null && _enemyHealth.IsDeadPublic; // Usar propiedad pública de EnemyHealth
        public bool isStaticWindowEnemy => behaviorType == EnemyBehaviorType.RangedStaticWindow;

        private void Awake()
        {
            _enemyHealth = GetComponent<EnemyHealth>();
            // ... (Obtener playerTarget) ...

            // Obtener VisualController (DEBE estar en visualsContainerTransform o sus hijos)
            if (visualsContainerTransform != null)
            {
                _visualController = visualsContainerTransform.GetComponentInChildren<EnemyVisualController>(true);
            }
            if (_visualController == null) // Fallback si visualsContainerTransform no está asignado pero hay un EVC en los hijos del root
            {
                _visualController = GetComponentInChildren<EnemyVisualController>(true);
            }
            if (_visualController == null && isStaticWindowEnemy) // Crítico para el de ventana
            {
                Debug.LogError($"AIController ({gameObject.name}): EnemyVisualController no encontrado! La lógica de animación de ventana fallará.");
            }


            if (!isStaticWindowEnemy)
            {
                _movementComponent = GetComponentInChildren<EnemyMovementComponent>();
                // ... (validación y creación de _movementComponent si es null) ...
                _patrolBehavior = new PatrolBehavior2D(moveSpeed, patrolWaitTime, patrolMoveTime);
                _chaseBehavior = new ChaseBehavior2D(moveSpeed, playerTarget, engagementRange);
            }
            _stayStillBehavior = new StayStillBehavior2D();


            // Obtener componentes de ataque
            if (behaviorType == EnemyBehaviorType.Melee)
                _meleeAttacker = GetComponent<EnemyAttackMelee>() ?? GetComponentInChildren<EnemyAttackMelee>(true);
            else 
                _rangedAttacker = GetComponent<EnemyAttackRanged>() ?? GetComponentInChildren<EnemyAttackRanged>(true);
            
            // ... (Validaciones de componentes de ataque) ...

            // Ya no inicializamos hashes del Animator aquí, lo hace EnemyVisualController
        }

        private void Update()
        {
            if (IsDead)
            {
                _movementComponent?.SetSteeringBehavior(_stayStillBehavior);
                return;
            }

            if (playerTarget == null) { // Si el jugador es destruido o no encontrado
                _isPlayerDetected = false;
                 if (isStaticWindowEnemy) HandleWindowEnemyLogic(); // Para que se cierre
                 else if (_movementComponent != null) {
                    if (canPatrol && _patrolBehavior != null) _movementComponent.SetSteeringBehavior(_patrolBehavior);
                    else _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                 }
                return;
            }

            if (isStaticWindowEnemy) HandleWindowEnemyLogic();
            else HandleMobileEnemyLogic();
        }

        private void HandleMobileEnemyLogic()
        {
            if (_movementComponent == null) return;

            _patrolBehavior?.UpdatePatrolParameters(moveSpeed, patrolWaitTime, patrolMoveTime);
            if (_chaseBehavior != null) {
                _chaseBehavior.UpdateTarget(playerTarget);
                _chaseBehavior.UpdateChaseSpeed(moveSpeed);
                _chaseBehavior.UpdateStoppingDistance(engagementRange);
            }

            HandleDetection(); // Para enemigos móviles, usa detección circular

            if (!CanMove) { _movementComponent.SetSteeringBehavior(_stayStillBehavior); return; }

            if (_isPlayerDetected)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
                bool canPhysicallyAttack = false;

                if (behaviorType == EnemyBehaviorType.Melee && _meleeAttacker != null)
                    canPhysicallyAttack = _meleeAttacker.CanInitiateAttack(playerTarget);
                else if (behaviorType == EnemyBehaviorType.RangedMobile && _rangedAttacker != null)
                    canPhysicallyAttack = _rangedAttacker.CanInitiateAttack(playerTarget);

                if (distanceToPlayer <= engagementRange && canPhysicallyAttack)
                {
                    _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                    FaceTarget(); 
                    if (behaviorType == EnemyBehaviorType.Melee) _meleeAttacker.TryAttack(playerTarget);
                    else if (behaviorType == EnemyBehaviorType.RangedMobile) _rangedAttacker.TryAttack(playerTarget);
                }
                else if (distanceToPlayer <= engagementRange && !canPhysicallyAttack)
                {
                    _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                    FaceTarget(); 
                }
                else 
                {
                    if (_chaseBehavior != null) _movementComponent.SetSteeringBehavior(_chaseBehavior);
                    else _movementComponent.SetSteeringBehavior(_stayStillBehavior);
                }
            }
            else 
            {
                if (canPatrol && _patrolBehavior != null) _movementComponent.SetSteeringBehavior(_patrolBehavior);
                else _movementComponent.SetSteeringBehavior(_stayStillBehavior);
            }
        }

        private void HandleWindowEnemyLogic()
        {
            if (_visualController == null || _rangedAttacker == null || playerTarget == null) return;
            
            HandleDetection(); // Usa detección en cono

            bool shouldTryToOpen = _isPlayerDetected && CanMove; // Intención inicial de abrir si detecta y no está ya en una secuencia de SetCanMove(false)
            bool isCurrentlyAttackingOrFiring = _rangedAttacker.IsFiring(); // Leer el estado de EnemyAttackRanged

            bool desiredWindowOpenState;

            if (isCurrentlyAttackingOrFiring)
            {
                desiredWindowOpenState = true; // Si está disparando, DEBE permanecer abierto
            }
            else
            {
                desiredWindowOpenState = _isPlayerDetected && CanMove; // Si no está disparando, abrir si detecta y puede moverse/iniciar
            }

            // Actualizar el estado de la ventana en el Animator si ha cambiado el estado deseado
            if (_currentWindowOpenState != desiredWindowOpenState)
            {
                _currentWindowOpenState = desiredWindowOpenState;
                _visualController.SetWindowOpenState(_currentWindowOpenState);
                Debug.Log($"[{Time.frameCount}] WindowEnemy AI: Setting Animator 'isOpen' to {_currentWindowOpenState}. Reason: PlayerDetected={_isPlayerDetected}, CanMove={CanMove}, IsAttackingOrFiring={isCurrentlyAttackingOrFiring}");
            }

            // Lógica de Ataque: Solo si la ventana está abierta (o se supone que lo está) y no está ya disparando
            if (_currentWindowOpenState && CanMove && !isCurrentlyAttackingOrFiring) 
            {
                AnimatorStateInfo currentAnimatorState = GetCurrentAnimatorStateIfPossible();
                bool isInAimableState = currentAnimatorState.IsName("Open - Aim") || currentAnimatorState.IsName("Open"); 
                                            
                if (isInAimableState && _rangedAttacker.CanInitiateAttack(playerTarget))
                {
                    Vector2 bestFireDir = _rangedAttacker.GetCalculatedBestFixedDirection(playerTarget.position);
                    float aimParamXValue = 0f;
                    if (Mathf.Abs(bestFireDir.x) < 0.1f && bestFireDir.y < -0.1f) aimParamXValue = 0f; 
                    else if (bestFireDir.x < -0.1f) aimParamXValue = -1f; 
                    else if (bestFireDir.x > 0.1f) aimParamXValue = 1f;  
                    
                    _visualController.SetWindowAimDirection(aimParamXValue);
                    _visualController.TriggerWindowAttack(); // Dispara el trigger para la animación/estado de Attack
                    
                    // TryAttack iniciará la corrutina que pone CanMove=false y isCurrentlyFiring=true
                    _rangedAttacker.TryAttack(playerTarget); 
                }
            }
        }

        // Helper para obtener el estado del animator de forma segura
        private AnimatorStateInfo GetCurrentAnimatorStateIfPossible()
        {
            if (_visualController != null) {
                Animator anim = _visualController.GetBodyAnimator(); // Asumiendo que GetBodyAnimator devuelve el animator correcto
                if (anim != null && anim.gameObject.activeInHierarchy && anim.runtimeAnimatorController != null) {
                    if (!anim.IsInTransition(0)) { // Solo si no está en transición
                        return anim.GetCurrentAnimatorStateInfo(0);
                    }
                }
            }
            return default; // Devuelve un estado inválido si no se puede obtener
        }

        private void HandleDetection()
        {
            if (playerTarget == null) { _isPlayerDetected = false; return; }
            float dist = Vector2.Distance(transform.position, playerTarget.position);

            if (dist > detectionRange) { _isPlayerDetected = false; return; }

            // Si está dentro del rango circular, comprobar cono si aplica
            if (isStaticWindowEnemy && useConeDetectionForWindow) 
            {
                Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;
                // La dirección del cono es local, transformarla a world space
                Vector2 worldConeFwd = transform.TransformDirection(coneForwardDirectionLocal.normalized);
                if (worldConeFwd == Vector2.zero) worldConeFwd = transform.up * (coneForwardDirectionLocal.y > 0 ? 1 : -1); // Fallback si transform.TransformDirection da cero (ej. escala Z cero)


                float angleToPlayer = Vector2.Angle(worldConeFwd, dirToPlayer);
                _isPlayerDetected = (angleToPlayer <= coneDetectionAngle / 2f);
            } 
            else // Detección circular para enemigos móviles o si el de ventana no usa cono
            {
                _isPlayerDetected = true; // Ya sabemos que dist <= detectionRange
            }
        }

        private void FaceTarget() 
        {
            if (playerTarget == null || !CanMove || isStaticWindowEnemy) return; // No orientar si no puede moverse o es de ventana

            if (playerTarget.position.x > transform.position.x && !IsFacingRight) Flip();
            else if (playerTarget.position.x < transform.position.x && IsFacingRight) Flip();
        }

        public void Flip() 
        {
            if (isStaticWindowEnemy) return; // Enemigo de ventana no flipea así

            IsFacingRight = !IsFacingRight;
            if (visualsContainerTransform != null) {
                visualsContainerTransform.localScale = new Vector3(
                    visualsContainerTransform.localScale.x * -1f,
                    visualsContainerTransform.localScale.y,
                    visualsContainerTransform.localScale.z);
            } else { Debug.LogWarning($"AIController on {gameObject.name}: visualsContainerTransform not set for Flip()."); }
        }
        
        public void SetCanMove(bool canMove) { this.CanMove = canMove; }

        // Gizmos para rangos (puedes copiar/adaptar de tu versión anterior)
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Detection & Engagement Ranges
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, engagementRange);

            // Cone Detection Gizmo (si aplica)
            if (isStaticWindowEnemy && useConeDetectionForWindow)
            {
                Gizmos.color = Color.blue;
                Vector3 forward = transform.TransformDirection(coneForwardDirectionLocal.normalized);
                if (forward == Vector3.zero)
                    forward = transform.forward; // Fallback si coneForwardDirectionLocal es cero

                Quaternion upRayRotation =
                    Quaternion.AngleAxis(-coneDetectionAngle / 2,
                        transform.forward); // Rotar alrededor del Z local para 2D
                Quaternion downRayRotation = Quaternion.AngleAxis(coneDetectionAngle / 2, transform.forward);

                Vector3 upRayDirection = upRayRotation * forward;
                Vector3 downRayDirection = downRayRotation * forward;

                Gizmos.DrawRay(transform.position, upRayDirection * detectionRange);
                Gizmos.DrawRay(transform.position, downRayDirection * detectionRange);
                // Dibujar arcos es más complejo, una línea entre los extremos del cono puede ayudar
                Gizmos.DrawLine(transform.position + upRayDirection * detectionRange,
                    transform.position + downRayDirection * detectionRange);
            }
        }
#endif
    }
}