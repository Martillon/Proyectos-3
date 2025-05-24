// En Scripts/Enemies/Visuals (o donde tengas los scripts de Visuals)
using UnityEngine;
using Scripts.Enemies.Core;
using Scripts.Enemies.Movement;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Ranged;

namespace Scripts.Enemies.Visuals
{
    [RequireComponent(typeof(Animator))]
    public class EnemyVisualController : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("Animator para el cuerpo del enemigo.")]
        [SerializeField] private Animator bodyAnimator;

        // Referencias a otros sistemas del enemigo (se buscarán en el padre/root)
        private EnemyAIController _aiController;
        private EnemyMovementComponent _movementComponent;
        private Rigidbody2D _rb; // Necesario para la velocidad
        private EnemyHealth _enemyHealth;
        private EnemyAttackMelee _meleeAttacker;
        private EnemyAttackRanged _rangedAttacker;

        // Animator Parameter Hashes
        private readonly int animIsMovingHash = Animator.StringToHash("isMoving");
        private readonly int animIsAttackingHash = Animator.StringToHash("isAttacking"); // Para estado general de ataque
        private readonly int animDieTriggerHash = Animator.StringToHash("Die"); // Asumimos un trigger "Die"

        void Awake()
        {
            if (bodyAnimator == null)
                bodyAnimator = GetComponent<Animator>();

            // Buscar componentes en el GameObject raíz del enemigo
            Transform root = transform.parent?.parent; // Asumiendo Visuals -> SpriteHolder -> ESTE SCRIPT
                                                      // O si este script está en "Visuals": root = transform.parent;
                                                      // O si este script está en "Sprite_Animator_Holder": root = transform.parent.parent;
            // Ajusta la búsqueda según dónde coloques este EnemyVisualController.
            // La forma más segura es si todos los componentes principales están en el Root y Visuals es hijo del Root:
            if (transform.parent != null && transform.parent.parent != null) // Si este script está en Sprite_Animator_Holder, y Visuals es su padre, y Root es el padre de Visuals
            {
                 root = transform.parent.parent;
            }
            else if (transform.parent != null) // Si este script está en Visuals, y Root es su padre
            {
                root = transform.parent;
            } else {
                root = transform; // Si este script está en el Root (menos probable para VisualController)
            }


            if (root != null)
            {
                _aiController = GetComponentInParent<EnemyAIController>();
                _movementComponent = root.GetComponentInChildren<EnemyMovementComponent>(); // Podría estar en un hijo de Root
                _rb = GetComponentInParent<Rigidbody2D>();
                _enemyHealth = GetComponentInParent<EnemyHealth>();
                _meleeAttacker = GetComponent<EnemyAttackMelee>();
                _rangedAttacker = GetComponent<EnemyAttackRanged>();
            }
            else
            {
                Debug.LogError("EnemyVisualController: No se pudo encontrar el Root del enemigo para obtener referencias.", this);
            }

            if (bodyAnimator == null) Debug.LogError("EnemyVisualController: Animator no asignado.", this);
            if (_aiController == null) Debug.LogWarning("EnemyVisualController: EnemyAIController no encontrado.", this);
            if (_movementComponent == null) Debug.LogWarning("EnemyVisualController: EnemyMovementComponent no encontrado.", this);
            if (_rb == null) Debug.LogWarning("EnemyVisualController: Rigidbody2D no encontrado en el Root.", this);
            if (_enemyHealth == null) Debug.LogWarning("EnemyVisualController: EnemyHealth no encontrado.", this);
            // meleeAttacker y rangedAttacker pueden ser null si el enemigo es de un solo tipo.
        }
        
        // Podrías suscribirte a eventos de EnemyHealth para Hit y Die si prefieres ese enfoque
        // en lugar de que EnemyHealth llame directamente a métodos aquí o ponga triggers.
        // Por ahora, asumimos que los triggers de ataque vienen de los scripts de ataque, y Die de EnemyHealth.

        void Update()
        {
            if (bodyAnimator == null || _aiController == null || _enemyHealth == null) return;

            if (_enemyHealth.IsDead) // Si está muerto, no actualizar otras animaciones de estado
            {
                // La animación de muerte debería haber sido disparada por EnemyHealth.Die()
                // bodyAnimator.SetTrigger(animDieTriggerHash); // EnemyHealth podría hacer esto
                return;
            }

            // Movimiento
            bool isGrounded = !_movementComponent || _movementComponent.IsGrounded; // Asumir grounded si no hay movementComponent

            bool isActuallyMoving = false;
            if (_movementComponent != null && _rb != null && _aiController.CanMove)
            {
                isActuallyMoving = isGrounded && Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
            }
            bodyAnimator.SetBool(animIsMovingHash, isActuallyMoving);
        }

        // Métodos públicos que podrían ser llamados por otros scripts (ej. EnemyHealth)
        public void TriggerDeathAnimation()
        {
            if(bodyAnimator != null) bodyAnimator.SetTrigger(animDieTriggerHash);
        }
        
    }
}
