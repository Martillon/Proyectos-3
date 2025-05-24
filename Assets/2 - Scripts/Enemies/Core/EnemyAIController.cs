// En Scripts/Enemies/Core (o similar)
using UnityEngine;
using Scripts.Enemies.Movement; // Para EnemyMovementComponent
using Scripts.Enemies.Movement.SteeringBehaviors; // Para ISteeringBehavior2D
using Scripts.Enemies.Movement.SteeringBehaviors.Implementations; // Para las clases concretas
using Scripts.Enemies.Core;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Ranged; // Para EnemyHealth (si no está en el mismo namespace)
// Añade using para tus scripts de ataque Melee/Ranged

namespace Scripts.Enemies.Core // Ajusta el namespace
{
    [RequireComponent(typeof(EnemyHealth))]
    // [RequireComponent(typeof(Rigidbody2D))] // EnemyMovementComponent ya lo requiere
    public class EnemyAIController : MonoBehaviour
    {
        public enum EnemyBehaviorType { Melee, Ranged }

        [Header("AI Core Settings")]
        [SerializeField] private EnemyBehaviorType behaviorType = EnemyBehaviorType.Melee;
        [Tooltip("Range within which the enemy detects the player.")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("Range within which the enemy will stop approaching and start attacking (if applicable).")]
        [SerializeField] private float engagementRange = 1.5f;
        [Tooltip("Transform of the player. If null, will try to find GameObject with 'Player' tag.")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private bool canPatrol = true;
        [Tooltip("Speed of the enemy when chasing the player.")]
        [SerializeField] private float initialChaseSpeed = 3f;

        [Header("Patrol Behavior Config")]
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private float patrolMoveTime = 3f; // Tiempo máximo moviéndose en una dirección antes de esperar

        // (ChaseSpeed y otros parámetros de behaviors específicos podrían ir aquí o ser manejados de otra forma)


        // Component References
        private EnemyMovementComponent movementComponent;
        private EnemyHealth enemyHealth;
        private StayStillBehavior2D stayStillBehavior;
        private PatrolBehavior2D patrolBehavior;
        private ChaseBehavior2D chaseBehavior; 
        private EnemyAttackMelee meleeAttacker; 
        private EnemyAttackRanged rangedAttacker; 

        // State
        private bool isPlayerDetected = false;
        public bool IsFacingRight { get; private set; } = true; // El movimiento lo gestionará
        public bool CanMove { get; private set; } = true; // Para pausar movimiento durante ataques, etc.

        public bool IsDead => enemyHealth != null && enemyHealth.IsDead; // Propiedad de conveniencia


        private void Awake()
        {
            movementComponent = GetComponentInChildren<EnemyMovementComponent>();
            enemyHealth = GetComponent<EnemyHealth>();
            // meleeAttacker = GetComponent<EnemyAttackMelee>();
            // rangedAttacker = GetComponent<EnemyAttackRanged>();

            if (playerTarget == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) playerTarget = playerObject.transform;
                // else Debug.LogWarning($"EnemyAIController on '{gameObject.name}': Player target not found.", this);
            }

            // Initialize steering behaviors
            stayStillBehavior = new StayStillBehavior2D();
            patrolBehavior = new PatrolBehavior2D(patrolSpeed, patrolWaitTime, patrolMoveTime);
            chaseBehavior = new ChaseBehavior2D(initialChaseSpeed, playerTarget, engagementRange);
            meleeAttacker = GetComponentInChildren<EnemyAttackMelee>();
            rangedAttacker = GetComponentInChildren<EnemyAttackRanged>();
            // Asegurarse de que el movimiento no esté en StayStill al inicio
            if (chaseBehavior != null) // Asegurarse de que chaseBehavior esté inicializado
            {
                chaseBehavior.UpdateTarget(playerTarget); 
                // chaseBehavior.UpdateChaseSpeed(currentChaseSpeed); // Si tienes una variable para la velocidad de chase
                // Corrección: Deberías pasar la velocidad de chase configurada
                chaseBehavior.UpdateChaseSpeed(initialChaseSpeed); 
                chaseBehavior.UpdateStoppingDistance(engagementRange); // Importante pasar el engagementRange
            }

            // Asegurar que el visualsContainerTransform esté asignado si Flip lo usa
            if (visualsContainerTransform == null) // Asumiendo que tienes esta variable
            {
                 Transform visuals = transform.Find("Visuals"); // Intenta encontrarlo por nombre
                 if (visuals != null) visualsContainerTransform = visuals;
                 // else Debug.LogError("Visuals child GameObject not found for flipping!", this);
            }
        }

        private void Update()
        {
            if (IsDead)
            {
                movementComponent.SetSteeringBehavior(stayStillBehavior);
                return;
            }

            // Actualizar parámetros de behaviors si pueden cambiar dinámicamente
            patrolBehavior.UpdatePatrolParameters(patrolSpeed, patrolWaitTime, patrolMoveTime);
            if (chaseBehavior != null) // Asegurarse de que chaseBehavior esté inicializado
            {
                chaseBehavior.UpdateTarget(playerTarget); 
                // chaseBehavior.UpdateChaseSpeed(currentChaseSpeed); // Si tienes una variable para la velocidad de chase
            }


            HandleDetection(); // Actualiza isPlayerDetected

            // Si un ataque está en progreso, CanMove será false. El movementComponent ya usa stayStill.
            // La lógica principal aquí es decidir si *iniciar* un ataque o cambiar de comportamiento de movimiento.
            if (!CanMove) 
            {
                // Ya está atacando o en una secuencia que bloquea el movimiento.
                // EnemyMovementComponent debería estar usando StayStillBehavior debido a CanMove.
                // No necesitamos hacer más aquí respecto al cambio de behavior.
                return;
            }

            if (isPlayerDetected)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
                bool canPhysicallyAttack = false;

                if (behaviorType == EnemyBehaviorType.Melee && meleeAttacker != null)
                {
                    canPhysicallyAttack = meleeAttacker.CanInitiateAttack(playerTarget);
                    //Debug.Log($"AI - Melee Check: distanceToPlayer({distanceToPlayer}) " +
                      //        $"<= engagementRange({engagementRange})? {distanceToPlayer <= engagementRange}. " +
                        //      $"canPhysicallyAttack? {canPhysicallyAttack}");
                }
                else if (behaviorType == EnemyBehaviorType.Ranged && rangedAttacker != null)
                {
                    canPhysicallyAttack = rangedAttacker.CanInitiateAttack(playerTarget);
                }

                if (distanceToPlayer <= engagementRange && canPhysicallyAttack)
                {
                    Debug.Log("AI STATE: DECIDING TO ATTACK");
                    // ESTADO: INICIAR ATAQUE
                    movementComponent.SetSteeringBehavior(stayStillBehavior);
                    // SetCanMove(false) será llamado por el script de ataque al iniciar.
                    FaceTarget(); // Orientarse justo antes de atacar

                    if (behaviorType == EnemyBehaviorType.Melee)
                    {
                        meleeAttacker.TryAttack(playerTarget);
                    }
                    else if (behaviorType == EnemyBehaviorType.Ranged)
                    {
                        rangedAttacker.TryAttack(playerTarget);
                    }
                }
                else if (distanceToPlayer <= engagementRange && !canPhysicallyAttack)
                {
                    Debug.Log("AI STATE: IN ENGAGEMENT RANGE, BUT CANNOT ATTACK");
                    // ESTADO: EN RANGO DE ENGAGEMENT, PERO NO PUEDE ATACAR (COOLDOWN, ETC.)
                    movementComponent.SetSteeringBehavior(stayStillBehavior);
                    FaceTarget(); // Mantenerse orientado al jugador
                }
                else // Fuera de engagementRange (o no puede atacar por otras razones y está lejos)
                {
                    Debug.Log("AI STATE: CHASING (or out of range)");
                    // ESTADO: PERSIGUIENDO
                    if (chaseBehavior != null) {
                         movementComponent.SetSteeringBehavior(chaseBehavior);
                    } else { // Fallback si chaseBehavior no está listo (debería estarlo)
                         movementComponent.SetSteeringBehavior(stayStillBehavior);
                         FaceTarget();
                    }
                }
            }
            else // No detectado
            {
                if (canPatrol)
                {
                    movementComponent.SetSteeringBehavior(patrolBehavior);
                }
                else
                {
                    movementComponent.SetSteeringBehavior(stayStillBehavior);
                }
            }
        }

        private void HandleDetection()
        {
            if (playerTarget == null)
            {
                isPlayerDetected = false;
                return;
            }
            // TODO: Implementar detección en cono si es necesario (para enemigo de ventana)
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            isPlayerDetected = (distanceToPlayer <= detectionRange);
        }

        private void FaceTarget()
        {
            if (playerTarget == null || !CanMove) return; // No orientar si no puede moverse (ej. atacando)

            if (playerTarget.position.x > transform.position.x && !IsFacingRight)
            {
                Flip();
            }
            else if (playerTarget.position.x < transform.position.x && IsFacingRight)
            {
                Flip();
            }
        }

        // Variable para el contenedor de los visuales (sprites, animator)
        [Header("Visuals")]
        [Tooltip("El Transform del GameObject que contiene los elementos visuales y que será escalado para flipear.")]
        [SerializeField] private Transform visualsContainerTransform;

        public void Flip()
        {
            IsFacingRight = !IsFacingRight;
            if (visualsContainerTransform != null)
            {
                visualsContainerTransform.localScale = new Vector3(
                    visualsContainerTransform.localScale.x * -1f,
                    visualsContainerTransform.localScale.y,
                    visualsContainerTransform.localScale.z
                );
            }
            else // Fallback si visualsContainerTransform no está asignado
            {
                 transform.Rotate(0f, 180f, 0f); // Esto podría no funcionar bien con sprites 2D
                 Debug.LogWarning("EnemyAIController: visualsContainerTransform not set for Flip(), using transform.Rotate as fallback.", this);
            }
        }
        
        public void SetCanMove(bool canEnemyMove)
        {
            this.CanMove = canEnemyMove;
        }

        // Gizmos para rangos (puedes copiar/adaptar de tu versión anterior)
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, engagementRange);
        }
        #endif
    }
}