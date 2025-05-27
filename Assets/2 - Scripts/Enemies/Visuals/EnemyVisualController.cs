// En Scripts/Enemies/Visuals/EnemyVisualController.cs
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
        [Header("Core Component References")]
        [SerializeField] private Animator bodyAnimator;

        // Referencias a otros sistemas del enemigo (se buscarán en el root)
        private EnemyAIController _aiController;
        private EnemyMovementComponent _movementComponent; // Solo relevante para móviles
        private Rigidbody2D _rb; // Solo relevante para móviles
        private EnemyHealth _enemyHealth;
        private EnemyAttackMelee _meleeAttacker;
        private EnemyAttackRanged _rangedAttacker;

        // Animator Parameter Hashes (Generales)
        private readonly int animIsMovingHash = Animator.StringToHash("isMoving");
        private readonly int animIsAttackingHash = Animator.StringToHash("isAttacking"); // Para estado general de ataque melee/ranged móvil
        private readonly int animDieTriggerHash = Animator.StringToHash("Die");
        
        // Animator Parameter Hashes (Específicos para Ventana, nombres deben coincidir con los del AIController y Animator)
        // Los nombres en sí pueden ser pasados desde AIController o ser constantes aquí si siempre son iguales.
        private int _animWindowIsOpenHash;
        private int _animWindowAimDirectionXHash;
        private int _animWindowTriggerAttackHash;


        void Awake()
        {
            if (bodyAnimator == null) bodyAnimator = GetComponent<Animator>();

            Transform root = transform.root; // Asumiendo que Visuals (donde está este script) es hijo del Root
            if (root == gameObject.transform) root = null; // Si este script está en el root, no buscar en el padre

            if (root != null)
            {
                _aiController = root.GetComponent<EnemyAIController>();
                _enemyHealth = root.GetComponent<EnemyHealth>();
                // Solo obtener componentes móviles si el AIController indica que no es de ventana
                if (_aiController != null && !_aiController.isStaticWindowEnemy)
                {
                    _movementComponent = root.GetComponentInChildren<EnemyMovementComponent>(true);
                    _rb = root.GetComponent<Rigidbody2D>();
                }
                // Los componentes de ataque se obtienen en Update o según necesidad, o aquí si siempre existen
                _meleeAttacker = root.GetComponent<EnemyAttackMelee>() ?? root.GetComponentInChildren<EnemyAttackMelee>(true);
                _rangedAttacker = root.GetComponent<EnemyAttackRanged>() ?? root.GetComponentInChildren<EnemyAttackRanged>(true);
            }
            else Debug.LogError("EVC: No se pudo encontrar el Root del enemigo.", this);

            // Validaciones
            if (bodyAnimator == null) Debug.LogError("EVC: Animator no asignado.", this);
            if (_aiController == null) Debug.LogWarning("EVC: EnemyAIController no encontrado.", this);
            if (_enemyHealth == null) Debug.LogWarning("EVC: EnemyHealth no encontrado.", this);

            // Inicializar hashes para ventana si este VisualController es para un enemigo de ventana
            if (_aiController != null && _aiController.isStaticWindowEnemy)
            {
                _animWindowIsOpenHash = Animator.StringToHash(_aiController.animParamIsOpen);
                _animWindowAimDirectionXHash = Animator.StringToHash(_aiController.animParamAimDirectionX);
                _animWindowTriggerAttackHash = Animator.StringToHash(_aiController.animTriggerActualAttack);
            }
        }

        void Update()
        {
            if (bodyAnimator == null || _aiController == null || _enemyHealth == null) return;
            if (_enemyHealth.IsDeadPublic) return;

            if (!_aiController.isStaticWindowEnemy) // Lógica para enemigos móviles
            {
                // Movimiento
                bool isActuallyMoving = false;
                if (_movementComponent != null && _rb != null && _aiController.CanMove)
                {
                    isActuallyMoving = _movementComponent.IsGrounded && Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
                }
                bodyAnimator.SetBool(animIsMovingHash, isActuallyMoving);

                // Ataque (para Melee o RangedMobile)
                bool isCurrentlyPerformingAttack = false;
                if (_aiController.behaviorType == EnemyAIController.EnemyBehaviorType.Melee && _meleeAttacker != null && _meleeAttacker.IsAttacking())
                {
                    isCurrentlyPerformingAttack = true;
                }
                else if (_aiController.behaviorType == EnemyAIController.EnemyBehaviorType.RangedMobile && _rangedAttacker != null && _rangedAttacker.IsFiring())
                {
                    isCurrentlyPerformingAttack = true;
                }
                bodyAnimator.SetBool(animIsAttackingHash, isCurrentlyPerformingAttack);
            }
            // Para el enemigo de ventana, el AIController llamará a métodos específicos para controlar su Animator.
        }

        // --- MÉTODOS PÚBLICOS PARA SER LLAMADOS POR EnemyAIController (especialmente para el de ventana) ---

        public void SetWindowOpenState(bool isOpen)
        {
            if (bodyAnimator != null && _animWindowIsOpenHash != 0)
            {
                bodyAnimator.SetBool(_animWindowIsOpenHash, isOpen);
            }
        }

        public void SetWindowAimDirection(float aimX)
        {
            if (bodyAnimator != null && _animWindowAimDirectionXHash != 0)
            {
                bodyAnimator.SetFloat(_animWindowAimDirectionXHash, aimX);
            }
        }

        public void TriggerWindowAttack()
        {
            if (bodyAnimator != null && _animWindowTriggerAttackHash != 0)
            {
                bodyAnimator.SetTrigger(_animWindowTriggerAttackHash);
            }
        }

        // --- Métodos públicos generales que podrían ser llamados por otros scripts (ej. EnemyHealth) ---
        public void TriggerDeathAnimation()
        {
            if(bodyAnimator != null && animDieTriggerHash != 0) bodyAnimator.SetTrigger(animDieTriggerHash);
        }
        
        // Necesario para PlayerHealthSystem si quieres que obtenga estas referencias de forma encapsulada
        public Animator GetBodyAnimator() => bodyAnimator;
        // public SpriteRenderer GetBodySpriteRenderer() => _bodySpriteRenderer; // Si tuvieras _bodySpriteRenderer
    }
}
